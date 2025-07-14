using System;
using System.Collections.Generic;

namespace SqlAnalyzer.Core.Configuration
{
    /// <summary>
    /// Factory for creating secure configuration providers
    /// </summary>
    public class ConfigurationProviderFactory
    {
        /// <summary>
        /// Creates a configuration provider based on the specified type
        /// </summary>
        /// <param name="providerType">The type of provider to create</param>
        /// <param name="options">Provider-specific options</param>
        /// <returns>The configuration provider</returns>
        public static ISecureConfigurationProvider CreateProvider(
            ConfigurationProviderType providerType, 
            Dictionary<string, string> options = null)
        {
            options ??= new Dictionary<string, string>();

            return providerType switch
            {
                ConfigurationProviderType.EnvironmentVariables => 
                    new EnvironmentVariableProvider(options.GetValueOrDefault("prefix")),
                
                ConfigurationProviderType.AzureKeyVault => 
                    new AzureKeyVaultProvider(options.GetValueOrDefault("keyVaultUrl") 
                        ?? throw new ArgumentException("keyVaultUrl is required for Azure Key Vault provider")),
                
                ConfigurationProviderType.Composite => 
                    throw new NotImplementedException("Composite provider not yet implemented"),
                
                _ => throw new ArgumentException($"Unknown provider type: {providerType}")
            };
        }

        /// <summary>
        /// Creates the default configuration provider based on environment
        /// </summary>
        /// <returns>The default configuration provider</returns>
        public static ISecureConfigurationProvider CreateDefault()
        {
            // Check if running in Azure (common Azure environment variables)
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID")) ||
                !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT")))
            {
                // In Azure, prefer environment variables (managed identity would use Key Vault)
                return new EnvironmentVariableProvider("SQLANALYZER_");
            }

            // Default to environment variables with prefix
            return new EnvironmentVariableProvider("SQLANALYZER_");
        }
    }

    /// <summary>
    /// Types of configuration providers
    /// </summary>
    public enum ConfigurationProviderType
    {
        /// <summary>
        /// Environment variables provider
        /// </summary>
        EnvironmentVariables,

        /// <summary>
        /// Azure Key Vault provider
        /// </summary>
        AzureKeyVault,

        /// <summary>
        /// Composite provider that chains multiple providers
        /// </summary>
        Composite
    }
}