using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace SqlAnalyzer.Core.Connections
{
    /// <summary>
    /// MySQL implementation of ISqlAnalyzerConnection
    /// </summary>
    public class MySqlConnection : SqlAnalyzerConnectionBase
    {
        private readonly AsyncRetryPolicy _retryPolicy;
        
        public override DatabaseType DatabaseType => DatabaseType.MySql;

        public MySqlConnection(string connectionString, ILogger<MySqlConnection> logger) 
            : base(connectionString, logger)
        {
            // Configure Polly retry policy for transient failures
            _retryPolicy = Policy
                .Handle<MySqlException>(ex => IsTransientError(ex))
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
                var builder = new MySqlConnectionStringBuilder(ConnectionString);
                DatabaseName = builder.Database;
                ServerName = builder.Server;
                
                _logger.LogDebug("Parsed connection string - Server: {Server}, Database: {Database}", 
                    ServerName, DatabaseName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse MySQL connection string");
                throw new ArgumentException("Invalid MySQL connection string", ex);
            }
        }

        protected override IDbConnection CreateConnection()
        {
            return new global::MySql.Data.MySqlClient.MySqlConnection(ConnectionString);
        }
        public override async Task<bool> TestConnectionAsync()
        {
            try
            {
                _logger.LogInformation("Testing connection to MySQL database: {DatabaseName}", DatabaseName);
                
                using (var connection = new global::MySql.Data.MySqlClient.MySqlConnection(ConnectionString))
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
                
                _logger.LogInformation("Successfully connected to MySQL database: {DatabaseName}", DatabaseName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to MySQL database: {DatabaseName}", DatabaseName);
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
                        using (var adapter = new MySqlDataAdapter((MySqlCommand)command))
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
                        using (var adapter = new MySqlDataAdapter((MySqlCommand)command))
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
                        return await ((MySqlCommand)command).ExecuteScalarAsync();
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
                        using (var adapter = new MySqlDataAdapter((MySqlCommand)command))
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
                var query = "SELECT VERSION() AS Version, @@version_comment AS Comment";
                
                var result = await ExecuteQueryAsync(query);
                
                if (result.Rows.Count > 0)
                {
                    var row = result.Rows[0];
                    return $"MySQL {row["Version"]} - {row["Comment"]}";
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
                        SUM(data_length + index_length) / 1024 / 1024 AS SizeMB
                    FROM information_schema.tables
                    WHERE table_schema = DATABASE()";
                
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
        /// Determines if a MySQL exception is transient and should be retried
        /// </summary>
        private bool IsTransientError(MySqlException ex)
        {
            // List of MySQL error codes that are considered transient
            var transientErrors = new[] { 
                1040, // Too many connections
                1042, // Can't get hostname for your address
                1043, // Bad handshake
                1044, // Access denied for user
                1045, // Access denied for user (using password)
                1053, // Server shutdown in progress
                1077, // Shutdown in progress
                1152, // Aborted connection
                1153, // Got a packet bigger than max_allowed_packet
                1154, // Got a read error from the connection pipe
                1155, // Got an error from fcntl()
                1156, // Got packets out of order
                1157, // Couldn't uncompress communication packet
                1158, // Got an error reading communication packets
                1159, // Got timeout reading communication packets
                1160, // Got an error writing communication packets
                1161, // Got timeout writing communication packets
                1205, // Lock wait timeout exceeded
                1213, // Deadlock found when trying to get lock
                2002, // Can't connect to local MySQL server
                2003, // Can't connect to MySQL server
                2006, // MySQL server has gone away
                2013  // Lost connection to MySQL server during query
            };

            if (Array.Exists(transientErrors, e => e == ex.Number))
            {
                return true;
            }

            // Also check for timeout exceptions
            if (ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }
}