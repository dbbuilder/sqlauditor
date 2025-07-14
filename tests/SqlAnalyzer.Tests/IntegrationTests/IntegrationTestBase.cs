using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetEnv;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlAnalyzer.Core.Configuration;
using SqlAnalyzer.Core.Connections;
using Xunit;

namespace SqlAnalyzer.Tests.IntegrationTests
{
    /// <summary>
    /// Base class for integration tests that require database connections
    /// </summary>
    public abstract class IntegrationTestBase : IAsyncLifetime, IDisposable
    {
        protected IServiceProvider ServiceProvider;
        protected IConnectionFactory ConnectionFactory;
        protected ISecureConfigurationProvider ConfigurationProvider;
        protected string SqlServerConnectionString;
        protected bool SkipIntegrationTests { get; private set; }
        protected readonly List<ISqlAnalyzerConnection> _activeConnections = new();

        protected IntegrationTestBase()
        {
            // Constructor is now empty - initialization moved to InitializeAsync
        }

        /// <summary>
        /// Initializes the test asynchronously
        /// </summary>
        public virtual async Task InitializeAsync()
        {
            // Load environment variables from .env file
            Env.Load();

            // Create secure configuration provider
            ConfigurationProvider = ConfigurationProviderFactory.CreateDefault();

            // Check if integration tests should run
            var runIntegrationTests = await ConfigurationProvider.GetValueAsync("RUN_INTEGRATION_TESTS");
            SkipIntegrationTests = runIntegrationTests?.ToLower() != "true";

            // Get connection string using secure provider
            SqlServerConnectionString = await ConfigurationProvider.GetConnectionStringAsync("SQLSERVER_TEST");

            // Setup dependency injection
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Register connection factory
            services.AddSingleton<IConnectionFactory, ConnectionFactory>();
            
            ServiceProvider = services.BuildServiceProvider();
            ConnectionFactory = ServiceProvider.GetRequiredService<IConnectionFactory>();

            // Allow derived classes to perform additional async initialization
            await OnInitializeAsync();
        }

        /// <summary>
        /// Override this method to perform additional async initialization
        /// </summary>
        protected virtual Task OnInitializeAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Creates a SQL Server connection for testing
        /// </summary>
        /// <returns>SQL Server connection (read-only)</returns>
        protected ISqlAnalyzerConnection CreateSqlServerConnection()
        {
            if (string.IsNullOrEmpty(SqlServerConnectionString))
            {
                throw new InvalidOperationException(
                    "SQL Server connection string not found. " +
                    "Please ensure SQLSERVER_TEST_CONNECTION is set in .env file");
            }

            var connection = ConnectionFactory.CreateConnection(SqlServerConnectionString, DatabaseType.SqlServer);
            _activeConnections.Add(connection);
            return connection;
        }

        /// <summary>
        /// Checks if integration tests should be skipped
        /// </summary>
        protected void CheckSkipIntegrationTest()
        {
            if (SkipIntegrationTests)
            {
                throw new SkipException(
                    "Integration tests are disabled. " +
                    "Set RUN_INTEGRATION_TESTS=true in .env file to enable");
            }
        }

        /// <summary>
        /// Disposes resources asynchronously
        /// </summary>
        public virtual async Task DisposeAsync()
        {
            // Close all active connections
            foreach (var connection in _activeConnections)
            {
                try
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        await connection.CloseAsync();
                    }
                    connection.Dispose();
                }
                catch
                {
                    // Ignore errors during cleanup
                }
            }
            _activeConnections.Clear();

            // Allow derived classes to perform additional async cleanup
            await OnDisposeAsync();

            // Dispose synchronous resources
            Dispose();
        }

        /// <summary>
        /// Override this method to perform additional async cleanup
        /// </summary>
        protected virtual Task OnDisposeAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ConnectionFactory?.Dispose();
                if (ServiceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Custom exception for skipping tests
    /// </summary>
    public class SkipException : Exception
    {
        public SkipException(string message) : base(message) { }
    }
}