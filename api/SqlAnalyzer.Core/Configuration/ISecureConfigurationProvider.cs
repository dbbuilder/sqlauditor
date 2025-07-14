using System.Threading.Tasks;

namespace SqlAnalyzer.Core.Configuration
{
    /// <summary>
    /// Interface for secure configuration providers
    /// </summary>
    public interface ISecureConfigurationProvider
    {
        /// <summary>
        /// Gets a configuration value by key
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <returns>The configuration value or null if not found</returns>
        Task<string> GetValueAsync(string key);

        /// <summary>
        /// Gets a connection string by name
        /// </summary>
        /// <param name="name">The connection string name</param>
        /// <returns>The connection string or null if not found</returns>
        Task<string> GetConnectionStringAsync(string name);

        /// <summary>
        /// Checks if a configuration key exists
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <returns>True if the key exists</returns>
        Task<bool> ContainsKeyAsync(string key);

        /// <summary>
        /// Gets the provider name for identification
        /// </summary>
        string ProviderName { get; }
    }
}