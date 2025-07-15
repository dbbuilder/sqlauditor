using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SqlAnalyzer.Core.Security
{
    /// <summary>
    /// Validates database connection strings for security and configuration issues
    /// </summary>
    public class ConnectionStringValidator : IConnectionStringValidator
    {
        private static readonly HashSet<string> WeakPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password", "123456", "password123", "admin", "letmein", "welcome",
            "monkey", "dragon", "baseball", "football", "qwerty", "abc123",
            "111111", "1234567", "12345678", "123456789", "1234567890",
            "sa", "root", "administrator", "guest", "user", "test"
        };

        private static readonly string[] ProductionIndicators =
        {
            "prod", "production", "live", "prd"
        };

        private static readonly string[] SecretIndicators =
        {
            "secret", "key", "token", "api", "private"
        };

        private static readonly Regex TokenPattern = new Regex(
            @"(ghp_|ghs_|gho_|github_pat_|xox[baprs]-|sk-|pk-|rk-|s3cr3t)",
            RegexOptions.IgnoreCase);

        public ValidationResult Validate(string connectionString)
        {
            var result = new ValidationResult
            {
                IsValid = true,
                SecurityLevel = SecurityLevel.Medium
            };

            // Check for null or empty
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationIssue(
                    "EMPTY_CONNECTION_STRING",
                    "Connection string is null or empty"));
                return result;
            }

            // Parse connection string
            var parsed = ParseConnectionString(connectionString);

            // Detect database type
            result.DatabaseType = DetectDatabaseType(parsed);

            // Initialize authentication type to Unknown
            result.AuthenticationType = AuthenticationType.Unknown;

            // Validate based on database type
            ValidateCredentials(parsed, result);
            ValidateSecuritySettings(parsed, result);
            CheckForSensitiveData(connectionString, parsed, result);

            // Determine security level
            DetermineSecurityLevel(parsed, result);

            return result;
        }

        private Dictionary<string, string> ParseConnectionString(string connectionString)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var keyValue = part.Split('=', 2);
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim();
                    var value = keyValue[1].Trim();

                    // Normalize common variations
                    key = NormalizeKey(key);
                    result[key] = value;
                }
            }

            return result;
        }

        private string NormalizeKey(string key)
        {
            return key.ToLower() switch
            {
                "server" or "data source" or "host" => "server",
                "database" or "initial catalog" => "database",
                "user id" or "uid" or "user" or "username" => "userid",
                "password" or "pwd" => "password",
                "integrated security" or "trusted_connection" => "integratedsecurity",
                _ => key.ToLower()
            };
        }

        private DatabaseType DetectDatabaseType(Dictionary<string, string> parsed)
        {
            // PostgreSQL detection
            if (parsed.ContainsKey("host") || parsed.ContainsKey("port") && parsed.GetValueOrDefault("port") == "5432")
            {
                return DatabaseType.PostgreSql;
            }

            // MySQL detection
            if (parsed.ContainsKey("port") && parsed.GetValueOrDefault("port") == "3306")
            {
                return DatabaseType.MySql;
            }

            // SQL Server indicators
            if (parsed.ContainsKey("integratedsecurity") ||
                parsed.ContainsKey("trustservercertificate") ||
                parsed.ContainsKey("multipleactiveresultsets"))
            {
                return DatabaseType.SqlServer;
            }

            // Default to SQL Server for ambiguous cases
            if (parsed.ContainsKey("server") && parsed.ContainsKey("database"))
            {
                return DatabaseType.SqlServer;
            }

            return DatabaseType.Unknown;
        }

        private void ValidateCredentials(Dictionary<string, string> parsed, ValidationResult result)
        {
            var hasIntegratedSecurity = parsed.GetValueOrDefault("integratedsecurity")?.ToLower() == "true" ||
                                       parsed.GetValueOrDefault("integratedsecurity")?.ToLower() == "sspi" ||
                                       parsed.GetValueOrDefault("integratedsecurity")?.ToLower() == "yes";

            if (hasIntegratedSecurity)
            {
                result.AuthenticationType = AuthenticationType.Windows;

                // Check for mixed authentication
                if (parsed.ContainsKey("userid") || parsed.ContainsKey("password"))
                {
                    result.Warnings.Add(new ValidationIssue(
                        "MIXED_AUTHENTICATION",
                        "Both Windows and SQL authentication specified",
                        "When using Integrated Security, User ID and Password should not be specified"));
                }

                // Validate Windows Auth is appropriate for the database type
                if (result.DatabaseType != DatabaseType.SqlServer && result.DatabaseType != DatabaseType.Unknown)
                {
                    result.Warnings.Add(new ValidationIssue(
                        "WINDOWS_AUTH_UNSUPPORTED",
                        $"Windows authentication not supported for {result.DatabaseType}",
                        "Windows authentication is only supported for SQL Server"));
                }
            }
            else
            {
                result.AuthenticationType = AuthenticationType.SqlServer;

                var userId = parsed.GetValueOrDefault("userid");
                var password = parsed.GetValueOrDefault("password");

                if (string.IsNullOrEmpty(userId))
                {
                    result.Warnings.Add(new ValidationIssue(
                        "MISSING_CREDENTIALS",
                        "No user credentials specified",
                        "Connection string has no User ID and is not using Integrated Security"));
                }

                if (string.IsNullOrEmpty(password))
                {
                    result.Warnings.Add(new ValidationIssue(
                        "EMPTY_PASSWORD",
                        "Password is empty",
                        "Empty passwords are a security risk"));
                }
                else if (IsWeakPassword(password))
                {
                    result.Warnings.Add(new ValidationIssue(
                        "WEAK_PASSWORD",
                        "Weak password detected",
                        "The password appears to be weak or commonly used"));
                }
            }
        }

        private void ValidateSecuritySettings(Dictionary<string, string> parsed, ValidationResult result)
        {
            // Check for MARS (Multiple Active Result Sets)
            if (parsed.GetValueOrDefault("multipleactiveresultsets")?.ToLower() == "true")
            {
                result.Warnings.Add(new ValidationIssue(
                    "MARS_ENABLED",
                    "Multiple Active Result Sets is enabled",
                    "MARS can have performance implications and should be used carefully"));
            }

            // Check for high timeout values
            var timeout = parsed.GetValueOrDefault("connecttimeout") ?? parsed.GetValueOrDefault("connectiontimeout");
            if (int.TryParse(timeout, out var timeoutValue) && timeoutValue > 60)
            {
                result.Warnings.Add(new ValidationIssue(
                    "HIGH_TIMEOUT",
                    $"High connection timeout detected: {timeoutValue} seconds",
                    "High timeouts may indicate network issues"));
            }

            // Check for missing TrustServerCertificate in SQL Server
            if (result.DatabaseType == DatabaseType.SqlServer &&
                !parsed.ContainsKey("trustservercertificate"))
            {
                // This is actually good for security, so no warning
            }
        }

        private void CheckForSensitiveData(string connectionString, Dictionary<string, string> parsed, ValidationResult result)
        {
            var lowerConnectionString = connectionString.ToLower();

            // Check for production indicators
            if (ProductionIndicators.Any(indicator => lowerConnectionString.Contains(indicator)))
            {
                result.Warnings.Add(new ValidationIssue(
                    "PRODUCTION_INDICATOR",
                    "Connection string may contain production server reference",
                    "Ensure production credentials are properly secured"));
            }

            // Check for possible secrets in password
            var password = parsed.GetValueOrDefault("password");
            if (!string.IsNullOrEmpty(password))
            {
                if (SecretIndicators.Any(indicator => password.ToLower().Contains(indicator)))
                {
                    result.Warnings.Add(new ValidationIssue(
                        "POSSIBLE_SECRET",
                        "Password may contain a secret or key",
                        "Consider using secure credential storage"));
                }

                if (TokenPattern.IsMatch(password))
                {
                    result.Warnings.Add(new ValidationIssue(
                        "POSSIBLE_TOKEN",
                        "Password appears to contain an API token",
                        "API tokens should not be used as database passwords"));
                }
            }
        }

        private void DetermineSecurityLevel(Dictionary<string, string> parsed, ValidationResult result)
        {
            // High security: Windows Auth with no warnings
            if (result.AuthenticationType == AuthenticationType.Windows)
            {
                // Check if there are no mixed auth warnings
                if (!result.Warnings.Any(w => w.Code == "MIXED_AUTHENTICATION" || w.Code == "WINDOWS_AUTH_UNSUPPORTED"))
                {
                    result.SecurityLevel = SecurityLevel.High;
                }
                else
                {
                    result.SecurityLevel = SecurityLevel.Medium;
                }
                return;
            }

            // Low security: errors or critical warnings
            if (result.Errors.Any() || result.Warnings.Any(w => w.Code == "WEAK_PASSWORD" || w.Code == "EMPTY_PASSWORD"))
            {
                result.SecurityLevel = SecurityLevel.Low;
                return;
            }

            // Medium security: default
            result.SecurityLevel = SecurityLevel.Medium;
        }

        private bool IsWeakPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return true;

            // Check against weak password list
            if (WeakPasswords.Contains(password))
                return true;

            // Check for simple patterns
            if (password.Length < 8)
                return true;

            // Check for all numbers
            if (password.All(char.IsDigit))
                return true;

            // Check for sequential patterns
            if (IsSequential(password))
                return true;

            return false;
        }

        private bool IsSequential(string password)
        {
            if (password.Length < 3)
                return false;

            for (int i = 0; i < password.Length - 2; i++)
            {
                if (password[i] + 1 == password[i + 1] && password[i + 1] + 1 == password[i + 2])
                    return true;
                if (password[i] - 1 == password[i + 1] && password[i + 1] - 1 == password[i + 2])
                    return true;
            }

            return false;
        }
    }
}