using System.Collections.Generic;
using System.Linq;

namespace SqlAnalyzer.Core.Security
{
    /// <summary>
    /// Interface for validating database connection strings
    /// </summary>
    public interface IConnectionStringValidator
    {
        /// <summary>
        /// Validates a connection string for security and configuration issues
        /// </summary>
        /// <param name="connectionString">The connection string to validate</param>
        /// <returns>Validation result with any errors or warnings</returns>
        ValidationResult Validate(string connectionString);
    }

    /// <summary>
    /// Result of connection string validation
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Whether the connection string is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Security level assessment
        /// </summary>
        public SecurityLevel SecurityLevel { get; set; }

        /// <summary>
        /// Detected database type
        /// </summary>
        public DatabaseType DatabaseType { get; set; }

        /// <summary>
        /// Detected authentication type
        /// </summary>
        public AuthenticationType AuthenticationType { get; set; }

        /// <summary>
        /// List of validation errors (connection string is invalid)
        /// </summary>
        public List<ValidationIssue> Errors { get; set; } = new List<ValidationIssue>();

        /// <summary>
        /// List of validation warnings (connection string is valid but has issues)
        /// </summary>
        public List<ValidationIssue> Warnings { get; set; } = new List<ValidationIssue>();

        /// <summary>
        /// Get security recommendations based on validation results
        /// </summary>
        public List<string> GetRecommendations()
        {
            var recommendations = new List<string>();

            if (Warnings.Any(w => w.Code == "WEAK_PASSWORD"))
            {
                recommendations.Add("Use a strong password with at least 8 characters, including uppercase, lowercase, numbers, and special characters.");
            }

            if (Warnings.Any(w => w.Code == "MISSING_CREDENTIALS") && DatabaseType == DatabaseType.SqlServer)
            {
                recommendations.Add("Consider using Windows Authentication (Integrated Security=true) for better security.");
            }

            if (Warnings.Any(w => w.Code == "PRODUCTION_INDICATOR"))
            {
                recommendations.Add("Ensure you're not using production credentials in development/test environments.");
            }

            if (!Errors.Any() && !Warnings.Any() && SecurityLevel != SecurityLevel.High)
            {
                recommendations.Add("Consider using Azure Key Vault or similar secure credential storage for production environments.");
            }

            if (Warnings.Any(w => w.Code == "HIGH_TIMEOUT"))
            {
                recommendations.Add("High connection timeouts may indicate network issues. Consider optimizing network connectivity.");
            }

            return recommendations;
        }
    }

    /// <summary>
    /// Represents a validation issue
    /// </summary>
    public class ValidationIssue
    {
        /// <summary>
        /// Error/warning code
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Human-readable message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Additional details
        /// </summary>
        public string Details { get; set; }

        public ValidationIssue(string code, string message, string details = null)
        {
            Code = code;
            Message = message;
            Details = details;
        }
    }

    /// <summary>
    /// Security level assessment
    /// </summary>
    public enum SecurityLevel
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Database type for validation context
    /// </summary>
    public enum DatabaseType
    {
        Unknown,
        SqlServer,
        PostgreSql,
        MySql
    }

    /// <summary>
    /// Authentication type detected in connection string
    /// </summary>
    public enum AuthenticationType
    {
        Unknown,
        Windows,
        SqlServer,
        Certificate,
        AzureActiveDirectory
    }
}