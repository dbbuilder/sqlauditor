using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace SqlAnalyzer.Tests.TestFramework
{
    /// <summary>
    /// Provides configuration for tests
    /// </summary>
    public static class TestConfiguration
    {
        private static readonly Lazy<IConfiguration> _configuration = new Lazy<IConfiguration>(BuildConfiguration);

        /// <summary>
        /// Gets the test configuration
        /// </summary>
        public static IConfiguration Configuration => _configuration.Value;

        /// <summary>
        /// Gets whether to run integration tests
        /// </summary>
        public static bool RunIntegrationTests => 
            Configuration.GetValue<bool>("RunIntegrationTests", false) ||
            Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS") == "true";

        /// <summary>
        /// Gets whether to run external tests
        /// </summary>
        public static bool RunExternalTests => 
            Configuration.GetValue<bool>("RunExternalTests", false) ||
            Environment.GetEnvironmentVariable("RUN_EXTERNAL_TESTS") == "true";

        /// <summary>
        /// Gets the SQL Server test connection string
        /// </summary>
        public static string? SqlServerConnectionString =>
            Configuration.GetConnectionString("SqlServerTest") ??
            Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION");

        /// <summary>
        /// Gets the PostgreSQL test connection string
        /// </summary>
        public static string? PostgreSqlConnectionString =>
            Configuration.GetConnectionString("PostgreSqlTest") ??
            Environment.GetEnvironmentVariable("POSTGRESQL_TEST_CONNECTION");

        /// <summary>
        /// Gets the MySQL test connection string
        /// </summary>
        public static string? MySqlConnectionString =>
            Configuration.GetConnectionString("MySqlTest") ??
            Environment.GetEnvironmentVariable("MYSQL_TEST_CONNECTION");

        /// <summary>
        /// Gets the test timeout in seconds
        /// </summary>
        public static int TestTimeoutSeconds =>
            Configuration.GetValue<int>("TestTimeoutSeconds", 30);

        /// <summary>
        /// Gets the maximum parallel threads for tests
        /// </summary>
        public static int MaxParallelThreads =>
            Configuration.GetValue<int>("MaxParallelThreads", 4);

        /// <summary>
        /// Gets whether to enable diagnostic messages
        /// </summary>
        public static bool EnableDiagnostics =>
            Configuration.GetValue<bool>("EnableDiagnostics", false);

        /// <summary>
        /// Checks if a specific test category should run
        /// </summary>
        public static bool ShouldRunCategory(string category)
        {
            // Check specific environment variables
            var envVar = $"RUN_{category.ToUpperInvariant()}_TESTS";
            if (Environment.GetEnvironmentVariable(envVar) == "false")
                return false;

            // Check configuration
            var configKey = $"RunTests:{category}";
            return Configuration.GetValue<bool>(configKey, true);
        }

        /// <summary>
        /// Gets test data directory
        /// </summary>
        public static string TestDataDirectory
        {
            get
            {
                var dir = Configuration.GetValue<string>("TestDataDirectory") ??
                         Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
                
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                    
                return dir;
            }
        }

        private static IConfiguration BuildConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Test.json", optional: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
                .AddEnvironmentVariables()
                .AddInMemoryCollection(new[]
                {
                    // Default test settings
                    new KeyValuePair<string, string?>("RunIntegrationTests", "false"),
                    new KeyValuePair<string, string?>("RunExternalTests", "false"),
                    new KeyValuePair<string, string?>("TestTimeoutSeconds", "30"),
                    new KeyValuePair<string, string?>("MaxParallelThreads", "4")
                });

            return builder.Build();
        }
    }

    /// <summary>
    /// Base class for tests that require configuration
    /// </summary>
    public abstract class ConfiguredTestBase
    {
        protected IConfiguration Configuration => TestConfiguration.Configuration;

        /// <summary>
        /// Skips test if integration tests are not enabled
        /// </summary>
        protected void RequireIntegrationTests()
        {
            if (!TestConfiguration.RunIntegrationTests)
                throw new SkipException("Integration tests are not enabled");
        }

        /// <summary>
        /// Skips test if external tests are not enabled
        /// </summary>
        protected void RequireExternalTests()
        {
            if (!TestConfiguration.RunExternalTests)
                throw new SkipException("External tests are not enabled");
        }

        /// <summary>
        /// Skips test if specific database is not configured
        /// </summary>
        protected void RequireDatabase(DatabaseType databaseType)
        {
            var connectionString = databaseType switch
            {
                DatabaseType.SqlServer => TestConfiguration.SqlServerConnectionString,
                DatabaseType.PostgreSql => TestConfiguration.PostgreSqlConnectionString,
                DatabaseType.MySql => TestConfiguration.MySqlConnectionString,
                _ => null
            };

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new SkipException($"{databaseType} database is not configured for testing");
        }
    }

    /// <summary>
    /// Custom exception for skipping tests
    /// </summary>
    public class SkipException : Exception
    {
        public SkipException(string reason) : base(reason) { }
    }
}