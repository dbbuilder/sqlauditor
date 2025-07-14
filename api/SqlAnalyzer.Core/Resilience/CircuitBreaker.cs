using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace SqlAnalyzer.Core.Resilience
{
    /// <summary>
    /// Implementation of circuit breaker pattern using Polly
    /// </summary>
    public class CircuitBreaker : ICircuitBreaker
    {
        private readonly ILogger<CircuitBreaker> _logger;
        private readonly CircuitBreakerOptions _options;
        private readonly IAsyncPolicy _policy;
        private readonly object _lock = new object();
        
        private CircuitState _state = CircuitState.Closed;
        private int _failureCount;
        private DateTime? _lastOpenedTime;
        private Exception? _lastException;
        private readonly CircuitBreakerStatistics _statistics;
        
        // Atomic counters for thread-safe operations
        private long _totalCalls;
        private long _successfulCalls;
        private long _failedCalls;
        private long _rejectedCalls;

        public CircuitState State => _state;
        public int FailureCount => _failureCount;
        public DateTime? LastOpenedTime => _lastOpenedTime;
        public Exception? LastException => _lastException;

        public CircuitBreaker(ILogger<CircuitBreaker> logger, CircuitBreakerOptions options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _statistics = new CircuitBreakerStatistics
            {
                CreatedAt = DateTime.UtcNow
            };

            // Build the circuit breaker policy
            _policy = Policy.Handle<Exception>(ex => ShouldHandleException(ex))
                .AdvancedCircuitBreakerAsync(
                    failureThreshold: 0.5, // 50% failure rate
                    samplingDuration: TimeSpan.FromSeconds(10),
                    minimumThroughput: _options.FailureThreshold,
                    durationOfBreak: _options.OpenDuration,
                    onBreak: (result, timespan) => OnBreak(result, timespan),
                    onReset: () => OnReset(),
                    onHalfOpen: () => OnHalfOpen());
        }

        public async Task ExecuteAsync(Func<Task> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            try
            {
                Interlocked.Increment(ref _totalCalls);
                _statistics.TotalCalls = _totalCalls;
                
                await _policy.ExecuteAsync(async () =>
                {
                    await action();
                    OnSuccess();
                });
            }
            catch (BrokenCircuitException ex)
            {
                Interlocked.Increment(ref _rejectedCalls);
                _statistics.RejectedCalls = _rejectedCalls;
                throw new CircuitBreakerOpenException(_state, _lastOpenedTime ?? DateTime.UtcNow, GetRetryAfter());
            }
            catch (Exception ex)
            {
                OnFailure(ex);
                throw;
            }
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            try
            {
                Interlocked.Increment(ref _totalCalls);
                _statistics.TotalCalls = _totalCalls;
                
                return await _policy.ExecuteAsync(async () =>
                {
                    var result = await func();
                    OnSuccess();
                    return result;
                });
            }
            catch (BrokenCircuitException ex)
            {
                Interlocked.Increment(ref _rejectedCalls);
                _statistics.RejectedCalls = _rejectedCalls;
                throw new CircuitBreakerOpenException(_state, _lastOpenedTime ?? DateTime.UtcNow, GetRetryAfter());
            }
            catch (Exception ex)
            {
                OnFailure(ex);
                throw;
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _state = CircuitState.Closed;
                _failureCount = 0;
                _lastOpenedTime = null;
                _lastException = null;
                _statistics.LastResetTime = DateTime.UtcNow;
                _statistics.ConsecutiveFailures = 0;
                
                if (_options.EnableLogging)
                {
                    _logger.LogInformation("Circuit breaker manually reset");
                }
            }
        }

        public CircuitBreakerStatistics GetStatistics()
        {
            lock (_lock)
            {
                return new CircuitBreakerStatistics
                {
                    TotalCalls = _statistics.TotalCalls,
                    SuccessfulCalls = _statistics.SuccessfulCalls,
                    FailedCalls = _statistics.FailedCalls,
                    RejectedCalls = _statistics.RejectedCalls,
                    ConsecutiveFailures = _statistics.ConsecutiveFailures,
                    CreatedAt = _statistics.CreatedAt,
                    LastResetTime = _statistics.LastResetTime
                };
            }
        }

        private bool ShouldHandleException(Exception ex)
        {
            // If no specific exception types configured, handle all
            if (_options.HandledExceptionTypes == null || _options.HandledExceptionTypes.Length == 0)
                return true;

            // Check if exception type is in the handled list
            var exceptionType = ex.GetType();
            foreach (var handledType in _options.HandledExceptionTypes)
            {
                if (handledType.IsAssignableFrom(exceptionType))
                    return true;
            }

            return false;
        }

        private void OnBreak(Exception exception, TimeSpan duration)
        {
            lock (_lock)
            {
                var previousState = _state;
                _state = CircuitState.Open;
                _lastOpenedTime = DateTime.UtcNow;
                _lastException = exception;
                
                if (_options.EnableLogging)
                {
                    _logger.LogWarning(exception,
                        "Circuit breaker opened for {Duration}s after {FailureCount} failures",
                        duration.TotalSeconds, _options.FailureThreshold);
                }

                _options.OnStateChange?.Invoke(previousState, _state);
            }
        }

        private void OnReset()
        {
            lock (_lock)
            {
                var previousState = _state;
                _state = CircuitState.Closed;
                _failureCount = 0;
                _statistics.ConsecutiveFailures = 0;
                
                if (_options.EnableLogging)
                {
                    _logger.LogInformation("Circuit breaker closed - service recovered");
                }

                _options.OnStateChange?.Invoke(previousState, _state);
            }
        }

        private void OnHalfOpen()
        {
            lock (_lock)
            {
                var previousState = _state;
                _state = CircuitState.HalfOpen;
                
                if (_options.EnableLogging)
                {
                    _logger.LogInformation("Circuit breaker half-open - testing service recovery");
                }

                _options.OnStateChange?.Invoke(previousState, _state);
            }
        }

        private void OnSuccess()
        {
            lock (_lock)
            {
                Interlocked.Increment(ref _successfulCalls);
                _statistics.SuccessfulCalls = _successfulCalls;
                _failureCount = 0;
                _statistics.ConsecutiveFailures = 0;
            }
        }

        private void OnFailure(Exception ex)
        {
            lock (_lock)
            {
                Interlocked.Increment(ref _failedCalls);
                _statistics.FailedCalls = _failedCalls;
                _failureCount++;
                _statistics.ConsecutiveFailures++;
                _lastException = ex;
                
                if (_options.EnableLogging)
                {
                    _logger.LogError(ex, "Circuit breaker recorded failure {FailureCount}/{Threshold}",
                        _failureCount, _options.FailureThreshold);
                }
            }
        }

        private TimeSpan GetRetryAfter()
        {
            if (_lastOpenedTime.HasValue)
            {
                var elapsed = DateTime.UtcNow - _lastOpenedTime.Value;
                var remaining = _options.OpenDuration - elapsed;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }

            return _options.OpenDuration;
        }
    }
}