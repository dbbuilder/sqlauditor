using System;

namespace SqlAnalyzer.Core.Connections
{
    /// <summary>
    /// Factory interface for creating database connections
    /// </summary>
    public interface IConnectionFactory : IDisposable
    {
        /// <summary>
        /// Creates a database connection based on the connection string
        /// </summary>
        /// <param name="connectionString">The database connection string</param>
        /// <returns>Database connection instance</returns>
        ISqlAnalyzerConnection CreateConnection(string connectionString);

        /// <summary>
        /// Creates a database connection with explicit database type
        /// </summary>
        /// <param name="connectionString">The database connection string</param>
        /// <param name="databaseType">The type of database</param>
        /// <returns>Database connection instance</returns>
        ISqlAnalyzerConnection CreateConnection(string connectionString, DatabaseType databaseType);
    }
}