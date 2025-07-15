using SqlAnalyzer.Api.Services;
using SqlAnalyzer.Core.Caching;
using SqlAnalyzer.Core.Configuration;
using SqlAnalyzer.Core.Connections;
using SqlAnalyzer.Core.Optimization;
using SqlAnalyzer.Core.Resilience;
using SqlAnalyzer.Core.Security;
using Microsoft.Extensions.Caching.Memory;

namespace SqlAnalyzer.Api.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection ConfigureSqlAnalyzer(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Core services
            services.AddSingleton<IConnectionFactory, ConnectionFactory>();
            services.AddSingleton<IConnectionStringValidator, ConnectionStringValidator>();
            services.AddSingleton<IQueryOptimizer, QueryOptimizer>();
            services.AddSingleton<IAdaptiveTimeoutCalculator, AdaptiveTimeoutCalculator>();

            // Caching
            services.AddSingleton<IMemoryCache, MemoryCache>();
            services.AddSingleton<IQueryCache>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<MemoryQueryCache>>();
                return new MemoryQueryCache(logger, "SqlAnalyzerCache");
            });
            services.AddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>();

            // Resilience
            services.AddSingleton<ICircuitBreakerFactory, CircuitBreakerFactory>();
            services.AddSingleton<CircuitBreakerOptions>(provider =>
            {
                return new CircuitBreakerOptions
                {
                    FailureThreshold = configuration.GetValue<int>("SqlAnalyzer:CircuitBreaker:FailureThreshold", 5),
                    OpenDuration = TimeSpan.FromSeconds(
                        configuration.GetValue<int>("SqlAnalyzer:CircuitBreaker:OpenDurationSeconds", 30)),
                    EnableLogging = true
                };
            });

            // Connection pooling
            services.AddSingleton<IConnectionPoolManager, ConnectionPoolManager>();

            // Configuration
            // Configuration provider is static, no need to register
            services.AddSingleton<ISecureConfigurationProvider, EnvironmentVariableProvider>();

            // Analysis service
            services.AddSingleton<IAnalysisService, AnalysisService>();

            // Background service
            services.AddSingleton<AnalysisBackgroundService>();

            return services;
        }
    }
}