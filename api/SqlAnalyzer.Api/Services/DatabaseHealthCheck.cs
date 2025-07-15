using Microsoft.Extensions.Diagnostics.HealthChecks;
using SqlAnalyzer.Core.Connections;

namespace SqlAnalyzer.Api.Services
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly IConfiguration _configuration;

        public DatabaseHealthCheck(IConnectionFactory connectionFactory, IConfiguration configuration)
        {
            _connectionFactory = connectionFactory;
            _configuration = configuration;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("HealthCheck");
                if (string.IsNullOrEmpty(connectionString))
                {
                    return HealthCheckResult.Healthy("No health check database configured");
                }

                using var connection = _connectionFactory.CreateConnection(
                    connectionString,
                    DatabaseType.SqlServer);

                // Test connection by executing a simple query
                await connection.ExecuteScalarAsync("SELECT 1");

                return HealthCheckResult.Healthy("Database connection successful");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "Database connection failed",
                    ex,
                    new Dictionary<string, object>
                    {
                        ["error"] = ex.Message
                    });
            }
        }
    }
}