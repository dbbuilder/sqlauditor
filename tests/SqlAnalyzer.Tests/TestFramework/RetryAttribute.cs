using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SqlAnalyzer.Tests.TestFramework
{
    /// <summary>
    /// Attribute to retry failed tests a specified number of times
    /// </summary>
    [XunitTestCaseDiscoverer("SqlAnalyzer.Tests.TestFramework.RetryTestCaseDiscoverer", "SqlAnalyzer.Tests")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RetryAttribute : FactAttribute
    {
        private readonly int _maxRetries;
        private readonly int _delayMilliseconds;
        private readonly bool _useExponentialBackoff;
        private readonly Type[] _retryableExceptions;

        /// <summary>
        /// Maximum number of retry attempts
        /// </summary>
        public int MaxRetries => _maxRetries;

        /// <summary>
        /// Delay between retries in milliseconds
        /// </summary>
        public int DelayMilliseconds
        {
            get => _delayMilliseconds;
            init => _delayMilliseconds = value;
        }

        /// <summary>
        /// Whether to use exponential backoff for delays
        /// </summary>
        public bool UseExponentialBackoff
        {
            get => _useExponentialBackoff;
            init => _useExponentialBackoff = value;
        }

        /// <summary>
        /// Specific exceptions to retry (null means retry all)
        /// </summary>
        public Type[] RetryableExceptions
        {
            get => _retryableExceptions;
            init => _retryableExceptions = value;
        }

        public RetryAttribute(int maxRetries = 3)
        {
            if (maxRetries < 1)
                throw new ArgumentException("Max retries must be at least 1", nameof(maxRetries));

            _maxRetries = maxRetries;
            _delayMilliseconds = 1000; // Default 1 second
            _useExponentialBackoff = false;
            _retryableExceptions = null; // Retry all exceptions by default
        }
    }

    /// <summary>
    /// Custom test case that implements retry logic
    /// </summary>
    public class RetryTestCase : XunitTestCase
    {
        private int _maxRetries = 3;
        private int _delayMilliseconds = 1000;
        private bool _useExponentialBackoff = false;
        private Type[] _retryableExceptions = null;

        [Obsolete("For deserialization only")]
        public RetryTestCase() { }

        public RetryTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay,
            TestMethodDisplayOptions defaultMethodDisplayOptions, ITestMethod testMethod,
            object[] testMethodArguments = null)
            : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod, testMethodArguments)
        {
            var retryAttribute = testMethod.Method.GetCustomAttributes(typeof(RetryAttribute)).FirstOrDefault() as RetryAttribute;
            if (retryAttribute != null)
            {
                _maxRetries = retryAttribute.MaxRetries;
                _delayMilliseconds = retryAttribute.DelayMilliseconds;
                _useExponentialBackoff = retryAttribute.UseExponentialBackoff;
                _retryableExceptions = retryAttribute.RetryableExceptions;
            }
        }

        public override async Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink,
            IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
        {
            var policy = new RetryPolicy(_maxRetries, _delayMilliseconds, _useExponentialBackoff, _retryableExceptions);
            var runSummary = new RunSummary();

            await policy.ExecuteAsync(async () =>
            {
                runSummary = await base.RunAsync(diagnosticMessageSink, messageBus, constructorArguments,
                    aggregator, cancellationTokenSource);

                if (runSummary.Failed > 0)
                {
                    throw new TestFailedException($"Test failed after attempt");
                }
            });

            return runSummary;
        }
    }

    /// <summary>
    /// Exception thrown when a test fails (for retry logic)
    /// </summary>
    public class TestFailedException : Exception
    {
        public TestFailedException(string message) : base(message) { }
    }

    /// <summary>
    /// Retry policy implementation
    /// </summary>
    public class RetryPolicy
    {
        private readonly int _maxRetries;
        private readonly int _delayMilliseconds;
        private readonly bool _useExponentialBackoff;
        private readonly Type[] _retryableExceptions;

        public RetryPolicy(int maxRetries, int delayMilliseconds, 
            bool useExponentialBackoff = false, Type[] retryableExceptions = null)
        {
            _maxRetries = maxRetries;
            _delayMilliseconds = delayMilliseconds;
            _useExponentialBackoff = useExponentialBackoff;
            _retryableExceptions = retryableExceptions;
        }

        public void Execute(Action action)
        {
            var exceptions = new List<Exception>();

            for (int attempt = 1; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    action();
                    return; // Success
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);

                    if (!ShouldRetry(ex) || attempt == _maxRetries)
                    {
                        throw;
                    }

                    var delay = GetDelay(attempt);
                    Thread.Sleep(delay);
                }
            }
        }

        public async Task ExecuteAsync(Func<Task> action)
        {
            var exceptions = new List<Exception>();

            for (int attempt = 1; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    await action();
                    return; // Success
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);

                    if (!ShouldRetry(ex) || attempt == _maxRetries)
                    {
                        throw;
                    }

                    var delay = GetDelay(attempt);
                    await Task.Delay(delay);
                }
            }
        }

        private bool ShouldRetry(Exception ex)
        {
            if (_retryableExceptions == null || _retryableExceptions.Length == 0)
                return true; // Retry all exceptions

            return _retryableExceptions.Any(type => type.IsAssignableFrom(ex.GetType()));
        }

        private int GetDelay(int attempt)
        {
            if (_useExponentialBackoff)
            {
                return _delayMilliseconds * (int)Math.Pow(2, attempt - 1);
            }

            return _delayMilliseconds;
        }
    }

    /// <summary>
    /// Test case discoverer for the RetryAttribute
    /// </summary>
    public class RetryTestCaseDiscoverer : IXunitTestCaseDiscoverer
    {
        private readonly IMessageSink _diagnosticMessageSink;

        public RetryTestCaseDiscoverer(IMessageSink diagnosticMessageSink)
        {
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, 
            ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            yield return new RetryTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), 
                discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod);
        }
    }
}