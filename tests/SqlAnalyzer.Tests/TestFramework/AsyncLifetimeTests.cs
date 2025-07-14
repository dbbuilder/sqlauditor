using System;
using System.Threading.Tasks;
using FluentAssertions;
using SqlAnalyzer.Tests.IntegrationTests;
using Xunit;
using Xunit.Abstractions;

namespace SqlAnalyzer.Tests.TestFramework
{
    /// <summary>
    /// Tests for IAsyncLifetime implementation in IntegrationTestBase
    /// </summary>
    public class AsyncLifetimeTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;
        private bool _initializeCalled = false;
        private bool _disposeCalled = false;

        public AsyncLifetimeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        protected override async Task OnInitializeAsync()
        {
            _output.WriteLine("OnInitializeAsync called");
            _initializeCalled = true;
            
            // Simulate async initialization work
            await Task.Delay(100);
            
            await base.OnInitializeAsync();
        }

        protected override async Task OnDisposeAsync()
        {
            _output.WriteLine("OnDisposeAsync called");
            _disposeCalled = true;
            
            // Simulate async cleanup work
            await Task.Delay(50);
            
            await base.OnDisposeAsync();
        }

        [Fact]
        public void AsyncLifetime_ShouldInitializeBeforeTest()
        {
            // Assert - InitializeAsync should have been called
            _initializeCalled.Should().BeTrue();
            _output.WriteLine("Test executing - initialization verified");
        }

        [Fact]
        public void ServiceProvider_ShouldBeInitialized()
        {
            // Assert
            ServiceProvider.Should().NotBeNull();
            ConnectionFactory.Should().NotBeNull();
        }

        [Fact]
        public async Task ConnectionCreation_ShouldTrackConnections()
        {
            // Skip if integration tests are disabled
            if (SkipIntegrationTests)
            {
                _output.WriteLine("Skipping integration test");
                return;
            }

            // Arrange & Act
            var connection = CreateSqlServerConnection();
            await connection.OpenAsync();

            // Assert
            _activeConnections.Should().Contain(connection);
            connection.State.Should().Be(System.Data.ConnectionState.Open);

            // Cleanup will happen in DisposeAsync
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public async Task MultipleTests_ShouldEachHaveCleanState(int testNumber)
        {
            _output.WriteLine($"Running test {testNumber}");
            
            // Each test should have a fresh state
            _activeConnections.Should().BeEmpty();
            
            // Simulate some test work
            await Task.Delay(50);
            
            _output.WriteLine($"Test {testNumber} completed");
        }
    }

    /// <summary>
    /// Example of a test class using async lifetime for database setup
    /// </summary>
    [Trait("Category", "AsyncLifetime")]
    public class DatabaseSetupAsyncLifetimeExample : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;
        private bool _databasePrepared = false;

        public DatabaseSetupAsyncLifetimeExample(ITestOutputHelper output)
        {
            _output = output;
        }

        protected override async Task OnInitializeAsync()
        {
            await base.OnInitializeAsync();

            if (!SkipIntegrationTests && !string.IsNullOrEmpty(SqlServerConnectionString))
            {
                // Prepare database state
                _output.WriteLine("Preparing database for tests...");
                
                using var connection = CreateSqlServerConnection();
                await connection.OpenAsync();
                
                // Verify we can query the database
                var query = "SELECT COUNT(*) FROM sys.tables WHERE type = 'U'";
                var tableCount = await connection.ExecuteScalarAsync<int>(query);
                
                _output.WriteLine($"Database has {tableCount} user tables");
                _databasePrepared = true;
            }
        }

        protected override async Task OnDisposeAsync()
        {
            if (_databasePrepared)
            {
                _output.WriteLine("Cleaning up database state...");
                // Any cleanup logic would go here
                await Task.Delay(50); // Simulate cleanup
            }

            await base.OnDisposeAsync();
        }

        [Fact]
        public void Database_ShouldBePrepared()
        {
            if (!SkipIntegrationTests)
            {
                _databasePrepared.Should().BeTrue();
            }
        }
    }
}