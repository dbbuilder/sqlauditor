using System;
using System.Threading.Tasks;

namespace SqlAnalyzer.Core.Configuration
{
    /// <summary>
    /// Configuration provider that reads from environment variables
    /// </summary>
    public class EnvironmentVariableProvider : ISecureConfigurationProvider
    {
        private readonly string _prefix;

        public string ProviderName => "EnvironmentVariables";

        /// <summary>
        /// Creates a new environment variable configuration provider
        /// </summary>
        /// <param name="prefix">Optional prefix for environment variables (e.g., "SQLANALYZER_")</param>
        public EnvironmentVariableProvider(string prefix = null)
        {
            _prefix = prefix ?? string.Empty;
        }

        public Task<string> GetValueAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Task.FromResult<string>(null);

            var envKey = $"{_prefix}{key}";
            var value = Environment.GetEnvironmentVariable(envKey);

            // Also try without prefix if not found
            if (value == null && !string.IsNullOrEmpty(_prefix))
            {
                value = Environment.GetEnvironmentVariable(key);
            }

            return Task.FromResult(value);
        }

        public async Task<string> GetConnectionStringAsync(string name)
        {
            // Try common patterns
            var patterns = new[]
            {
                $"ConnectionStrings__{name}",
                $"ConnectionStrings:{name}",
                $"{name}_CONNECTION",
                $"{name}_CONNECTION_STRING",
                name
            };

            foreach (var pattern in patterns)
            {
                var value = await GetValueAsync(pattern);
                if (!string.IsNullOrEmpty(value))
                    return value;
            }

            return null;
        }

        public async Task<bool> ContainsKeyAsync(string key)
        {
            var value = await GetValueAsync(key);
            return !string.IsNullOrEmpty(value);
        }
    }
}