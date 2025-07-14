using System;
using System.Collections.Generic;
using FluentAssertions;
using SqlAnalyzer.Core.Security;
using Xunit;

namespace SqlAnalyzer.Tests.Security
{
    public class ConnectionStringValidatorTests
    {
        private readonly ConnectionStringValidator _validator;

        public ConnectionStringValidatorTests()
        {
            _validator = new ConnectionStringValidator();
        }

        [Theory]
        [InlineData("Server=localhost;Database=TestDB;User Id=sa;Password=Test123!;", true)]
        [InlineData("Server=localhost;Database=TestDB;Integrated Security=true;", true)]
        [InlineData("Host=localhost;Database=testdb;Username=postgres;Password=Test123!;", true)]
        [InlineData("Server=localhost;Database=testdb;User=root;Password=Test123!;", true)]
        public void Validate_WithValidConnectionStrings_ShouldReturnSuccess(string connectionString, bool expected)
        {
            // Act
            var result = _validator.Validate(connectionString);

            // Assert
            result.IsValid.Should().Be(expected);
            result.Errors.Should().BeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_WithNullOrEmptyString_ShouldReturnError(string connectionString)
        {
            // Act
            var result = _validator.Validate(connectionString);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == "EMPTY_CONNECTION_STRING");
        }

        [Theory]
        [InlineData("Server=localhost;Database=TestDB;User Id=sa;Password=;")]
        [InlineData("Server=localhost;Database=TestDB;User Id=sa;")]
        [InlineData("Server=localhost;Database=TestDB;User Id=;Password=Test123!;")]
        public void Validate_WithMissingCredentials_ShouldReturnWarning(string connectionString)
        {
            // Act
            var result = _validator.Validate(connectionString);

            // Assert
            result.IsValid.Should().BeTrue(); // Still valid but with warnings
            result.Warnings.Should().NotBeEmpty();
            result.Warnings.Should().Contain(w => w.Code == "MISSING_CREDENTIALS" || w.Code == "EMPTY_PASSWORD");
        }

        [Theory]
        [InlineData("Server=localhost;Database=TestDB;User Id=sa;Password=password;")]
        [InlineData("Server=localhost;Database=TestDB;User Id=sa;Password=123456;")]
        [InlineData("Server=localhost;Database=TestDB;User Id=sa;Password=admin;")]
        [InlineData("Server=localhost;Database=TestDB;User Id=sa;Password=sa;")]
        public void Validate_WithWeakPasswords_ShouldReturnWarning(string connectionString)
        {
            // Act
            var result = _validator.Validate(connectionString);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Warnings.Should().Contain(w => w.Code == "WEAK_PASSWORD");
        }

        [Theory]
        [InlineData("Server=localhost;Database=TestDB;User Id=sa;Password=Test123!;MultipleActiveResultSets=true;", "MARS_ENABLED")]
        [InlineData("Server=localhost;Database=TestDB;User Id=sa;Password=Test123!;Connect Timeout=300;", "HIGH_TIMEOUT")]
        [InlineData("Server=localhost;Database=TestDB;User Id=sa;Password=Test123!;TrustServerCertificate=false;", null)]
        public void Validate_WithSecuritySettings_ShouldCheckProperly(string connectionString, string expectedWarningCode)
        {
            // Act
            var result = _validator.Validate(connectionString);

            // Assert
            result.IsValid.Should().BeTrue();
            if (expectedWarningCode != null)
            {
                result.Warnings.Should().Contain(w => w.Code == expectedWarningCode);
            }
        }

        [Fact]
        public void Validate_WithWindowsAuthentication_ShouldNotRequirePassword()
        {
            // Arrange
            var connectionString = "Server=localhost;Database=TestDB;Integrated Security=true;";

            // Act
            var result = _validator.Validate(connectionString);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Warnings.Should().BeEmpty();
            result.SecurityLevel.Should().Be(SecurityLevel.High);
        }

        [Fact]
        public void CheckForSensitiveData_ShouldDetectCommonPatterns()
        {
            // Arrange
            var connectionStrings = new Dictionary<string, string>
            {
                { "Server=prod-server;Database=ProductionDB;User Id=admin;Password=Prod123!;", "PRODUCTION_INDICATOR" },
                { "Server=localhost;Database=TestDB;User Id=sa;Password=MySecretKey123;", "POSSIBLE_SECRET" },
                { "Server=localhost;Database=TestDB;User Id=sa;Password=ghp_1234567890abcdef;", "POSSIBLE_TOKEN" }
            };

            foreach (var kvp in connectionStrings)
            {
                // Act
                var result = _validator.Validate(kvp.Key);

                // Assert
                result.Warnings.Should().Contain(w => w.Code == kvp.Value);
            }
        }

        [Fact]
        public void GetDatabaseType_ShouldIdentifyCorrectly()
        {
            // Arrange
            var testCases = new Dictionary<string, SqlAnalyzer.Core.Security.DatabaseType>
            {
                { "Server=localhost;Database=TestDB;User Id=sa;Password=Test123!;", SqlAnalyzer.Core.Security.DatabaseType.SqlServer },
                { "Host=localhost;Database=testdb;Username=postgres;Password=Test123!;", SqlAnalyzer.Core.Security.DatabaseType.PostgreSql },
                { "Server=localhost;Database=testdb;User=root;Password=Test123!;", SqlAnalyzer.Core.Security.DatabaseType.MySql }
            };

            foreach (var testCase in testCases)
            {
                // Act
                var result = _validator.Validate(testCase.Key);

                // Assert
                result.DatabaseType.Should().Be(testCase.Value);
            }
        }

        [Fact]
        public void GetRecommendations_ShouldProvideSecuritySuggestions()
        {
            // Arrange
            var connectionString = "Server=localhost;Database=TestDB;User Id=sa;Password=password;";

            // Act
            var result = _validator.Validate(connectionString);
            var recommendations = result.GetRecommendations();

            // Assert
            recommendations.Should().NotBeEmpty();
            recommendations.Should().Contain(r => r.Contains("strong password"));
            recommendations.Should().Contain(r => r.Contains("Windows Authentication"));
        }

        [Theory]
        [InlineData("Server=localhost;Database=TestDB;Integrated Security=true;", AuthenticationType.Windows)]
        [InlineData("Server=localhost;Database=TestDB;Integrated Security=SSPI;", AuthenticationType.Windows)]
        [InlineData("Server=localhost;Database=TestDB;Integrated Security=yes;", AuthenticationType.Windows)]
        [InlineData("Server=localhost;Database=TestDB;User Id=sa;Password=pass;", AuthenticationType.SqlServer)]
        [InlineData("Server=localhost;Database=TestDB;", AuthenticationType.Unknown)]
        public void Validate_ShouldDetectAuthenticationType(string connectionString, AuthenticationType expected)
        {
            // Act
            var result = _validator.Validate(connectionString);

            // Assert
            result.AuthenticationType.Should().Be(expected);
        }

        [Fact]
        public void Validate_WithWindowsAuthAndCredentials_ShouldWarnAboutMixedAuth()
        {
            // Arrange
            var connectionString = "Server=localhost;Database=TestDB;Integrated Security=true;User Id=sa;Password=pass;";

            // Act
            var result = _validator.Validate(connectionString);

            // Assert
            result.AuthenticationType.Should().Be(AuthenticationType.Windows);
            result.Warnings.Should().Contain(w => w.Code == "MIXED_AUTHENTICATION");
        }

        [Fact]
        public void Validate_WithWindowsAuthForNonSqlServer_ShouldWarn()
        {
            // Arrange
            var connectionString = "Host=localhost;Database=TestDB;Port=5432;Integrated Security=true;";

            // Act
            var result = _validator.Validate(connectionString);

            // Assert
            result.DatabaseType.Should().Be(SqlAnalyzer.Core.Security.DatabaseType.PostgreSql);
            result.Warnings.Should().Contain(w => w.Code == "WINDOWS_AUTH_UNSUPPORTED");
        }

        [Fact]
        public void Validate_WithWindowsAuth_ShouldHaveHighSecurityLevel()
        {
            // Arrange
            var connectionString = "Server=localhost;Database=TestDB;Integrated Security=true;";

            // Act
            var result = _validator.Validate(connectionString);

            // Assert
            result.SecurityLevel.Should().Be(SecurityLevel.High);
            result.AuthenticationType.Should().Be(AuthenticationType.Windows);
        }
    }
}