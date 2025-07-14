using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SqlAnalyzer.Core.Resilience
{
    /// <summary>
    /// Factory for creating and managing circuit breakers for different services
    /// </summary>
    public interface ICircuitBreakerFactory
    {
        /// <summary>
        /// Gets or creates a circuit breaker for the specified service
        /// </summary>
        ICircuitBreaker GetOrCreateCircuitBreaker(string serviceName, CircuitBreakerOptions? options = null);

        /// <summary>
        /// Gets an existing circuit breaker if it exists
        /// </summary>
        ICircuitBreaker? GetCircuitBreaker(string serviceName);

        /// <summary>
        /// Resets all circuit breakers
        /// </summary>
        void ResetAll();

        /// <summary>
        /// Gets statistics for all circuit breakers
        /// </summary>
        CircuitBreakerFactoryStatistics GetStatistics();
    }

    /// <summary>
    /// Default implementation of circuit breaker factory
    /// </summary>
    public class CircuitBreakerFactory : ICircuitBreakerFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ConcurrentDictionary<string, ICircuitBreaker> _circuitBreakers;
        private readonly CircuitBreakerOptions _defaultOptions;

        public CircuitBreakerFactory(ILoggerFactory loggerFactory, CircuitBreakerOptions? defaultOptions = null)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _circuitBreakers = new ConcurrentDictionary<string, ICircuitBreaker>(StringComparer.OrdinalIgnoreCase);
            _defaultOptions = defaultOptions ?? new CircuitBreakerOptions();
        }

        public ICircuitBreaker GetOrCreateCircuitBreaker(string serviceName, CircuitBreakerOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentException("Service name cannot be null or empty", nameof(serviceName));

            return _circuitBreakers.GetOrAdd(serviceName, name =>
            {
                var logger = _loggerFactory.CreateLogger<CircuitBreaker>();
                var effectiveOptions = options ?? _defaultOptions;
                return new CircuitBreaker(logger, effectiveOptions);
            });
        }

        public ICircuitBreaker? GetCircuitBreaker(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                return null;

            _circuitBreakers.TryGetValue(serviceName, out var circuitBreaker);
            return circuitBreaker;
        }

        public void ResetAll()
        {
            foreach (var circuitBreaker in _circuitBreakers.Values)
            {
                circuitBreaker.Reset();
            }
        }

        public CircuitBreakerFactoryStatistics GetStatistics()
        {
            var stats = new CircuitBreakerFactoryStatistics
            {
                TotalCircuitBreakers = _circuitBreakers.Count
            };

            foreach (var kvp in _circuitBreakers)
            {
                var serviceName = kvp.Key;
                var circuitBreaker = kvp.Value;
                var cbStats = circuitBreaker.GetStatistics();

                stats.ServiceStatistics[serviceName] = new ServiceCircuitBreakerInfo
                {
                    ServiceName = serviceName,
                    State = circuitBreaker.State,
                    Statistics = cbStats
                };

                stats.TotalCalls += cbStats.TotalCalls;
                stats.TotalSuccessfulCalls += cbStats.SuccessfulCalls;
                stats.TotalFailedCalls += cbStats.FailedCalls;
                stats.TotalRejectedCalls += cbStats.RejectedCalls;

                if (circuitBreaker.State == CircuitState.Open)
                    stats.OpenCircuits++;
                else if (circuitBreaker.State == CircuitState.HalfOpen)
                    stats.HalfOpenCircuits++;
            }

            return stats;
        }
    }

    /// <summary>
    /// Statistics for all circuit breakers managed by the factory
    /// </summary>
    public class CircuitBreakerFactoryStatistics
    {
        /// <summary>
        /// Total number of circuit breakers
        /// </summary>
        public int TotalCircuitBreakers { get; set; }

        /// <summary>
        /// Number of open circuits
        /// </summary>
        public int OpenCircuits { get; set; }

        /// <summary>
        /// Number of half-open circuits
        /// </summary>
        public int HalfOpenCircuits { get; set; }

        /// <summary>
        /// Total calls across all circuit breakers
        /// </summary>
        public long TotalCalls { get; set; }

        /// <summary>
        /// Total successful calls across all circuit breakers
        /// </summary>
        public long TotalSuccessfulCalls { get; set; }

        /// <summary>
        /// Total failed calls across all circuit breakers
        /// </summary>
        public long TotalFailedCalls { get; set; }

        /// <summary>
        /// Total rejected calls across all circuit breakers
        /// </summary>
        public long TotalRejectedCalls { get; set; }

        /// <summary>
        /// Statistics for each service
        /// </summary>
        public Dictionary<string, ServiceCircuitBreakerInfo> ServiceStatistics { get; set; } = new();

        /// <summary>
        /// Overall success rate percentage
        /// </summary>
        public double OverallSuccessRate => TotalCalls > 0 ? (double)TotalSuccessfulCalls / TotalCalls * 100 : 0;
    }

    /// <summary>
    /// Information about a specific service's circuit breaker
    /// </summary>
    public class ServiceCircuitBreakerInfo
    {
        /// <summary>
        /// Name of the service
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Current state of the circuit breaker
        /// </summary>
        public CircuitState State { get; set; }

        /// <summary>
        /// Circuit breaker statistics
        /// </summary>
        public CircuitBreakerStatistics Statistics { get; set; } = new();
    }
}