using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqlAnalyzer.Core.Connections
{
    /// <summary>
    /// Interface for database connections used by the SQL Analyzer
    /// </summary>
    public interface ISqlAnalyzerConnection : IDisposable
    {
        /// <summary>
        /// Gets the database type (SQLServer, PostgreSQL, or MySQL)
        /// </summary>
        DatabaseType DatabaseType { get; }

        /// <summary>
        /// Gets the connection string
        /// </summary>
        string ConnectionString { get; }

        /// <summary>
        /// Gets the database name
        /// </summary>
        string DatabaseName { get; }

        /// <summary>
        /// Gets the server/host name
        /// </summary>
        string ServerName { get; }
        /// <summary>
        /// Tests the database connection
        /// </summary>
        /// <returns>True if connection is successful</returns>
        Task<bool> TestConnectionAsync();

        /// <summary>
        /// Executes a query and returns a DataTable
        /// </summary>
        /// <param name="query">SQL query to execute</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>DataTable with results</returns>
        Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null);

        /// <summary>
        /// Executes a stored procedure and returns a DataTable
        /// </summary>
        /// <param name="procedureName">Name of the stored procedure</param>
        /// <param name="parameters">Procedure parameters</param>
        /// <returns>DataTable with results</returns>
        Task<DataTable> ExecuteStoredProcedureAsync(string procedureName, Dictionary<string, object> parameters = null);

        /// <summary>
        /// Executes a query and returns multiple result sets
        /// </summary>
        /// <param name="query">SQL query to execute</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>DataSet with multiple tables</returns>
        Task<DataSet> ExecuteQueryMultipleAsync(string query, Dictionary<string, object> parameters = null);

        /// <summary>
        /// Executes a query and returns a scalar value
        /// </summary>
        /// <param name="query">SQL query to execute</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Scalar result</returns>
        Task<object> ExecuteScalarAsync(string query, Dictionary<string, object> parameters = null);
        /// <summary>
        /// Gets the database version
        /// </summary>
        /// <returns>Database version string</returns>
        Task<string> GetDatabaseVersionAsync();

        /// <summary>
        /// Gets the database size in MB
        /// </summary>
        /// <returns>Database size</returns>
        Task<decimal> GetDatabaseSizeAsync();

        /// <summary>
        /// Opens the database connection
        /// </summary>
        Task OpenAsync();

        /// <summary>
        /// Closes the database connection
        /// </summary>
        Task CloseAsync();

        /// <summary>
        /// Begins a database transaction
        /// </summary>
        /// <returns>Transaction object</returns>
        Task<IDbTransaction> BeginTransactionAsync();
    }

    /// <summary>
    /// Enum for supported database types
    /// </summary>
    public enum DatabaseType
    {
        SqlServer,
        PostgreSql,
        MySql
    }
}