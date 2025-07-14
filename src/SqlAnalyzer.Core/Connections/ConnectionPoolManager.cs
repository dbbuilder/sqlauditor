using System;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SqlAnalyzer.Core.Connections
{
    /// <summary>
    /// Manages database connection pools for optimal performance
    /// </summary>
    public class ConnectionPoolManager : IConnectionPoolManager
    {
        private readonly ILogger<ConnectionPoolManager> _logger;
        private readonly IConnectionFactory _connectionFactory;
        private readonly ConcurrentDictionary<string, ConnectionPoolStatistics> _statistics;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _poolSemaphores;
        private bool _disposed;

        public ConnectionPoolManager(ILogger<ConnectionPoolManager> logger, IConnectionFactory connectionFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _statistics = new ConcurrentDictionary<string, ConnectionPoolStatistics>();
            _poolSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
        }

        public async Task<bool> WarmConnectionPoolAsync(string connectionString, DatabaseType databaseType, int poolSize = 10)
        {
            _logger.LogInformation("Warming connection pool for {DatabaseType} with {PoolSize} connections", 
                databaseType, poolSize);

            var tasks = new Task[poolSize];
            var success = true;

            try
            {
                for (int i = 0; i < poolSize; i++)
                {
                    tasks[i] = Task.Run(async () =>
                    {
                        try
                        {
                            using var connection = _connectionFactory.CreateConnection(connectionString, databaseType);
                            await connection.OpenAsync();
                            
                            // Execute a simple query to ensure connection is fully established
                            var query = GetTestQuery(databaseType);
                            await connection.ExecuteScalarAsync(query);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to warm connection");
                            success = false;
                        }
                    });
                }

                await Task.WhenAll(tasks);
                
                _logger.LogInformation("Connection pool warmed successfully with {SuccessCount} connections", 
                    tasks.Count(t => !t.IsFaulted));
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to warm connection pool");
                return false;
            }
        }

        public async Task<ConnectionPoolStatistics> GetPoolStatisticsAsync(string connectionString, DatabaseType databaseType)
        {
            var key = GetPoolKey(connectionString, databaseType);
            
            if (!_statistics.TryGetValue(key, out var stats))
            {
                stats = new ConnectionPoolStatistics
                {
                    ConnectionString = connectionString,
                    DatabaseType = databaseType,
                    LastAccessed = DateTime.UtcNow
                };
                _statistics.TryAdd(key, stats);
            }

            // Update statistics based on database type
            await UpdatePoolStatistics(stats, connectionString, databaseType);
            
            return stats;
        }

        public void ClearPool(string connectionString, DatabaseType databaseType)
        {
            _logger.LogInformation("Clearing connection pool for {DatabaseType}", databaseType);

            try
            {
                // Note: Direct pool clearing is not available through ISqlAnalyzerConnection
                // This would require provider-specific implementation
                // For now, we'll just remove from our statistics and let the pool manage itself
                
                var key = GetPoolKey(connectionString, databaseType);
                _statistics.TryRemove(key, out _);
                
                _logger.LogInformation("Connection pool statistics cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear connection pool");
                throw;
            }
        }

        public string ConfigurePool(string connectionString, ConnectionPoolSettings settings, DatabaseType databaseType)
        {
            // Parse existing connection string
            var parts = connectionString.Split(';')
                .Select(p => p.Split('='))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);

            // Apply pool settings based on database type
            switch (databaseType)
            {
                case DatabaseType.SqlServer:
                    parts["Min Pool Size"] = settings.MinPoolSize.ToString();
                    parts["Max Pool Size"] = settings.MaxPoolSize.ToString();
                    parts["Connect Timeout"] = settings.ConnectionTimeout.ToString();
                    parts["Pooling"] = settings.PoolingEnabled.ToString();
                    if (settings.ConnectionLifetime > 0)
                        parts["Load Balance Timeout"] = settings.ConnectionLifetime.ToString();
                    break;
                    
                case DatabaseType.PostgreSql:
                    parts["MinPoolSize"] = settings.MinPoolSize.ToString();
                    parts["MaxPoolSize"] = settings.MaxPoolSize.ToString();
                    parts["Timeout"] = settings.ConnectionTimeout.ToString();
                    parts["Pooling"] = settings.PoolingEnabled.ToString();
                    if (settings.ConnectionLifetime > 0)
                        parts["Connection Idle Lifetime"] = settings.ConnectionLifetime.ToString();
                    break;
                    
                case DatabaseType.MySql:
                    parts["MinimumPoolSize"] = settings.MinPoolSize.ToString();
                    parts["MaximumPoolSize"] = settings.MaxPoolSize.ToString();
                    parts["ConnectionTimeout"] = settings.ConnectionTimeout.ToString();
                    parts["Pooling"] = settings.PoolingEnabled.ToString();
                    if (settings.ConnectionLifetime > 0)
                        parts["ConnectionLifeTime"] = settings.ConnectionLifetime.ToString();
                    break;
            }

            // Rebuild connection string
            return string.Join(";", parts.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }

        public async Task<ConnectionPoolHealth> MonitorPoolHealthAsync(string connectionString, DatabaseType databaseType)
        {
            var health = new ConnectionPoolHealth
            {
                ConnectionString = connectionString,
                CheckedAt = DateTime.UtcNow
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var connection = _connectionFactory.CreateConnection(connectionString, databaseType);
                await connection.OpenAsync();
                
                var query = GetTestQuery(databaseType);
                await connection.ExecuteScalarAsync(query);

                health.IsHealthy = true;
                health.HealthMessage = "Connection pool is healthy";
                health.SuccessfulConnections++;
            }
            catch (Exception ex)
            {
                health.IsHealthy = false;
                health.HealthMessage = $"Connection pool is unhealthy: {ex.Message}";
                health.FailedConnections++;
                _logger.LogError(ex, "Connection pool health check failed");
            }
            finally
            {
                stopwatch.Stop();
                health.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
            }

            return health;
        }

        // Connection creation is now handled by IConnectionFactory
        // Configuration methods removed - now handled inline in ConfigurePool

        private string GetTestQuery(DatabaseType databaseType)
        {
            return databaseType switch
            {
                DatabaseType.SqlServer => "SELECT 1",
                DatabaseType.PostgreSql => "SELECT 1",
                DatabaseType.MySql => "SELECT 1",
                _ => "SELECT 1"
            };
        }

        private string GetPoolKey(string connectionString, DatabaseType databaseType)
        {
            return $"{databaseType}:{connectionString.GetHashCode()}";
        }

        private async Task UpdatePoolStatistics(ConnectionPoolStatistics stats, string connectionString, DatabaseType databaseType)
        {
            // Note: Getting actual pool statistics is database-specific and may require
            // database-specific queries or APIs. This is a simplified version.
            
            stats.LastAccessed = DateTime.UtcNow;
            stats.TotalConnections = stats.ActiveConnections + stats.AvailableConnections;
            
            // For demonstration, we'll attempt to get some basic info
            try
            {
                using var connection = _connectionFactory.CreateConnection(connectionString, databaseType);
                await connection.OpenAsync();
                stats.TotalConnectionsCreated++;
            }
            catch
            {
                // Ignore errors in statistics gathering
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _logger.LogInformation("Disposing ConnectionPoolManager");
                
                // Clear all known pools
                foreach (var stat in _statistics.Values)
                {
                    try
                    {
                        ClearPool(stat.ConnectionString, stat.DatabaseType);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error clearing pool during disposal");
                    }
                }
                
                _statistics.Clear();
            }

            _disposed = true;
        }
    }
}