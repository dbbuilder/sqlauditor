using System;
using System.Threading.Tasks;

namespace SqlAnalyzer.Core.Resilience
{
    /// <summary>
    /// Interface for implementing circuit breaker pattern to handle transient failures
    /// </summary>
    public interface ICircuitBreaker
    {
        /// <summary>
        /// Gets the current state of the circuit breaker
        /// </summary>
        CircuitState State { get; }

        /// <summary>
        /// Gets the number of consecutive failures
        /// </summary>
        int FailureCount { get; }

        /// <summary>
        /// Gets the last time the circuit was opened
        /// </summary>
        DateTime? LastOpenedTime { get; }

        /// <summary>
        /// Gets the last exception that caused the circuit to open
        /// </summary>
        Exception? LastException { get; }

        /// <summary>
        /// Executes an action with circuit breaker protection
        /// </summary>
        Task ExecuteAsync(Func<Task> action);

        /// <summary>
        /// Executes a function with circuit breaker protection
        /// </summary>
        Task<T> ExecuteAsync<T>(Func<Task<T>> func);

        /// <summary>
        /// Resets the circuit breaker to closed state
        /// </summary>
        void Reset();

        /// <summary>
        /// Gets statistics about the circuit breaker
        /// </summary>
        CircuitBreakerStatistics GetStatistics();
    }

    /// <summary>
    /// Represents the state of a circuit breaker
    /// </summary>
    public enum CircuitState
    {
        /// <summary>
        /// Circuit is closed and requests are allowed through
        /// </summary>
        Closed,

        /// <summary>
        /// Circuit is open and requests are blocked
        /// </summary>
        Open,

        /// <summary>
        /// Circuit is half-open and testing if the service has recovered
        /// </summary>
        HalfOpen
    }

    /// <summary>
    /// Configuration for circuit breaker behavior
    /// </summary>
    public class CircuitBreakerOptions
    {
        /// <summary>
        /// Number of consecutive failures before opening the circuit
        /// </summary>
        public int FailureThreshold { get; set; } = 5;

        /// <summary>
        /// Duration to wait before attempting to close the circuit
        /// </summary>
        public TimeSpan OpenDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Types of exceptions that should trigger the circuit breaker
        /// </summary>
        public Type[] HandledExceptionTypes { get; set; } = Array.Empty<Type>();

        /// <summary>
        /// Callback when circuit state changes
        /// </summary>
        public Action<CircuitState, CircuitState>? OnStateChange { get; set; }

        /// <summary>
        /// Whether to log circuit breaker events
        /// </summary>
        public bool EnableLogging { get; set; } = true;
    }

    /// <summary>
    /// Statistics about circuit breaker usage
    /// </summary>
    public class CircuitBreakerStatistics
    {
        /// <summary>
        /// Total number of calls made through the circuit breaker
        /// </summary>
        public long TotalCalls { get; set; }

        /// <summary>
        /// Number of successful calls
        /// </summary>
        public long SuccessfulCalls { get; set; }

        /// <summary>
        /// Number of failed calls
        /// </summary>
        public long FailedCalls { get; set; }

        /// <summary>
        /// Number of calls rejected due to open circuit
        /// </summary>
        public long RejectedCalls { get; set; }

        /// <summary>
        /// Current consecutive failure count
        /// </summary>
        public int ConsecutiveFailures { get; set; }

        /// <summary>
        /// Time the circuit breaker was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last time the circuit was reset
        /// </summary>
        public DateTime? LastResetTime { get; set; }

        /// <summary>
        /// Success rate percentage
        /// </summary>
        public double SuccessRate => TotalCalls > 0 ? (double)SuccessfulCalls / TotalCalls * 100 : 0;
    }

    /// <summary>
    /// Exception thrown when circuit breaker is open
    /// </summary>
    public class CircuitBreakerOpenException : Exception
    {
        public CircuitState State { get; }
        public DateTime OpenedAt { get; }
        public TimeSpan RetryAfter { get; }

        public CircuitBreakerOpenException(CircuitState state, DateTime openedAt, TimeSpan retryAfter)
            : base($"Circuit breaker is {state}. Service has been unavailable since {openedAt}. Retry after {retryAfter}.")
        {
            State = state;
            OpenedAt = openedAt;
            RetryAfter = retryAfter;
        }
    }
}