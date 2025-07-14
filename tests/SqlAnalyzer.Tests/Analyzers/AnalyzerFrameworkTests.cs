using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SqlAnalyzer.Core.Analyzers;
using SqlAnalyzer.Core.Connections;
using SqlAnalyzer.Core.Models;
using Xunit;

namespace SqlAnalyzer.Tests.Analyzers
{
    public class AnalyzerFrameworkTests
    {
        private readonly Mock<ISqlAnalyzerConnection> _mockConnection;
        private readonly Mock<ILogger<TestAnalyzer>> _mockLogger;

        public AnalyzerFrameworkTests()
        {
            _mockConnection = new Mock<ISqlAnalyzerConnection>();
            _mockLogger = new Mock<ILogger<TestAnalyzer>>();
        }

        [Fact]
        public async Task AnalyzeAsync_ShouldReturnAnalysisResult()
        {
            // Arrange
            var analyzer = new TestAnalyzer(_mockConnection.Object, _mockLogger.Object);

            // Act
            var result = await analyzer.AnalyzeAsync();

            // Assert
            result.Should().NotBeNull();
            result.AnalyzerName.Should().Be("TestAnalyzer");
            result.DatabaseName.Should().Be(_mockConnection.Object.DatabaseName);
            result.Findings.Should().NotBeNull();
            result.AnalysisStartTime.Should().BeBefore(result.AnalysisEndTime);
        }

        [Fact]
        public void Constructor_WithNullConnection_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var act = () => new TestAnalyzer(null, _mockLogger.Object);
            act.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("connection");
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var act = () => new TestAnalyzer(_mockConnection.Object, null);
            act.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("logger");
        }

        [Fact]
        public async Task AnalyzeAsync_WithException_ShouldCatchAndLogError()
        {
            // Arrange
            var analyzer = new ExceptionThrowingAnalyzer(_mockConnection.Object, _mockLogger.Object);

            // Act
            var result = await analyzer.AnalyzeAsync();

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Test exception");
        }

        [Theory]
        [InlineData(Severity.Info, "Information finding")]
        [InlineData(Severity.Warning, "Warning finding")]
        [InlineData(Severity.Error, "Error finding")]
        [InlineData(Severity.Critical, "Critical finding")]
        public async Task AddFinding_ShouldAddFindingWithCorrectSeverity(Severity severity, string message)
        {
            // Arrange
            var analyzer = new TestAnalyzer(_mockConnection.Object, _mockLogger.Object);
            analyzer.TestSeverity = severity;
            analyzer.TestMessage = message;

            // Act
            var result = await analyzer.AnalyzeAsync();

            // Assert
            result.Findings.Should().HaveCount(1);
            var finding = result.Findings[0];
            finding.Severity.Should().Be(severity);
            finding.Message.Should().Be(message);
        }

        private class TestAnalyzer : BaseAnalyzer<object>
        {
            public Severity TestSeverity { get; set; } = Severity.Info;
            public string TestMessage { get; set; } = "Test finding";

            public TestAnalyzer(ISqlAnalyzerConnection connection, ILogger<TestAnalyzer> logger) 
                : base(connection, logger)
            {
            }

            protected override async Task<List<object>> CollectDataAsync()
            {
                return await Task.FromResult(new List<object> { new object() });
            }

            protected override async Task AnalyzeDataAsync(List<object> data)
            {
                AddFinding(TestSeverity, TestMessage, "Test recommendation", "TestCategory");
                await Task.CompletedTask;
            }
        }

        private class ExceptionThrowingAnalyzer : BaseAnalyzer<object>
        {
            public ExceptionThrowingAnalyzer(ISqlAnalyzerConnection connection, ILogger<TestAnalyzer> logger) 
                : base(connection, logger)
            {
            }

            protected override async Task<List<object>> CollectDataAsync()
            {
                throw new InvalidOperationException("Test exception");
            }

            protected override async Task AnalyzeDataAsync(List<object> data)
            {
                await Task.CompletedTask;
            }
        }
    }
}