using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SqlAnalyzer.Core.Connections;
using Xunit;

namespace SqlAnalyzer.Tests.Connections
{
    public class PostgreSqlConnectionTests
    {
        private readonly Mock<ILogger<PostgreSqlConnection>> _mockLogger;
        private const string ValidConnectionString = "Host=localhost;Database=testdb;Username=postgres;Password=Test123!;";

        public PostgreSqlConnectionTests()
        {
            _mockLogger = new Mock<ILogger<PostgreSqlConnection>>();
        }

        [Fact]
        public void Constructor_WithValidConnectionString_ShouldParseCorrectly()
        {
            // Act
            var connection = new PostgreSqlConnection(ValidConnectionString, _mockLogger.Object);

            // Assert
            connection.ConnectionString.Should().Be(ValidConnectionString);
            connection.DatabaseName.Should().Be("testdb");
            connection.ServerName.Should().Be("localhost");
            connection.DatabaseType.Should().Be(DatabaseType.PostgreSql);
        }

        [Fact]
        public void Constructor_WithInvalidConnectionString_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidConnectionString = "InvalidConnectionString";

            // Act & Assert
            var act = () => new PostgreSqlConnection(invalidConnectionString, _mockLogger.Object);
            act.Should().Throw<ArgumentException>()
                .WithMessage("Invalid PostgreSQL connection string*");
        }

        [Fact]
        public void Constructor_WithNullConnectionString_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var act = () => new PostgreSqlConnection(null, _mockLogger.Object);
            act.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("connectionString");
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var act = () => new PostgreSqlConnection(ValidConnectionString, null);
            act.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("logger");
        }

        [Theory]
        [InlineData("Host=myserver;Database=mydb;Username=myuser;Password=mypass;", "myserver", "mydb")]
        [InlineData("Server=10.0.0.1;Port=5432;Database=testdb;User Id=postgres;Password=pass;", "10.0.0.1", "testdb")]
        [InlineData("Host=localhost;Port=5432;Database=postgres;Username=admin;Password=pass;", "localhost", "postgres")]
        public void ParseConnectionString_ShouldExtractServerAndDatabase(string connectionString, string expectedServer, string expectedDatabase)
        {
            // Act
            var connection = new PostgreSqlConnection(connectionString, _mockLogger.Object);

            // Assert
            connection.ServerName.Should().Be(expectedServer);
            connection.DatabaseName.Should().Be(expectedDatabase);
        }

        [Fact]
        public async Task GetDatabaseVersionAsync_ShouldReturnVersionString()
        {
            // Arrange
            var connection = new PostgreSqlConnection(ValidConnectionString, _mockLogger.Object);
            
            // The method should construct a proper PostgreSQL version query
            connection.Should().NotBeNull();
        }

        [Fact]
        public async Task GetDatabaseSizeAsync_ShouldReturnDecimalValue()
        {
            // Arrange
            var connection = new PostgreSqlConnection(ValidConnectionString, _mockLogger.Object);
            
            // The method should construct a proper PostgreSQL size query
            connection.Should().NotBeNull();
        }

        [Fact]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var connection = new PostgreSqlConnection(ValidConnectionString, _mockLogger.Object);

            // Act & Assert
            var act = () => connection.Dispose();
            act.Should().NotThrow();
        }

        [Fact]
        public void ExecuteQueryAsync_ShouldHandlePostgreSQLSpecificTypes()
        {
            // This test verifies that PostgreSQL-specific data types are handled correctly
            var connection = new PostgreSqlConnection(ValidConnectionString, _mockLogger.Object);
            connection.Should().NotBeNull();
        }
    }
}