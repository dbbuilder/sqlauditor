using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;
using Polly;
using Polly.Retry;

namespace SqlAnalyzer.Core.Connections
{
    /// <summary>
    /// PostgreSQL implementation of ISqlAnalyzerConnection
    /// </summary>
    public class PostgreSqlConnection : SqlAnalyzerConnectionBase
    {
        private readonly AsyncRetryPolicy _retryPolicy;

        public override DatabaseType DatabaseType => DatabaseType.PostgreSql;

        public PostgreSqlConnection(string connectionString, ILogger<PostgreSqlConnection> logger)
            : base(connectionString, logger)
        {
            // Configure Polly retry policy for transient failures
            _retryPolicy = Policy
                .Handle<NpgsqlException>(ex => IsTransientError(ex))
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(exception,
                            "Transient error occurred. Retry attempt {RetryCount} after {TimeSpan}ms",
                            retryCount,
                            timeSpan.TotalMilliseconds);
                    });
        }

        protected override void ParseConnectionString()
        {
            try
            {
                var builder = new NpgsqlConnectionStringBuilder(ConnectionString);
                DatabaseName = builder.Database;
                ServerName = builder.Host;

                _logger.LogDebug("Parsed connection string - Server: {Server}, Database: {Database}",
                    ServerName, DatabaseName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse PostgreSQL connection string");
                throw new ArgumentException("Invalid PostgreSQL connection string", ex);
            }
        }

        protected override IDbConnection CreateConnection()
        {
            return new NpgsqlConnection(ConnectionString);
        }

        public override async Task<bool> TestConnectionAsync()
        {
            try
            {
                _logger.LogInformation("Testing connection to PostgreSQL database: {DatabaseName}", DatabaseName);

                using (var connection = new NpgsqlConnection(ConnectionString))
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

                _logger.LogInformation("Successfully connected to PostgreSQL database: {DatabaseName}", DatabaseName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to PostgreSQL database: {DatabaseName}", DatabaseName);
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
                        using (var reader = await ((NpgsqlCommand)command).ExecuteReaderAsync())
                        {
                            var dataTable = new DataTable();
                            dataTable.Load(reader);

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
                        using (var reader = await ((NpgsqlCommand)command).ExecuteReaderAsync())
                        {
                            var dataTable = new DataTable();
                            dataTable.Load(reader);

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
                        return await ((NpgsqlCommand)command).ExecuteScalarAsync();
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
                        using (var adapter = new NpgsqlDataAdapter((NpgsqlCommand)command))
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
                var query = "SELECT version()";

                var result = await ExecuteScalarAsync(query);

                if (result != null)
                {
                    return result.ToString();
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
                    SELECT pg_database_size(current_database()) / 1024.0 / 1024.0 AS size_mb";

                var result = await ExecuteScalarAsync(query);

                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToDecimal(result);
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
        /// Determines if a PostgreSQL exception is transient and should be retried
        /// </summary>
        private bool IsTransientError(NpgsqlException ex)
        {
            // PostgreSQL error codes that are considered transient
            string[] transientErrorCodes = {
                "08000", // connection_exception
                "08003", // connection_does_not_exist
                "08006", // connection_failure
                "08001", // sqlclient_unable_to_establish_sqlconnection
                "08004", // sqlserver_rejected_establishment_of_sqlconnection
                "40001", // serialization_failure
                "40P01", // deadlock_detected
                "53000", // insufficient_resources
                "53100", // disk_full
                "53200", // out_of_memory
                "53300", // too_many_connections
                "57P03", // cannot_connect_now
                "58000", // system_error
                "58030", // io_error
                "55P03", // lock_not_available
                "55006", // object_in_use
                "55000", // object_not_in_prerequisite_state
                "57014"  // query_canceled
            };

            if (ex.SqlState != null && Array.Exists(transientErrorCodes, code => code == ex.SqlState))
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