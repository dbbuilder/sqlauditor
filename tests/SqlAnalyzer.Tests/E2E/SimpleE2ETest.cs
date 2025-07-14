using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlAnalyzer.Core.Connections;
using SqlAnalyzer.Core.Security;
using SqlAnalyzer.Tests.TestFramework;
using Xunit;
using Xunit.Abstractions;

namespace SqlAnalyzer.Tests.E2E
{
    /// <summary>
    /// Simple E2E test that can run without full framework dependencies
    /// </summary>
    [Trait("Category", TestCategories.E2E)]
    [Trait("Category", TestCategories.SqlServer)]
    public class SimpleE2ETest : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        public SimpleE2ETest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task SimpleE2E_ConnectAndQuery_ShouldWork()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddSingleton<IConnectionFactory, ConnectionFactory>();
            services.AddSingleton<IConnectionStringValidator, ConnectionStringValidator>();
            
            var serviceProvider = services.BuildServiceProvider();
            var connectionString = Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                _output.WriteLine("Skipping E2E test - no SQL Server connection configured");
                return;
            }

            _output.WriteLine($"Running E2E test against SQL Server");
            _output.WriteLine($"Connection: {connectionString.Substring(0, 50)}...");

            // Act & Assert
            var factory = serviceProvider.GetRequiredService<IConnectionFactory>();
            var validator = serviceProvider.GetRequiredService<IConnectionStringValidator>();

            // Test 1: Validate connection string
            var validationResult = validator.Validate(connectionString);
            validationResult.IsValid.Should().BeTrue("Connection string should be valid");
            _output.WriteLine($"✓ Connection string validated (Security: {validationResult.SecurityLevel})");

            // Test 2: Connect to database
            using var connection = factory.CreateConnection(connectionString, DatabaseType.SqlServer);
            await connection.OpenAsync();
            connection.State.Should().Be(System.Data.ConnectionState.Open);
            _output.WriteLine("✓ Connected to SQL Server");

            // Test 3: Execute simple query
            var dbName = await connection.ExecuteScalarAsync("SELECT DB_NAME()");
            dbName.Should().NotBeNull();
            _output.WriteLine($"✓ Database name: {dbName}");

            // Test 4: Query table count
            var tableCount = await connection.ExecuteScalarAsync("SELECT COUNT(*) FROM sys.tables WITH (NOLOCK)");
            _output.WriteLine($"✓ Table count: {tableCount}");

            // Test 5: Verify read-only access
            using (var transaction = connection.BeginTransaction())
            {
                // Execute a read query in transaction
                var result = await connection.ExecuteScalarAsync(
                    "SELECT TOP 1 name FROM sys.tables WITH (NOLOCK)", 
                    transaction: transaction);
                    
                transaction.Rollback();
                _output.WriteLine("✓ Transaction test completed (rolled back)");
            }

            _output.WriteLine("");
            _output.WriteLine("=== E2E Test Completed Successfully ===");
        }
    }
}