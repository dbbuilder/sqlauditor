namespace SqlAnalyzer.Core.Configuration
{
    /// <summary>
    /// Factory interface for creating configuration providers
    /// </summary>
    public interface IConfigurationProviderFactory
    {
        /// <summary>
        /// Creates a configuration provider based on the type
        /// </summary>
        /// <param name="type">The type of configuration provider to create</param>
        /// <returns>Configuration provider instance</returns>
        ISecureConfigurationProvider Create(ConfigurationProviderType type);
    }
}