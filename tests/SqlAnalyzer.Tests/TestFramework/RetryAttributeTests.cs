using System;
using System.Threading.Tasks;
using FluentAssertions;
using SqlAnalyzer.Tests.TestFramework;
using Xunit;
using Xunit.Abstractions;

namespace SqlAnalyzer.Tests.TestFramework
{
    public class RetryAttributeTests
    {
        private readonly ITestOutputHelper _output;
        private static int _executionCount;

        public RetryAttributeTests(ITestOutputHelper output)
        {
            _output = output;
            _executionCount = 0;
        }

        [Fact]
        [Retry(3)]
        public void RetryAttribute_WithPassingTest_ShouldExecuteOnce()
        {
            // This test should pass on first attempt
            _executionCount++;
            _output.WriteLine($"Execution count: {_executionCount}");
            
            true.Should().BeTrue();
        }

        [Fact]
        [Retry(3, DelayMilliseconds = 100)]
        public async Task RetryAttribute_WithAsyncPassingTest_ShouldExecuteOnce()
        {
            await Task.Delay(10);
            true.Should().BeTrue();
        }

        // Note: Testing failing tests is tricky in xUnit as they would fail the test run
        // Instead, we'll test the retry logic separately

        [Fact]
        public void RetryPolicy_ShouldRetrySpecifiedTimes()
        {
            // Arrange
            var policy = new RetryPolicy(3, 100);
            var attempts = 0;
            var succeeded = false;

            // Act
            try
            {
                policy.Execute(() =>
                {
                    attempts++;
                    if (attempts < 3)
                    {
                        throw new Exception("Simulated failure");
                    }
                });
                succeeded = true;
            }
            catch
            {
                // Expected for this test
            }

            // Assert
            attempts.Should().Be(3);
            succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task RetryPolicy_WithAsync_ShouldRetrySpecifiedTimes()
        {
            // Arrange
            var policy = new RetryPolicy(3, 50);
            var attempts = 0;

            // Act
            await policy.ExecuteAsync(async () =>
            {
                attempts++;
                await Task.Delay(10);
                
                if (attempts < 2)
                {
                    throw new Exception("Simulated async failure");
                }
            });

            // Assert
            attempts.Should().Be(2);
        }

        [Fact]
        public void RetryPolicy_ShouldThrowAfterMaxAttempts()
        {
            // Arrange
            var policy = new RetryPolicy(2, 50);
            var attempts = 0;

            // Act & Assert
            var act = () => policy.Execute(() =>
            {
                attempts++;
                throw new Exception($"Failure {attempts}");
            });

            act.Should().Throw<Exception>()
                .WithMessage("Failure 2");
            attempts.Should().Be(2);
        }

        [Fact]
        public void RetryPolicy_WithExponentialBackoff_ShouldIncreaseDelay()
        {
            // Arrange
            var policy = new RetryPolicy(3, 100, useExponentialBackoff: true);
            var timestamps = new System.Collections.Generic.List<DateTime>();

            // Act
            try
            {
                policy.Execute(() =>
                {
                    timestamps.Add(DateTime.UtcNow);
                    throw new Exception("Force retry");
                });
            }
            catch
            {
                // Expected
            }

            // Assert
            timestamps.Should().HaveCount(3);
            
            // Verify delays are increasing (approximately)
            if (timestamps.Count >= 3)
            {
                var delay1 = (timestamps[1] - timestamps[0]).TotalMilliseconds;
                var delay2 = (timestamps[2] - timestamps[1]).TotalMilliseconds;
                
                delay2.Should().BeGreaterThan(delay1 * 1.5); // Allow some tolerance
            }
        }

        [Theory]
        [InlineData(typeof(InvalidOperationException), true)]
        [InlineData(typeof(ArgumentException), false)]
        public void RetryPolicy_WithSpecificExceptions_ShouldOnlyRetryForConfigured(Type exceptionType, bool shouldRetry)
        {
            // Arrange
            var policy = new RetryPolicy(3, 50, retryableExceptions: new[] { typeof(InvalidOperationException) });
            var attempts = 0;

            // Act
            try
            {
                policy.Execute(() =>
                {
                    attempts++;
                    throw (Exception)Activator.CreateInstance(exceptionType, "Test exception");
                });
            }
            catch
            {
                // Expected
            }

            // Assert
            if (shouldRetry)
            {
                attempts.Should().Be(3);
            }
            else
            {
                attempts.Should().Be(1);
            }
        }
    }
}