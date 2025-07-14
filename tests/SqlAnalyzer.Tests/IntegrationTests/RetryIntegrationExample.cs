using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using SqlAnalyzer.Core.Connections;
using SqlAnalyzer.Tests.TestFramework;
using Xunit;
using Xunit.Abstractions;

namespace SqlAnalyzer.Tests.IntegrationTests
{
    /// <summary>
    /// Example integration tests demonstrating the RetryAttribute usage
    /// </summary>
    [Trait("Category", "Integration")]
    public class RetryIntegrationExample : IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly IConnectionFactory _connectionFactory;
        private readonly ITestOutputHelper _output;
        private ISqlAnalyzerConnection _connection;

        public RetryIntegrationExample(ITestOutputHelper output)
        {
            _output = output;
            _configuration = TestConfiguration.BuildConfiguration();
            _connectionFactory = new ConnectionFactory();
        }

        [Fact]
        [Retry(3, DelayMilliseconds = 2000)]
        [Trait("Retry", "Example")]
        public async Task ConnectionTest_WithRetry_ShouldHandleTransientFailures()
        {
            // This test will be retried up to 3 times with 2-second delays
            var connectionString = _configuration["SQLSERVER_TEST_CONNECTION"];
            
            if (string.IsNullOrEmpty(connectionString))
            {
                _output.WriteLine("Skipping test - no connection string configured");
                return;
            }

            // Act
            _connection = _connectionFactory.CreateConnection(connectionString);
            await _connection.OpenAsync();

            // Assert
            _connection.State.Should().Be(System.Data.ConnectionState.Open);
        }

        [Fact]
        [Retry(5, DelayMilliseconds = 500, UseExponentialBackoff = true)]
        [Trait("Retry", "Exponential")]
        public async Task HeavyQueryTest_WithExponentialBackoff_ShouldCompleteEventually()
        {
            // This test uses exponential backoff: 500ms, 1s, 2s, 4s, 8s
            var connectionString = _configuration["SQLSERVER_TEST_CONNECTION"];
            
            if (string.IsNullOrEmpty(connectionString))
            {
                _output.WriteLine("Skipping test - no connection string configured");
                return;
            }

            // Arrange
            _connection = _connectionFactory.CreateConnection(connectionString);
            await _connection.OpenAsync();

            // Act - Query that might timeout or fail transiently
            var query = @"
                SELECT COUNT(*) as TableCount
                FROM sys.tables
                WHERE type = 'U'";

            var result = await _connection.ExecuteScalarAsync<int>(query);

            // Assert
            result.Should().BeGreaterThanOrEqualTo(0);
        }

        [Theory]
        [InlineData("sys.tables")]
        [InlineData("sys.columns")]
        [InlineData("sys.indexes")]
        [Retry(3, RetryableExceptions = new[] { typeof(TimeoutException), typeof(InvalidOperationException) })]
        [Trait("Retry", "Selective")]
        public async Task SystemTableQuery_WithSelectiveRetry_ShouldOnlyRetrySpecificExceptions(string tableName)
        {
            // This test only retries TimeoutException and InvalidOperationException
            var connectionString = _configuration["SQLSERVER_TEST_CONNECTION"];
            
            if (string.IsNullOrEmpty(connectionString))
            {
                _output.WriteLine("Skipping test - no connection string configured");
                return;
            }

            // Arrange
            _connection = _connectionFactory.CreateConnection(connectionString);
            await _connection.OpenAsync();

            // Act
            var query = $@"
                SELECT COUNT(*) 
                FROM {tableName}";

            var result = await _connection.ExecuteScalarAsync<int>(query);

            // Assert
            result.Should().BeGreaterThanOrEqualTo(0);
            _output.WriteLine($"Found {result} records in {tableName}");
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}