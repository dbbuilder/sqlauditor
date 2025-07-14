using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SqlAnalyzer.Core.Connections
{
    /// <summary>
    /// Base class for database connections
    /// </summary>
    public abstract class SqlAnalyzerConnectionBase : ISqlAnalyzerConnection
    {
        protected readonly ILogger _logger;
        protected IDbConnection _connection;
        protected IDbTransaction _currentTransaction;
        protected bool _disposed = false;

        public abstract DatabaseType DatabaseType { get; }
        public string ConnectionString { get; protected set; }
        public string DatabaseName { get; protected set; }
        public string ServerName { get; protected set; }

        protected SqlAnalyzerConnectionBase(string connectionString, ILogger logger)
        {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Parse connection string for database and server names
            ParseConnectionString();
            _connection = CreateConnection();
        }
        protected abstract void ParseConnectionString();
        protected abstract IDbConnection CreateConnection();
        public abstract Task<bool> TestConnectionAsync();
        public abstract Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null);
        public abstract Task<DataTable> ExecuteStoredProcedureAsync(string procedureName, Dictionary<string, object> parameters = null);
        public abstract Task<DataSet> ExecuteQueryMultipleAsync(string query, Dictionary<string, object> parameters = null);
        public abstract Task<object> ExecuteScalarAsync(string query, Dictionary<string, object> parameters = null);
        public abstract Task<string> GetDatabaseVersionAsync();
        public abstract Task<decimal> GetDatabaseSizeAsync();

        public virtual async Task OpenAsync()
        {
            try
            {
                if (_connection.State == ConnectionState.Open)
                    return;

                _logger.LogDebug("Opening connection to {DatabaseType} database: {DatabaseName}", DatabaseType, DatabaseName);

                if (_connection.State != ConnectionState.Open)
                {
                    await Task.Run(() => _connection.Open());
                }
                
                _logger.LogDebug("Successfully opened connection to database: {DatabaseName}", DatabaseName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open connection to database: {DatabaseName}", DatabaseName);
                throw;
            }
        }
        public virtual async Task CloseAsync()
        {
            if (_connection.State == ConnectionState.Closed)
                return;

            try
            {
                _logger.LogDebug("Closing connection to database: {DatabaseName}", DatabaseName);
                
                if (_currentTransaction != null)
                {
                    _currentTransaction.Rollback();
                    _currentTransaction.Dispose();
                    _currentTransaction = null;
                }
                
                if (_connection != null && _connection.State != ConnectionState.Closed)
                {
                    await Task.Run(() => _connection.Close());
                }
                
                _logger.LogDebug("Successfully closed connection to database: {DatabaseName}", DatabaseName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to close connection to database: {DatabaseName}", DatabaseName);
                throw;
            }
        }

        public virtual async Task<IDbTransaction> BeginTransactionAsync()
        {
            if (_connection.State != ConnectionState.Open)
                throw new InvalidOperationException("Connection must be open to begin a transaction");

            if (_currentTransaction != null)
                throw new InvalidOperationException("A transaction is already in progress");

            try
            {
                _logger.LogDebug("Beginning transaction on database: {DatabaseName}", DatabaseName);
                
                _currentTransaction = await Task.Run(() => _connection.BeginTransaction());
                return _currentTransaction;
            }            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to begin transaction on database: {DatabaseName}", DatabaseName);
                throw;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        if (_currentTransaction != null)
                        {
                            _currentTransaction.Rollback();
                            _currentTransaction.Dispose();
                        }

                        if (_connection != null)
                        {
                            if (_connection.State != ConnectionState.Closed)
                            {
                                _connection.Close();
                            }
                            _connection.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error disposing connection");
                    }
                }                _disposed = true;
            }
        }

        /// <summary>
        /// Creates a command with retry policy using Polly
        /// </summary>
        protected IDbCommand CreateCommandWithRetry(string commandText, CommandType commandType, Dictionary<string, object> parameters = null)
        {
            var command = _connection.CreateCommand();
            command.CommandText = commandText;
            command.CommandType = commandType;
            command.CommandTimeout = 300; // 5 minutes timeout
            command.Transaction = _currentTransaction;

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = param.Key;
                    parameter.Value = param.Value ?? DBNull.Value;
                    command.Parameters.Add(parameter);
                }
            }

            return command;
        }
    }
}