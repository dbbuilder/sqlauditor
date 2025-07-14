using System;
using System.Threading.Tasks;
using FluentAssertions;
using SqlAnalyzer.Core.Configuration;
using Xunit;

namespace SqlAnalyzer.Tests.Configuration
{
    public class EnvironmentVariableProviderTests : IDisposable
    {
        private const string TestPrefix = "SQLANALYZER_TEST_";
        private readonly EnvironmentVariableProvider _provider;
        private readonly EnvironmentVariableProvider _providerWithPrefix;

        public EnvironmentVariableProviderTests()
        {
            _provider = new EnvironmentVariableProvider();
            _providerWithPrefix = new EnvironmentVariableProvider(TestPrefix);
        }

        [Fact]
        public async Task GetValueAsync_WithExistingVariable_ShouldReturnValue()
        {
            // Arrange
            var key = "TEST_CONFIG_VALUE";
            var value = "TestValue123";
            Environment.SetEnvironmentVariable(key, value);

            // Act
            var result = await _provider.GetValueAsync(key);

            // Assert
            result.Should().Be(value);
        }

        [Fact]
        public async Task GetValueAsync_WithNonExistingVariable_ShouldReturnNull()
        {
            // Arrange
            var key = "NON_EXISTING_KEY_" + Guid.NewGuid();

            // Act
            var result = await _provider.GetValueAsync(key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetValueAsync_WithPrefix_ShouldFindPrefixedVariable()
        {
            // Arrange
            var key = "CONFIG_KEY";
            var value = "PrefixedValue";
            Environment.SetEnvironmentVariable($"{TestPrefix}{key}", value);

            // Act
            var result = await _providerWithPrefix.GetValueAsync(key);

            // Assert
            result.Should().Be(value);
        }

        [Fact]
        public async Task GetValueAsync_WithPrefix_ShouldFallbackToNonPrefixed()
        {
            // Arrange
            var key = "FALLBACK_KEY";
            var value = "FallbackValue";
            Environment.SetEnvironmentVariable(key, value);

            // Act
            var result = await _providerWithPrefix.GetValueAsync(key);

            // Assert
            result.Should().Be(value);
        }

        [Theory]
        [InlineData("TestDb", "ConnectionStrings__TestDb", "Server=localhost;Database=TestDb;")]
        [InlineData("TestDb", "ConnectionStrings:TestDb", "Server=localhost;Database=TestDb2;")]
        [InlineData("TestDb", "TestDb_CONNECTION", "Server=localhost;Database=TestDb3;")]
        [InlineData("TestDb", "TestDb_CONNECTION_STRING", "Server=localhost;Database=TestDb4;")]
        [InlineData("TestDb", "TestDb", "Server=localhost;Database=TestDb5;")]
        public async Task GetConnectionStringAsync_ShouldTryCommonPatterns(string name, string envKey, string expectedValue)
        {
            // Arrange
            Environment.SetEnvironmentVariable(envKey, expectedValue);

            // Act
            var result = await _provider.GetConnectionStringAsync(name);

            // Assert
            result.Should().Be(expectedValue);

            // Cleanup
            Environment.SetEnvironmentVariable(envKey, null);
        }

        [Fact]
        public async Task GetConnectionStringAsync_WithNoMatchingPattern_ShouldReturnNull()
        {
            // Arrange
            var name = "NonExistingDb_" + Guid.NewGuid();

            // Act
            var result = await _provider.GetConnectionStringAsync(name);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task ContainsKeyAsync_WithExistingKey_ShouldReturnTrue()
        {
            // Arrange
            var key = "EXISTING_KEY";
            Environment.SetEnvironmentVariable(key, "SomeValue");

            // Act
            var result = await _provider.ContainsKeyAsync(key);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ContainsKeyAsync_WithNonExistingKey_ShouldReturnFalse()
        {
            // Arrange
            var key = "NON_EXISTING_KEY_" + Guid.NewGuid();

            // Act
            var result = await _provider.ContainsKeyAsync(key);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ProviderName_ShouldReturnCorrectName()
        {
            // Assert
            _provider.ProviderName.Should().Be("EnvironmentVariables");
        }

        public void Dispose()
        {
            // Clean up any test environment variables
            var testKeys = new[]
            {
                "TEST_CONFIG_VALUE",
                "CONFIG_KEY",
                "FALLBACK_KEY",
                "EXISTING_KEY",
                $"{TestPrefix}CONFIG_KEY"
            };

            foreach (var key in testKeys)
            {
                Environment.SetEnvironmentVariable(key, null);
            }
        }
    }
}