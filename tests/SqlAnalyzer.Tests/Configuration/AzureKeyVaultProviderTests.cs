using System;
using System.Threading.Tasks;
using FluentAssertions;
using SqlAnalyzer.Core.Configuration;
using Xunit;

namespace SqlAnalyzer.Tests.Configuration
{
    public class AzureKeyVaultProviderTests
    {
        [Fact]
        public void Constructor_WithNullUrl_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var act = () => new AzureKeyVaultProvider(null);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("keyVaultUrl");
        }

        [Fact]
        public void Constructor_WithEmptyUrl_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var act = () => new AzureKeyVaultProvider("");
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("keyVaultUrl");
        }

        [Fact]
        public void Constructor_WithValidUrl_ShouldCreateInstance()
        {
            // Arrange & Act
            var provider = new AzureKeyVaultProvider("https://myvault.vault.azure.net/");

            // Assert
            provider.Should().NotBeNull();
            provider.ProviderName.Should().Be("AzureKeyVault");
        }

        [Fact]
        public async Task GetValueAsync_ShouldThrowNotImplementedException()
        {
            // Arrange
            var provider = new AzureKeyVaultProvider("https://myvault.vault.azure.net/");

            // Act & Assert
            await provider.Invoking(p => p.GetValueAsync("test-key"))
                .Should().ThrowAsync<NotImplementedException>()
                .WithMessage("*Azure Key Vault provider is not yet implemented*");
        }

        [Fact]
        public async Task GetConnectionStringAsync_ShouldThrowNotImplementedException()
        {
            // Arrange
            var provider = new AzureKeyVaultProvider("https://myvault.vault.azure.net/");

            // Act & Assert
            await provider.Invoking(p => p.GetConnectionStringAsync("TestDb"))
                .Should().ThrowAsync<NotImplementedException>()
                .WithMessage("*Azure Key Vault provider is not yet implemented*");
        }

        [Fact]
        public async Task ContainsKeyAsync_ShouldThrowNotImplementedException()
        {
            // Arrange
            var provider = new AzureKeyVaultProvider("https://myvault.vault.azure.net/");

            // Act & Assert
            await provider.Invoking(p => p.ContainsKeyAsync("test-key"))
                .Should().ThrowAsync<NotImplementedException>()
                .WithMessage("*Azure Key Vault provider is not yet implemented*");
        }
    }
}