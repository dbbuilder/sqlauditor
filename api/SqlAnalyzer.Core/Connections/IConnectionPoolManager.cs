using System;
using System.Threading.Tasks;

namespace SqlAnalyzer.Core.Connections
{
    /// <summary>
    /// Interface for managing database connection pools
    /// </summary>
    public interface IConnectionPoolManager : IDisposable
    {
        /// <summary>
        /// Warms up the connection pool by creating and closing connections
        /// </summary>
        Task<bool> WarmConnectionPoolAsync(string connectionString, DatabaseType databaseType, int poolSize = 10);

        /// <summary>
        /// Gets statistics about the connection pool
        /// </summary>
        Task<ConnectionPoolStatistics> GetPoolStatisticsAsync(string connectionString, DatabaseType databaseType);

        /// <summary>
        /// Clears all connections from the pool
        /// </summary>
        void ClearPool(string connectionString, DatabaseType databaseType);

        /// <summary>
        /// Configures connection pool settings
        /// </summary>
        string ConfigurePool(string connectionString, ConnectionPoolSettings settings, DatabaseType databaseType);

        /// <summary>
        /// Monitors the health of the connection pool
        /// </summary>
        Task<ConnectionPoolHealth> MonitorPoolHealthAsync(string connectionString, DatabaseType databaseType);
    }

    /// <summary>
    /// Connection pool statistics
    /// </summary>
    public class ConnectionPoolStatistics
    {
        public string ConnectionString { get; set; }
        public DatabaseType DatabaseType { get; set; }
        public int ActiveConnections { get; set; }
        public int AvailableConnections { get; set; }
        public int TotalConnections { get; set; }
        public DateTime LastAccessed { get; set; }
        public long TotalConnectionsCreated { get; set; }
        public long TotalConnectionsDisposed { get; set; }
    }

    /// <summary>
    /// Connection pool settings
    /// </summary>
    public class ConnectionPoolSettings
    {
        public int MinPoolSize { get; set; } = 0;
        public int MaxPoolSize { get; set; } = 100;
        public int ConnectionTimeout { get; set; } = 30;
        public int ConnectionLifetime { get; set; } = 0;
        public bool PoolingEnabled { get; set; } = true;
        public bool LoadBalanceTimeout { get; set; } = false;
    }

    /// <summary>
    /// Connection pool health status
    /// </summary>
    public class ConnectionPoolHealth
    {
        public string ConnectionString { get; set; }
        public bool IsHealthy { get; set; }
        public string HealthMessage { get; set; }
        public DateTime CheckedAt { get; set; }
        public double ResponseTimeMs { get; set; }
        public int FailedConnections { get; set; }
        public int SuccessfulConnections { get; set; }
    }
}