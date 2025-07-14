using System;
using System.Collections.Generic;
using FluentAssertions;
using SqlAnalyzer.Core.Configuration;
using Xunit;

namespace SqlAnalyzer.Tests.Configuration
{
    public class ConfigurationProviderFactoryTests
    {
        [Fact]
        public void CreateProvider_WithEnvironmentVariableType_ShouldReturnEnvironmentProvider()
        {
            // Act
            var provider = ConfigurationProviderFactory.CreateProvider(ConfigurationProviderType.EnvironmentVariables);

            // Assert
            provider.Should().BeOfType<EnvironmentVariableProvider>();
            provider.ProviderName.Should().Be("EnvironmentVariables");
        }

        [Fact]
        public void CreateProvider_WithEnvironmentVariableTypeAndPrefix_ShouldReturnPrefixedProvider()
        {
            // Arrange
            var options = new Dictionary<string, string> { ["prefix"] = "TEST_" };

            // Act
            var provider = ConfigurationProviderFactory.CreateProvider(
                ConfigurationProviderType.EnvironmentVariables, 
                options);

            // Assert
            provider.Should().BeOfType<EnvironmentVariableProvider>();
        }

        [Fact]
        public void CreateProvider_WithAzureKeyVaultType_ShouldReturnKeyVaultProvider()
        {
            // Arrange
            var options = new Dictionary<string, string> 
            { 
                ["keyVaultUrl"] = "https://myvault.vault.azure.net/" 
            };

            // Act
            var provider = ConfigurationProviderFactory.CreateProvider(
                ConfigurationProviderType.AzureKeyVault, 
                options);

            // Assert
            provider.Should().BeOfType<AzureKeyVaultProvider>();
            provider.ProviderName.Should().Be("AzureKeyVault");
        }

        [Fact]
        public void CreateProvider_WithAzureKeyVaultTypeAndNoUrl_ShouldThrowArgumentException()
        {
            // Act & Assert
            var act = () => ConfigurationProviderFactory.CreateProvider(ConfigurationProviderType.AzureKeyVault);
            
            act.Should().Throw<ArgumentException>()
                .WithMessage("*keyVaultUrl is required for Azure Key Vault provider*");
        }

        [Fact]
        public void CreateProvider_WithCompositeType_ShouldThrowNotImplementedException()
        {
            // Act & Assert
            var act = () => ConfigurationProviderFactory.CreateProvider(ConfigurationProviderType.Composite);
            
            act.Should().Throw<NotImplementedException>()
                .WithMessage("*Composite provider not yet implemented*");
        }

        [Fact]
        public void CreateDefault_InNonAzureEnvironment_ShouldReturnEnvironmentProvider()
        {
            // Arrange - ensure Azure environment variables are not set
            Environment.SetEnvironmentVariable("WEBSITE_INSTANCE_ID", null);
            Environment.SetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT", null);

            // Act
            var provider = ConfigurationProviderFactory.CreateDefault();

            // Assert
            provider.Should().BeOfType<EnvironmentVariableProvider>();
            provider.ProviderName.Should().Be("EnvironmentVariables");
        }

        [Fact]
        public void CreateDefault_InAzureWebApp_ShouldReturnEnvironmentProvider()
        {
            // Arrange
            Environment.SetEnvironmentVariable("WEBSITE_INSTANCE_ID", "test-instance");

            try
            {
                // Act
                var provider = ConfigurationProviderFactory.CreateDefault();

                // Assert
                provider.Should().BeOfType<EnvironmentVariableProvider>();
                provider.ProviderName.Should().Be("EnvironmentVariables");
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("WEBSITE_INSTANCE_ID", null);
            }
        }

        [Fact]
        public void CreateDefault_InAzureFunctions_ShouldReturnEnvironmentProvider()
        {
            // Arrange
            Environment.SetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT", "Development");

            try
            {
                // Act
                var provider = ConfigurationProviderFactory.CreateDefault();

                // Assert
                provider.Should().BeOfType<EnvironmentVariableProvider>();
                provider.ProviderName.Should().Be("EnvironmentVariables");
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT", null);
            }
        }
    }
}