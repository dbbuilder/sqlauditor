using System;
using System.Threading.Tasks;
using DotNetEnv;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlAnalyzer.Core.Connections;
using SqlAnalyzer.Core.Analyzers;
using Xunit;
using Xunit.Abstractions;

namespace SqlAnalyzer.Tests.IntegrationTests
{
    /// <summary>
    /// Basic integration test to verify SQL Server connection
    /// </summary>
    [Collection("IntegrationTests")]
    public class BasicIntegrationTest : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnectionFactory _connectionFactory;
        private readonly string _connectionString;
        private ISqlAnalyzerConnection _connection;

        public BasicIntegrationTest(ITestOutputHelper output)
        {
            _output = output;
            
            // Load environment variables from .env file
            Env.Load();
            
            // Get connection string from environment
            _connectionString = Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION");
            
            // Setup dependency injection
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            services.AddSingleton<IConnectionFactory, ConnectionFactory>();
            
            _serviceProvider = services.BuildServiceProvider();
            _connectionFactory = _serviceProvider.GetRequiredService<IConnectionFactory>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task TestSqlServerConnection()
        {
            // Arrange
            if (string.IsNullOrEmpty(_connectionString))
            {
                _output.WriteLine("Skipping test - no connection string configured");
                return;
            }

            _output.WriteLine($"Testing connection to SQL Server...");
            
            // Act
            _connection = _connectionFactory.CreateConnection(_connectionString, DatabaseType.SqlServer);
            await _connection.OpenAsync();
            
            // Assert
            Assert.Equal(System.Data.ConnectionState.Open, _connection.State);
            _output.WriteLine($"Successfully connected to database: {_connection.DatabaseName}");
            
            // Test a simple query
            var result = await _connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sys.tables WHERE type = 'U'");
            _output.WriteLine($"Found {result} user tables in the database");
            
            Assert.True(result >= 0);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task TestTableAnalyzer()
        {
            // Arrange
            if (string.IsNullOrEmpty(_connectionString))
            {
                _output.WriteLine("Skipping test - no connection string configured");
                return;
            }

            _connection = _connectionFactory.CreateConnection(_connectionString, DatabaseType.SqlServer);
            var logger = _serviceProvider.GetRequiredService<ILogger<TableAnalyzer>>();
            var analyzer = new TableAnalyzer(_connection, logger);
            
            // Act
            var result = await analyzer.AnalyzeAsync();
            
            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal("Table Analyzer", result.AnalyzerName);
            
            _output.WriteLine($"Analysis completed for database: {result.DatabaseName}");
            _output.WriteLine($"Total tables analyzed: {result.Summary.TotalObjectsAnalyzed}");
            _output.WriteLine($"Total findings: {result.Summary.TotalFindings}");
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}