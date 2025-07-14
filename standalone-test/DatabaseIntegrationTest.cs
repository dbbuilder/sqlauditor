using System;
using System.Threading.Tasks;
using DotNetEnv;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlAnalyzer.Core.Analyzers;
using SqlAnalyzer.Core.Connections;
using Xunit;
using Xunit.Abstractions;

namespace StandaloneTest
{
    public class DatabaseIntegrationTest : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnectionFactory _connectionFactory;
        private ISqlAnalyzerConnection? _connection;

        public DatabaseIntegrationTest(ITestOutputHelper output)
        {
            _output = output;
            
            // Load environment variables
            Env.Load();
            
            // Setup DI
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            services.AddSingleton<IConnectionFactory, ConnectionFactory>();
            
            _serviceProvider = services.BuildServiceProvider();
            _connectionFactory = _serviceProvider.GetRequiredService<IConnectionFactory>();
        }

        [Fact]
        public async Task TestSqlServerConnection()
        {
            // Arrange
            var connectionString = Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION");
            if (string.IsNullOrEmpty(connectionString))
            {
                _output.WriteLine("Skipping test - no connection string configured");
                return;
            }

            // Act
            _connection = _connectionFactory.CreateConnection(connectionString, DatabaseType.SqlServer);
            await _connection.OpenAsync();
            
            // Assert
            _connection.DatabaseName.Should().NotBeNullOrEmpty();
            
            var tableCountObj = await _connection.ExecuteScalarAsync("SELECT COUNT(*) FROM sys.tables WHERE type = 'U'");
            var tableCount = Convert.ToInt32(tableCountObj);
            tableCount.Should().BeGreaterThanOrEqualTo(0);
            
            _output.WriteLine($"Connected to: {_connection.DatabaseName}");
            _output.WriteLine($"User tables: {tableCount}");
        }

        [Fact]
        public async Task TestTableAnalyzer()
        {
            // Arrange
            var connectionString = Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION");
            if (string.IsNullOrEmpty(connectionString))
            {
                _output.WriteLine("Skipping test - no connection string configured");
                return;
            }

            _connection = _connectionFactory.CreateConnection(connectionString, DatabaseType.SqlServer);
            await _connection.OpenAsync(); // Need to open connection first!
            
            var logger = _serviceProvider.GetRequiredService<ILogger<TableAnalyzer>>();
            var analyzer = new TableAnalyzer(_connection, logger);
            
            // Act
            var result = await analyzer.AnalyzeAsync();
            
            // Assert
            result.Should().NotBeNull();
            
            if (!result.Success)
            {
                _output.WriteLine($"Analysis failed: {result.ErrorMessage}");
            }
            
            result.Success.Should().BeTrue();
            result.DatabaseName.Should().NotBeNullOrEmpty();
            result.Summary.Should().NotBeNull();
            
            _output.WriteLine($"Analyzed {result.Summary.TotalObjectsAnalyzed} tables");
            _output.WriteLine($"Found {result.Summary.TotalFindings} findings");
            
            foreach (var finding in result.Findings.Take(5))
            {
                _output.WriteLine($"- {finding.Severity}: {finding.Message}");
            }
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}