using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlAnalyzer.Core.Security;

namespace SqlAnalyzer.Core.Connections
{
    /// <summary>
    /// Factory for creating database connections with automatic type detection
    /// </summary>
    public class ConnectionFactory : IConnectionFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ConnectionFactory> _logger;
        private readonly IConnectionStringValidator _connectionStringValidator;
        private readonly List<ISqlAnalyzerConnection> _connections;
        private bool _disposed;

        public ConnectionFactory(IServiceProvider serviceProvider, ILogger<ConnectionFactory> logger)
            : this(serviceProvider, logger, new ConnectionStringValidator())
        {
        }

        public ConnectionFactory(IServiceProvider serviceProvider, ILogger<ConnectionFactory> logger, IConnectionStringValidator connectionStringValidator)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionStringValidator = connectionStringValidator ?? throw new ArgumentNullException(nameof(connectionStringValidator));
            _connections = new List<ISqlAnalyzerConnection>();
        }

        public ISqlAnalyzerConnection CreateConnection(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

            // Validate connection string
            var validationResult = _connectionStringValidator.Validate(connectionString);
            
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.Message));
                throw new ArgumentException($"Invalid connection string: {errorMessages}", nameof(connectionString));
            }

            // Log warnings if any
            foreach (var warning in validationResult.Warnings)
            {
                _logger.LogWarning("Connection string warning: {Code} - {Message}", warning.Code, warning.Message);
            }

            // Log security level
            _logger.LogInformation("Connection string security level: {SecurityLevel}", validationResult.SecurityLevel);

            var databaseType = DetectDatabaseType(connectionString);
            return CreateConnection(connectionString, databaseType);
        }

        public ISqlAnalyzerConnection CreateConnection(string connectionString, DatabaseType databaseType)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

            // Validate connection string
            var validationResult = _connectionStringValidator.Validate(connectionString);
            
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.Message));
                throw new ArgumentException($"Invalid connection string: {errorMessages}", nameof(connectionString));
            }

            _logger.LogInformation("Creating {DatabaseType} connection", databaseType);

            ISqlAnalyzerConnection connection = databaseType switch
            {
                DatabaseType.SqlServer => new SqlServerConnection(
                    connectionString, 
                    _serviceProvider.GetRequiredService<ILogger<SqlServerConnection>>()),
                
                DatabaseType.PostgreSql => new PostgreSqlConnection(
                    connectionString, 
                    _serviceProvider.GetRequiredService<ILogger<PostgreSqlConnection>>()),
                
                DatabaseType.MySql => new MySqlConnection(
                    connectionString, 
                    _serviceProvider.GetRequiredService<ILogger<MySqlConnection>>()),
                
                _ => throw new ArgumentException($"Unsupported database type: {databaseType}")
            };

            _connections.Add(connection);
            _logger.LogDebug("Created {DatabaseType} connection for database: {DatabaseName}", 
                databaseType, connection.DatabaseName);

            return connection;
        }

        private DatabaseType DetectDatabaseType(string connectionString)
        {
            var lowerConnectionString = connectionString.ToLowerInvariant();

            // SQL Server detection patterns
            if (ContainsAny(lowerConnectionString, "data source=", "server=", "initial catalog=", "integrated security=", "trusted_connection="))
            {
                // Additional check to differentiate from PostgreSQL which also uses "server="
                if (!ContainsAny(lowerConnectionString, "host=", "port=5432"))
                {
                    _logger.LogDebug("Detected SQL Server connection string");
                    return DatabaseType.SqlServer;
                }
            }

            // PostgreSQL detection patterns
            if (ContainsAny(lowerConnectionString, "host=", "port=5432", "postgresql", "postgres"))
            {
                _logger.LogDebug("Detected PostgreSQL connection string");
                return DatabaseType.PostgreSql;
            }

            // MySQL detection patterns
            if (ContainsAny(lowerConnectionString, "port=3306", "mysql", "uid=", "pwd="))
            {
                _logger.LogDebug("Detected MySQL connection string");
                return DatabaseType.MySql;
            }

            // Try more specific patterns
            if (lowerConnectionString.Contains("server=") && lowerConnectionString.Contains("database="))
            {
                // Check for MySQL-specific keywords
                if (ContainsAny(lowerConnectionString, "uid=", "pwd=", "user=", "password="))
                {
                    // If it has User Id or User ID (with space), it's likely SQL Server
                    if (lowerConnectionString.Contains("user id="))
                    {
                        _logger.LogDebug("Detected SQL Server connection string (user id pattern)");
                        return DatabaseType.SqlServer;
                    }
                    else
                    {
                        _logger.LogDebug("Detected MySQL connection string (uid/user pattern)");
                        return DatabaseType.MySql;
                    }
                }
            }

            _logger.LogError("Unable to determine database type from connection string");
            throw new ArgumentException("Unable to determine database type from connection string. Please use the overload that accepts DatabaseType parameter.");
        }

        private bool ContainsAny(string text, params string[] values)
        {
            return values.Any(value => text.Contains(value));
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
                _logger.LogDebug("Disposing ConnectionFactory and {Count} connections", _connections.Count);

                foreach (var connection in _connections)
                {
                    try
                    {
                        connection.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error disposing connection");
                    }
                }

                _connections.Clear();
            }

            _disposed = true;
        }
    }
}