using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace SqlAnalyzer.Core.Connections
{
    /// <summary>
    /// SQL Server implementation of ISqlAnalyzerConnection
    /// </summary>
    public class SqlServerConnection : SqlAnalyzerConnectionBase
    {
        private readonly AsyncRetryPolicy _retryPolicy;
        
        public override DatabaseType DatabaseType => DatabaseType.SqlServer;

        public SqlServerConnection(string connectionString, ILogger<SqlServerConnection> logger) 
            : base(connectionString, logger)
        {
            // Configure Polly retry policy for transient failures
            _retryPolicy = Policy
                .Handle<SqlException>(ex => IsTransientError(ex))
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {                        _logger.LogWarning(exception, 
                            "Transient error occurred. Retry attempt {RetryCount} after {TimeSpan}ms", 
                            retryCount, 
                            timeSpan.TotalMilliseconds);
                    });
        }

        protected override void ParseConnectionString()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(ConnectionString);
                DatabaseName = builder.InitialCatalog;
                ServerName = builder.DataSource;
                
                _logger.LogDebug("Parsed connection string - Server: {Server}, Database: {Database}", 
                    ServerName, DatabaseName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse SQL Server connection string");
                throw new ArgumentException("Invalid SQL Server connection string", ex);
            }
        }

        protected override IDbConnection CreateConnection()
        {
            return new SqlConnection(ConnectionString);
        }
        public override async Task<bool> TestConnectionAsync()
        {
            try
            {
                _logger.LogInformation("Testing connection to SQL Server database: {DatabaseName}", DatabaseName);
                
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await _retryPolicy.ExecuteAsync(async () =>
                    {
                        await connection.OpenAsync();
                        
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "SELECT 1";
                            await command.ExecuteScalarAsync();
                        }
                    });
                }
                
                _logger.LogInformation("Successfully connected to SQL Server database: {DatabaseName}", DatabaseName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to SQL Server database: {DatabaseName}", DatabaseName);
                return false;
            }
        }
        public override async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null)
        {
            try
            {
                _logger.LogDebug("Executing query on database: {DatabaseName}", DatabaseName);
                
                if (_connection.State != ConnectionState.Open)
                {
                    await OpenAsync();
                }

                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    using (var command = CreateCommandWithRetry(query, CommandType.Text, parameters))
                    {
                        using (var adapter = new SqlDataAdapter((SqlCommand)command))
                        {
                            var dataTable = new DataTable();
                            await Task.Run(() => adapter.Fill(dataTable));
                            
                            _logger.LogDebug("Query returned {RowCount} rows", dataTable.Rows.Count);
                            return dataTable;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute query: {Query}", query);
                throw;
            }
        }
        public override async Task<DataTable> ExecuteStoredProcedureAsync(string procedureName, Dictionary<string, object> parameters = null)
        {
            try
            {
                _logger.LogDebug("Executing stored procedure: {ProcedureName} on database: {DatabaseName}", 
                    procedureName, DatabaseName);
                
                if (_connection.State != ConnectionState.Open)
                {
                    await OpenAsync();
                }

                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    using (var command = CreateCommandWithRetry(procedureName, CommandType.StoredProcedure, parameters))
                    {
                        using (var adapter = new SqlDataAdapter((SqlCommand)command))
                        {
                            var dataTable = new DataTable();
                            await Task.Run(() => adapter.Fill(dataTable));
                            
                            _logger.LogDebug("Stored procedure returned {RowCount} rows", dataTable.Rows.Count);
                            return dataTable;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute stored procedure: {ProcedureName}", procedureName);
                throw;
            }
        }
        public override async Task<object> ExecuteScalarAsync(string query, Dictionary<string, object> parameters = null)
        {
            try
            {
                _logger.LogDebug("Executing scalar query on database: {DatabaseName}", DatabaseName);
                
                if (_connection.State != ConnectionState.Open)
                {
                    await OpenAsync();
                }

                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    using (var command = CreateCommandWithRetry(query, CommandType.Text, parameters))
                    {
                        return await ((SqlCommand)command).ExecuteScalarAsync();
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute scalar query: {Query}", query);
                throw;
            }
        }

        public override async Task<DataSet> ExecuteQueryMultipleAsync(string query, Dictionary<string, object> parameters = null)
        {
            try
            {
                _logger.LogDebug("Executing multi-result query on database: {DatabaseName}", DatabaseName);
                
                if (_connection.State != ConnectionState.Open)
                {
                    await OpenAsync();
                }

                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    using (var command = CreateCommandWithRetry(query, CommandType.Text, parameters))
                    {
                        using (var adapter = new SqlDataAdapter((SqlCommand)command))
                        {
                            var dataSet = new DataSet();
                            await Task.Run(() => adapter.Fill(dataSet));
                            
                            _logger.LogDebug("Query returned {TableCount} result sets", dataSet.Tables.Count);
                            return dataSet;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute multi-result query");
                throw;
            }
        }
        public override async Task<string> GetDatabaseVersionAsync()
        {
            try
            {
                var query = @"
                    SELECT 
                        SERVERPROPERTY('ProductVersion') AS Version,
                        SERVERPROPERTY('ProductLevel') AS ProductLevel,
                        SERVERPROPERTY('Edition') AS Edition";
                
                var result = await ExecuteQueryAsync(query);
                
                if (result.Rows.Count > 0)
                {
                    var row = result.Rows[0];
                    return $"SQL Server {row["Version"]} {row["ProductLevel"]} - {row["Edition"]}";
                }
                
                return "Unknown";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get database version");
                throw;
            }
        }
        public override async Task<decimal> GetDatabaseSizeAsync()
        {
            try
            {
                var query = @"
                    SELECT 
                        SUM(CAST(size AS BIGINT)) * 8.0 / 1024 AS SizeMB
                    FROM sys.database_files
                    WHERE type_desc = 'ROWS'";
                
                var result = await ExecuteQueryAsync(query);
                
                if (result.Rows.Count > 0 && result.Rows[0]["SizeMB"] != DBNull.Value)
                {
                    return Convert.ToDecimal(result.Rows[0]["SizeMB"]);
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get database size");
                throw;
            }
        }
        /// <summary>
        /// Determines if a SQL exception is transient and should be retried
        /// </summary>
        private bool IsTransientError(SqlException ex)
        {
            // List of SQL Server error numbers that are considered transient
            int[] transientErrors = { 
                49918, // Cannot process request. Not enough resources to process request
                49919, // Cannot process create or update request. Too many create or update operations in progress
                49920, // Cannot process request. Too many operations in progress
                4060,  // Cannot open database requested by the login
                40143, // The service has encountered an error processing your request
                233,   // Connection initialization error
                64,    // A connection was successfully established but then an error occurred
                20,    // The instance of SQL Server does not support encryption
                0      // Timeout
            };

            foreach (SqlError error in ex.Errors)
            {
                if (Array.Exists(transientErrors, e => e == error.Number))
                {
                    return true;
                }
            }

            // Also check for timeout exceptions
            if (ex.Number == -2 || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }
}