using System;
using FluentAssertions;
using SqlAnalyzer.Core.Optimization;
using Xunit;

namespace SqlAnalyzer.Tests.Optimization
{
    public class AdaptiveTimeoutCalculatorTests
    {
        private readonly AdaptiveTimeoutCalculator _calculator;

        public AdaptiveTimeoutCalculatorTests()
        {
            _calculator = new AdaptiveTimeoutCalculator();
        }

        [Theory]
        [InlineData(100, 30)]    // 100 MB -> 30 seconds
        [InlineData(500, 35)]    // 500 MB -> 35 seconds
        [InlineData(1000, 40)]   // 1 GB -> 40 seconds
        [InlineData(5000, 80)]   // 5 GB -> 80 seconds
        [InlineData(10000, 130)] // 10 GB -> 130 seconds
        public void CalculateTimeout_BasedOnDatabaseSize_ShouldReturnAppropriateTimeout(decimal sizeMB, int expectedTimeout)
        {
            // Act
            var timeout = _calculator.CalculateTimeout(sizeMB);

            // Assert
            timeout.Should().Be(expectedTimeout);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        public void CalculateTimeout_WithInvalidSize_ShouldReturnDefaultTimeout(decimal sizeMB)
        {
            // Act
            var timeout = _calculator.CalculateTimeout(sizeMB);

            // Assert
            timeout.Should().Be(30); // Default timeout
        }

        [Fact]
        public void CalculateTimeout_WithCustomOptions_ShouldRespectLimits()
        {
            // Arrange
            var options = new TimeoutCalculationOptions
            {
                MinTimeout = 60,
                MaxTimeout = 120,
                BaseFactor = 0.01m
            };

            // Act
            var smallDbTimeout = _calculator.CalculateTimeout(100, options);
            var largeDbTimeout = _calculator.CalculateTimeout(50000, options);

            // Assert
            smallDbTimeout.Should().Be(60); // Should not go below minimum
            largeDbTimeout.Should().Be(120); // Should not exceed maximum
        }

        [Theory]
        [InlineData(1, 1, 45)]      // 1 table -> +0 seconds
        [InlineData(100, 100, 45)]  // 100 objects -> +0 seconds
        [InlineData(500, 500, 50)]  // 500 objects -> +5 seconds
        [InlineData(1000, 1000, 55)] // 1000 objects -> +10 seconds
        [InlineData(5000, 5000, 75)] // 5000 objects -> +30 seconds
        public void CalculateAnalysisTimeout_WithObjectCount_ShouldAdjustTimeout(
            int tableCount, int totalObjects, int expectedTimeout)
        {
            // Arrange
            var context = new AnalysisContext
            {
                DatabaseSizeMB = 1000, // 1 GB base
                TableCount = tableCount,
                TotalObjectCount = totalObjects
            };

            // Act
            var timeout = _calculator.CalculateAnalysisTimeout(context);

            // Assert
            timeout.Should().Be(expectedTimeout);
        }

        [Fact]
        public void CalculateAnalysisTimeout_WithComplexity_ShouldIncreaseTimeout()
        {
            // Arrange
            var simpleContext = new AnalysisContext
            {
                DatabaseSizeMB = 1000,
                TableCount = 100,
                TotalObjectCount = 500,
                HasComplexQueries = false
            };

            var complexContext = new AnalysisContext
            {
                DatabaseSizeMB = 1000,
                TableCount = 100,
                TotalObjectCount = 500,
                HasComplexQueries = true
            };

            // Act
            var simpleTimeout = _calculator.CalculateAnalysisTimeout(simpleContext);
            var complexTimeout = _calculator.CalculateAnalysisTimeout(complexContext);

            // Assert
            complexTimeout.Should().BeGreaterThan(simpleTimeout);
        }

        [Fact]
        public void RecordExecutionTime_ShouldUpdateHistoricalData()
        {
            // Arrange
            var operation = "TableAnalysis";

            // Act
            _calculator.RecordExecutionTime(operation, 1000, 25);
            _calculator.RecordExecutionTime(operation, 1000, 30);
            _calculator.RecordExecutionTime(operation, 1000, 35);

            var suggestedTimeout = _calculator.GetSuggestedTimeout(operation, 1000);

            // Assert
            suggestedTimeout.Should().BeGreaterThan(35); // Should be greater than max recorded time
        }

        [Fact]
        public void GetSuggestedTimeout_WithNoHistory_ShouldUseCalculated()
        {
            // Arrange
            var operation = "NewOperation";

            // Act
            var timeout = _calculator.GetSuggestedTimeout(operation, 5000);

            // Assert
            timeout.Should().Be(_calculator.CalculateTimeout(5000));
        }

        [Fact]
        public void AdjustForNetworkLatency_ShouldIncreaseTimeout()
        {
            // Arrange
            var baseTimeout = 30;
            var networkLatencyMs = 100;

            // Act
            var adjustedTimeout = _calculator.AdjustForNetworkLatency(baseTimeout, networkLatencyMs);

            // Assert
            adjustedTimeout.Should().BeGreaterThan(baseTimeout);
        }

        [Theory]
        [InlineData(30, 0.8, 24)]   // 80% load -> reduce by 20%
        [InlineData(30, 0.5, 30)]   // 50% load -> no change
        [InlineData(30, 0.2, 36)]   // 20% load -> increase by 20%
        public void AdjustForSystemLoad_ShouldModifyTimeout(int baseTimeout, double cpuUsage, int expectedTimeout)
        {
            // Act
            var adjustedTimeout = _calculator.AdjustForSystemLoad(baseTimeout, cpuUsage);

            // Assert
            adjustedTimeout.Should().Be(expectedTimeout);
        }

        [Fact]
        public void GetDynamicTimeout_ShouldCombineAllFactors()
        {
            // Arrange
            var context = new DynamicTimeoutContext
            {
                Operation = "ComplexAnalysis",
                DatabaseSizeMB = 5000,
                ObjectCount = 1000,
                NetworkLatencyMs = 50,
                SystemCpuUsage = 0.7,
                HistoricalExecutionTimes = new[] { 45, 50, 55 }
            };

            // Act
            var timeout = _calculator.GetDynamicTimeout(context);

            // Assert
            timeout.Should().BeGreaterThan(55); // Should be greater than historical max
            timeout.Should().BeLessThan(300); // Should be reasonable
        }
    }
}