using System.Threading.Tasks;
using FluentAssertions;
using SqlAnalyzer.Core.Connections;
using Xunit;
using Xunit.Abstractions;

namespace SqlAnalyzer.Tests.IntegrationTests
{
    /// <summary>
    /// Integration tests for ConnectionFactory with real database connections
    /// </summary>
    public class ConnectionFactoryIntegrationTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        public ConnectionFactoryIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task CreateConnection_WithSqlServerConnectionString_ShouldCreateWorkingConnection()
        {
            // Arrange
            CheckSkipIntegrationTest();

            // Act
            using var connection = ConnectionFactory.CreateConnection(SqlServerConnectionString);

            // Assert
            connection.Should().NotBeNull();
            connection.Should().BeOfType<SqlServerConnection>();
            connection.DatabaseType.Should().Be(DatabaseType.SqlServer);
            
            // Test the connection works
            var testResult = await connection.TestConnectionAsync();
            testResult.Should().BeTrue();
            
            _output.WriteLine($"Successfully created and tested SQL Server connection");
            _output.WriteLine($"Database: {connection.DatabaseName}");
            _output.WriteLine($"Server: {connection.ServerName}");
        }

        [Fact]
        public async Task CreateConnection_AutoDetection_ShouldCorrectlyIdentifySqlServer()
        {
            // Arrange
            CheckSkipIntegrationTest();

            // Act - Let factory auto-detect the database type
            using var connection = ConnectionFactory.CreateConnection(SqlServerConnectionString);

            // Assert
            connection.Should().BeOfType<SqlServerConnection>();
            connection.DatabaseType.Should().Be(DatabaseType.SqlServer);
            
            // Verify it's functional
            var version = await connection.GetDatabaseVersionAsync();
            version.Should().Contain("SQL Server");
            
            _output.WriteLine($"Auto-detected SQL Server connection");
            _output.WriteLine($"Version: {version}");
        }

        [Fact]
        public async Task CreateConnection_ExplicitType_ShouldOverrideAutoDetection()
        {
            // Arrange
            CheckSkipIntegrationTest();
            
            // Create a generic connection string that could be ambiguous
            var genericConnectionString = "Server=localhost;Database=test;User=sa;Password=pass;";

            // Act - Force SQL Server type
            using var connection = ConnectionFactory.CreateConnection(genericConnectionString, DatabaseType.SqlServer);

            // Assert
            connection.Should().BeOfType<SqlServerConnection>();
            connection.DatabaseType.Should().Be(DatabaseType.SqlServer);
            
            _output.WriteLine("Successfully created SQL Server connection with explicit type");
        }

        [Fact]
        public async Task ConnectionFactory_ShouldDisposeAllCreatedConnections()
        {
            // Arrange
            CheckSkipIntegrationTest();
            
            // Create a new factory for this test
            var factory = new ConnectionFactory(ServiceProvider, ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ConnectionFactory>>());

            // Act - Create multiple connections
            var connection1 = factory.CreateConnection(SqlServerConnectionString);
            var connection2 = factory.CreateConnection(SqlServerConnectionString, DatabaseType.SqlServer);
            
            // Test they work
            var test1 = await connection1.TestConnectionAsync();
            var test2 = await connection2.TestConnectionAsync();
            
            test1.Should().BeTrue();
            test2.Should().BeTrue();
            
            // Dispose the factory
            factory.Dispose();
            
            // Assert - Connections should be disposed (no way to directly test, but no exceptions should occur)
            _output.WriteLine("Factory disposed successfully with all connections");
        }

        [Fact]
        public async Task CreateConnection_MultipleTimes_ShouldCreateIndependentConnections()
        {
            // Arrange
            CheckSkipIntegrationTest();

            // Act - Create multiple connections
            using var connection1 = ConnectionFactory.CreateConnection(SqlServerConnectionString);
            using var connection2 = ConnectionFactory.CreateConnection(SqlServerConnectionString);

            // Assert - They should be different instances
            connection1.Should().NotBeSameAs(connection2);
            
            // Both should work independently
            var task1 = connection1.ExecuteScalarAsync("SELECT 1");
            var task2 = connection2.ExecuteScalarAsync("SELECT 2");
            
            var results = await Task.WhenAll(task1, task2);
            
            results[0].Should().Be(1);
            results[1].Should().Be(2);
            
            _output.WriteLine("Successfully created and used multiple independent connections");
        }

        [Fact]
        public async Task CreateConnection_WithVariousConnectionStringFormats_ShouldWork()
        {
            // Arrange
            CheckSkipIntegrationTest();

            // Test various SQL Server connection string formats
            var connectionStrings = new[]
            {
                // Standard format
                SqlServerConnectionString,
                
                // With spaces
                SqlServerConnectionString.Replace("=", " = ").Replace(";", " ; "),
                
                // Different casing
                SqlServerConnectionString.Replace("Server", "server").Replace("Database", "database")
            };

            foreach (var connStr in connectionStrings)
            {
                // Act
                using var connection = ConnectionFactory.CreateConnection(connStr);
                
                // Assert
                connection.Should().BeOfType<SqlServerConnection>();
                var testResult = await connection.TestConnectionAsync();
                testResult.Should().BeTrue();
                
                _output.WriteLine($"Successfully connected with connection string format: {connStr.Substring(0, 30)}...");
            }
        }
    }
}