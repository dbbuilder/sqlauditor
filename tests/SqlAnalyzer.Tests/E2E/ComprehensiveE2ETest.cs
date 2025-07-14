using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlAnalyzer.Core.Analyzers;
using SqlAnalyzer.Core.Caching;
using SqlAnalyzer.Core.Configuration;
using SqlAnalyzer.Core.Connections;
using SqlAnalyzer.Core.Optimization;
using SqlAnalyzer.Core.Resilience;
using SqlAnalyzer.Core.Security;
using SqlAnalyzer.Tests.TestFramework;
using Xunit;
using Xunit.Abstractions;

namespace SqlAnalyzer.Tests.E2E
{
    /// <summary>
    /// Comprehensive end-to-end test demonstrating all SQL Analyzer features
    /// </summary>
    [Collection("Non-Parallel Tests")]
    [Trait("Category", TestCategories.E2E)]
    [Trait("Category", TestCategories.Integration)]
    [Trait("Category", TestCategories.SqlServer)]
    [Trait(TestTraits.Priority.TraitName, TestTraits.Priority.Critical)]
    public class ComprehensiveE2ETest : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;
        private readonly IServiceProvider _serviceProvider;

        public ComprehensiveE2ETest(ITestOutputHelper output)
        {
            _output = output;
            
            // Build comprehensive service provider with all features
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task FullSystemTest_AllFeatures_ShouldWorkCorrectly()
        {
            _output.WriteLine("=== SQL Analyzer Comprehensive E2E Test ===");
            _output.WriteLine($"Started at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Step 1: Test Security Validation
                await TestSecurityValidation();

                // Step 2: Test Connection Management with Resilience
                await TestConnectionManagement();

                // Step 3: Test Query Optimization
                await TestQueryOptimization();

                // Step 4: Test Database Analysis
                await TestDatabaseAnalysis();

                // Step 5: Test Caching
                await TestCaching();

                // Step 6: Test Circuit Breaker
                await TestCircuitBreaker();

                // Step 7: Test Connection Pool Management
                await TestConnectionPoolManagement();

                // Step 8: Test Configuration Providers
                await TestConfigurationProviders();

                // Step 9: Test Adaptive Timeout
                await TestAdaptiveTimeout();

                // Step 10: Test Full Analysis Pipeline
                await TestFullAnalysisPipeline();

                stopwatch.Stop();
                _output.WriteLine($"\n=== All E2E Tests Completed Successfully ===");
                _output.WriteLine($"Total time: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"\nE2E Test Failed: {ex.Message}");
                _output.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task TestSecurityValidation()
        {
            _output.WriteLine("\n--- Step 1: Testing Security Validation ---");
            
            var validator = _serviceProvider.GetRequiredService<IConnectionStringValidator>();
            
            // Test various connection strings
            var testCases = new[]
            {
                ("Weak Password", "Server=localhost;Database=test;User Id=sa;Password=123;", SecurityLevel.Low),
                ("Strong Password", "Server=localhost;Database=test;User Id=sa;Password=Str0ng!P@ssw0rd#2024;", SecurityLevel.High),
                ("Windows Auth", "Server=localhost;Database=test;Integrated Security=true;", SecurityLevel.High),
                ("Production Server", "Server=prod-sql.company.com;Database=ProductionDB;User Id=app;Password=AppP@ss123!;", SecurityLevel.Medium)
            };

            foreach (var (name, connectionString, expectedLevel) in testCases)
            {
                var result = validator.Validate(connectionString);
                _output.WriteLine($"  {name}: {result.SecurityLevel} (Expected: {expectedLevel})");
                
                if (result.Issues.Any())
                {
                    foreach (var issue in result.Issues)
                    {
                        _output.WriteLine($"    - {issue}");
                    }
                }

                result.IsValid.Should().BeTrue($"{name} should be valid");
            }
            
            _output.WriteLine("  ✓ Security validation completed");
        }

        private async Task TestConnectionManagement()
        {
            _output.WriteLine("\n--- Step 2: Testing Connection Management ---");
            
            var factory = _serviceProvider.GetRequiredService<IConnectionFactory>();
            var connectionString = Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                _output.WriteLine("  ⚠ Skipping - No SQL Server connection configured");
                return;
            }

            using var connection = factory.CreateConnection(connectionString, DatabaseType.SqlServer);
            
            // Test connection with retry policy
            _output.WriteLine("  Testing connection with retry policy...");
            await connection.OpenAsync();
            connection.State.Should().Be(System.Data.ConnectionState.Open);
            
            // Test basic query
            var result = await connection.ExecuteScalarAsync("SELECT DB_NAME()");
            _output.WriteLine($"  Connected to database: {result}");
            
            // Test transaction
            using (var transaction = connection.BeginTransaction())
            {
                await connection.ExecuteScalarAsync("SELECT 1", transaction: transaction);
                transaction.Commit();
                _output.WriteLine("  Transaction test completed");
            }
            
            _output.WriteLine("  ✓ Connection management completed");
        }

        private async Task TestQueryOptimization()
        {
            _output.WriteLine("\n--- Step 3: Testing Query Optimization ---");
            
            var optimizer = _serviceProvider.GetRequiredService<IQueryOptimizer>();
            
            // Test NOLOCK hint
            var selectQuery = "SELECT * FROM sys.tables WHERE name = 'test'";
            var optimizedQuery = optimizer.AddNoLockHint(selectQuery, DatabaseType.SqlServer);
            _output.WriteLine($"  Original: {selectQuery}");
            _output.WriteLine($"  Optimized: {optimizedQuery}");
            optimizedQuery.Should().Contain("WITH (NOLOCK)");
            
            // Test pagination
            var paginatedQuery = optimizer.AddPagination(selectQuery, 10, 20, DatabaseType.SqlServer);
            _output.WriteLine($"  Paginated: {paginatedQuery}");
            paginatedQuery.Should().Contain("OFFSET").And.Contain("FETCH");
            
            _output.WriteLine("  ✓ Query optimization completed");
        }

        private async Task TestDatabaseAnalysis()
        {
            _output.WriteLine("\n--- Step 4: Testing Database Analysis ---");
            
            var connectionString = Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION");
            if (string.IsNullOrEmpty(connectionString))
            {
                _output.WriteLine("  ⚠ Skipping - No SQL Server connection configured");
                return;
            }

            var factory = _serviceProvider.GetRequiredService<IConnectionFactory>();
            using var connection = factory.CreateConnection(connectionString, DatabaseType.SqlServer);
            await connection.OpenAsync();
            
            var analyzer = new TableAnalyzer(connection, _serviceProvider.GetRequiredService<ILogger<TableAnalyzer>>());
            var result = await analyzer.AnalyzeAsync();
            
            _output.WriteLine($"  Analysis completed: {result.Success}");
            _output.WriteLine($"  Database: {result.DatabaseName}");
            _output.WriteLine($"  Tables analyzed: {result.Summary.TotalObjectsAnalyzed}");
            _output.WriteLine($"  Findings: {result.Summary.TotalFindings}");
            
            if (result.Findings.Any())
            {
                _output.WriteLine("  Sample findings:");
                foreach (var finding in result.Findings.Take(3))
                {
                    _output.WriteLine($"    - [{finding.Severity}] {finding.Message}");
                }
            }
            
            _output.WriteLine("  ✓ Database analysis completed");
        }

        private async Task TestCaching()
        {
            _output.WriteLine("\n--- Step 5: Testing Caching ---");
            
            var cache = _serviceProvider.GetRequiredService<IQueryCache>();
            var keyGenerator = _serviceProvider.GetRequiredService<ICacheKeyGenerator>();
            
            // Test cache operations
            var key = keyGenerator.GenerateKey("SELECT * FROM users WHERE id = @id", 123);
            var testData = new { Id = 123, Name = "Test User", CreatedAt = DateTime.UtcNow };
            
            // Set cache
            await cache.SetAsync(key, testData, new CacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(5),
                Priority = CachePriority.High
            });
            
            // Get from cache
            var cachedResult = await cache.GetAsync<dynamic>(key);
            cachedResult.IsHit.Should().BeTrue();
            _output.WriteLine($"  Cache hit: {cachedResult.IsHit}");
            
            // Test statistics
            var stats = cache.GetStatistics();
            _output.WriteLine($"  Cache statistics:");
            _output.WriteLine($"    - Hit ratio: {stats.HitRatio:F1}%");
            _output.WriteLine($"    - Total hits: {stats.TotalHits}");
            _output.WriteLine($"    - Current entries: {stats.CurrentEntryCount}");
            
            _output.WriteLine("  ✓ Caching completed");
        }

        private async Task TestCircuitBreaker()
        {
            _output.WriteLine("\n--- Step 6: Testing Circuit Breaker ---");
            
            var factory = _serviceProvider.GetRequiredService<ICircuitBreakerFactory>();
            var circuitBreaker = factory.GetOrCreateCircuitBreaker("TestService", new CircuitBreakerOptions
            {
                FailureThreshold = 3,
                OpenDuration = TimeSpan.FromSeconds(5),
                EnableLogging = true
            });
            
            var failureCount = 0;
            
            // Simulate failures
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    await circuitBreaker.ExecuteAsync(async () =>
                    {
                        if (failureCount < 3)
                        {
                            failureCount++;
                            throw new Exception("Simulated failure");
                        }
                        await Task.CompletedTask;
                    });
                }
                catch (CircuitBreakerOpenException)
                {
                    _output.WriteLine($"  Circuit breaker opened after {failureCount} failures");
                    break;
                }
                catch (Exception)
                {
                    _output.WriteLine($"  Failure {failureCount} recorded");
                }
            }
            
            var stats = circuitBreaker.GetStatistics();
            _output.WriteLine($"  Circuit breaker statistics:");
            _output.WriteLine($"    - State: {circuitBreaker.State}");
            _output.WriteLine($"    - Failed calls: {stats.FailedCalls}");
            _output.WriteLine($"    - Rejected calls: {stats.RejectedCalls}");
            
            _output.WriteLine("  ✓ Circuit breaker completed");
        }

        private async Task TestConnectionPoolManagement()
        {
            _output.WriteLine("\n--- Step 7: Testing Connection Pool Management ---");
            
            var poolManager = _serviceProvider.GetRequiredService<IConnectionPoolManager>();
            var connectionString = Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                _output.WriteLine("  ⚠ Skipping - No SQL Server connection configured");
                return;
            }

            // Configure pool
            var settings = new ConnectionPoolSettings
            {
                MinPoolSize = 5,
                MaxPoolSize = 20,
                ConnectionTimeout = 30,
                PoolingEnabled = true
            };
            
            var configuredConnString = poolManager.ConfigurePool(connectionString, settings, DatabaseType.SqlServer);
            _output.WriteLine("  Connection pool configured");
            
            // Warm pool
            var warmResult = await poolManager.WarmConnectionPoolAsync(configuredConnString, DatabaseType.SqlServer, 5);
            _output.WriteLine($"  Pool warming: {(warmResult ? "Success" : "Failed")}");
            
            // Get statistics
            var poolStats = await poolManager.GetPoolStatisticsAsync(configuredConnString, DatabaseType.SqlServer);
            _output.WriteLine($"  Pool statistics:");
            _output.WriteLine($"    - Total connections: {poolStats.TotalConnections}");
            _output.WriteLine($"    - Active connections: {poolStats.ActiveConnections}");
            
            _output.WriteLine("  ✓ Connection pool management completed");
        }

        private async Task TestConfigurationProviders()
        {
            _output.WriteLine("\n--- Step 8: Testing Configuration Providers ---");
            
            var factory = _serviceProvider.GetRequiredService<IConfigurationProviderFactory>();
            
            // Test environment variable provider
            var envProvider = factory.Create(ConfigurationProviderType.EnvironmentVariables);
            Environment.SetEnvironmentVariable("TEST_CONFIG_VALUE", "TestValue123");
            
            var value = await envProvider.GetValueAsync("TEST_CONFIG_VALUE");
            _output.WriteLine($"  Environment variable test: {value}");
            value.Should().Be("TestValue123");
            
            // Test Azure Key Vault provider (stub)
            var kvProvider = factory.Create(ConfigurationProviderType.AzureKeyVault);
            _output.WriteLine("  Azure Key Vault provider created (stub implementation)");
            
            _output.WriteLine("  ✓ Configuration providers completed");
        }

        private async Task TestAdaptiveTimeout()
        {
            _output.WriteLine("\n--- Step 9: Testing Adaptive Timeout ---");
            
            var calculator = _serviceProvider.GetRequiredService<IAdaptiveTimeoutCalculator>();
            
            // Test timeout calculation
            var timeout1 = calculator.CalculateTimeout(100); // 100 MB database
            var timeout2 = calculator.CalculateTimeout(1000); // 1 GB database
            var timeout3 = calculator.CalculateTimeout(10000); // 10 GB database
            
            _output.WriteLine($"  Timeout for 100 MB: {timeout1} seconds");
            _output.WriteLine($"  Timeout for 1 GB: {timeout2} seconds");
            _output.WriteLine($"  Timeout for 10 GB: {timeout3} seconds");
            
            timeout1.Should().BeLessThan(timeout2);
            timeout2.Should().BeLessThan(timeout3);
            
            // Test dynamic timeout
            var context = new DynamicTimeoutContext
            {
                Operation = "TableAnalysis",
                DatabaseSizeMB = 500,
                NetworkLatencyMs = 50,
                HistoricalExecutionTimes = new List<TimeSpan> 
                { 
                    TimeSpan.FromSeconds(5), 
                    TimeSpan.FromSeconds(6), 
                    TimeSpan.FromSeconds(7) 
                }
            };
            
            var dynamicTimeout = calculator.GetDynamicTimeout(context);
            _output.WriteLine($"  Dynamic timeout: {dynamicTimeout} seconds");
            
            _output.WriteLine("  ✓ Adaptive timeout completed");
        }

        private async Task TestFullAnalysisPipeline()
        {
            _output.WriteLine("\n--- Step 10: Testing Full Analysis Pipeline ---");
            
            var connectionString = Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION");
            if (string.IsNullOrEmpty(connectionString))
            {
                _output.WriteLine("  ⚠ Skipping - No SQL Server connection configured");
                return;
            }

            // Create a complete analysis pipeline
            var factory = _serviceProvider.GetRequiredService<IConnectionFactory>();
            var cache = _serviceProvider.GetRequiredService<IQueryCache>();
            var circuitBreaker = _serviceProvider.GetRequiredService<ICircuitBreakerFactory>()
                .GetOrCreateCircuitBreaker("AnalysisPipeline");
            
            var pipelineResult = await circuitBreaker.ExecuteAsync(async () =>
            {
                // Check cache first
                var cacheKey = "full_analysis_" + connectionString.GetHashCode();
                var cachedAnalysis = await cache.GetAsync<AnalysisResult>(cacheKey);
                
                if (cachedAnalysis.IsHit)
                {
                    _output.WriteLine("  Analysis retrieved from cache");
                    return cachedAnalysis.Value!;
                }
                
                // Perform analysis
                using var connection = factory.CreateConnection(connectionString, DatabaseType.SqlServer);
                await connection.OpenAsync();
                
                var analyzer = new TableAnalyzer(connection, 
                    _serviceProvider.GetRequiredService<ILogger<TableAnalyzer>>());
                    
                var result = await analyzer.AnalyzeAsync();
                
                // Cache the result
                await cache.SetAsync(cacheKey, result, new CacheEntryOptions
                {
                    AbsoluteExpiration = DateTime.UtcNow.AddMinutes(10),
                    Priority = CachePriority.High
                });
                
                return result;
            });
            
            _output.WriteLine($"  Pipeline completed: {pipelineResult.Success}");
            _output.WriteLine($"  Analysis duration: {pipelineResult.AnalysisDuration.TotalSeconds:F2} seconds");
            
            _output.WriteLine("  ✓ Full analysis pipeline completed");
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Logging
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddProvider(new TestLoggerProvider(_output));
            });

            // Core services
            services.AddSingleton<IConnectionFactory, ConnectionFactory>();
            services.AddSingleton<IConnectionStringValidator, ConnectionStringValidator>();
            services.AddSingleton<IQueryOptimizer, QueryOptimizer>();
            services.AddSingleton<IAdaptiveTimeoutCalculator, AdaptiveTimeoutCalculator>();
            
            // Caching
            services.AddSingleton<IQueryCache, MemoryQueryCache>();
            services.AddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>();
            
            // Resilience
            services.AddSingleton<ICircuitBreakerFactory, CircuitBreakerFactory>();
            services.AddSingleton<CircuitBreakerOptions>(new CircuitBreakerOptions
            {
                FailureThreshold = 5,
                OpenDuration = TimeSpan.FromSeconds(30)
            });
            
            // Connection pooling
            services.AddSingleton<IConnectionPoolManager, ConnectionPoolManager>();
            
            // Configuration
            services.AddSingleton<IConfigurationProviderFactory, ConfigurationProviderFactory>();
            services.AddSingleton<ISecureConfigurationProvider, EnvironmentVariableProvider>();
        }

        private class TestLoggerProvider : ILoggerProvider
        {
            private readonly ITestOutputHelper _output;

            public TestLoggerProvider(ITestOutputHelper output)
            {
                _output = output;
            }

            public ILogger CreateLogger(string categoryName)
            {
                return new TestLogger(categoryName, _output);
            }

            public void Dispose() { }
        }

        private class TestLogger : ILogger
        {
            private readonly string _categoryName;
            private readonly ITestOutputHelper _output;

            public TestLogger(string categoryName, ITestOutputHelper output)
            {
                _categoryName = categoryName;
                _output = output;
            }

            public IDisposable BeginScope<TState>(TState state) => new NoopDisposable();

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, 
                Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (IsEnabled(logLevel))
                {
                    _output.WriteLine($"[{logLevel}] {_categoryName}: {formatter(state, exception)}");
                }
            }

            private class NoopDisposable : IDisposable
            {
                public void Dispose() { }
            }
        }
    }
}