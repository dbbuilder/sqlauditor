using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using SqlAnalyzer.Core.Connections;
using Xunit;
using Xunit.Abstractions;

namespace SqlAnalyzer.Tests.IntegrationTests
{
    /// <summary>
    /// Integration tests for SQL Server connection - READ ONLY operations
    /// </summary>
    public class SqlServerConnectionIntegrationTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        public SqlServerConnectionIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestConnectionAsync_WithValidCredentials_ShouldReturnTrue()
        {
            // Arrange
            CheckSkipIntegrationTest();
            using var connection = CreateSqlServerConnection();
            
            // Act
            var result = await connection.TestConnectionAsync();
            
            // Assert
            result.Should().BeTrue();
            _output.WriteLine($"Successfully connected to database: {connection.DatabaseName} on server: {connection.ServerName}");
        }

        [Fact]
        public async Task GetDatabaseVersionAsync_ShouldReturnVersionInfo()
        {
            // Arrange
            CheckSkipIntegrationTest();
            using var connection = CreateSqlServerConnection();
            
            // Act
            var version = await connection.GetDatabaseVersionAsync();
            
            // Assert
            version.Should().NotBeNullOrWhiteSpace();
            version.Should().Contain("SQL Server");
            _output.WriteLine($"Database version: {version}");
        }

        [Fact]
        public async Task GetDatabaseSizeAsync_ShouldReturnPositiveSize()
        {
            // Arrange
            CheckSkipIntegrationTest();
            using var connection = CreateSqlServerConnection();
            
            // Act
            var sizeMB = await connection.GetDatabaseSizeAsync();
            
            // Assert
            sizeMB.Should().BeGreaterThan(0);
            _output.WriteLine($"Database size: {sizeMB:N2} MB");
        }

        [Fact]
        public async Task ExecuteQueryAsync_ReadOnlyQuery_ShouldReturnData()
        {
            // Arrange
            CheckSkipIntegrationTest();
            using var connection = CreateSqlServerConnection();
            
            // Act - Query system tables (always safe, read-only)
            var query = @"
                SELECT TOP 10 
                    s.name AS SchemaName,
                    t.name AS TableName,
                    t.create_date AS CreatedDate
                FROM sys.tables t
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                WHERE t.is_ms_shipped = 0
                ORDER BY t.create_date DESC";
            
            var result = await connection.ExecuteQueryAsync(query);
            
            // Assert
            result.Should().NotBeNull();
            result.Rows.Count.Should().BeGreaterThanOrEqualTo(0);
            
            _output.WriteLine($"Found {result.Rows.Count} user tables in the database");
            foreach (System.Data.DataRow row in result.Rows)
            {
                _output.WriteLine($"  - {row["SchemaName"]}.{row["TableName"]} (Created: {row["CreatedDate"]})");
            }
        }

        [Fact]
        public async Task ExecuteScalarAsync_CountQuery_ShouldReturnCount()
        {
            // Arrange
            CheckSkipIntegrationTest();
            using var connection = CreateSqlServerConnection();
            
            // Act - Count tables (read-only operation)
            var query = "SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0";
            var result = await connection.ExecuteScalarAsync(query);
            
            // Assert
            result.Should().NotBeNull();
            var count = Convert.ToInt32(result);
            count.Should().BeGreaterThanOrEqualTo(0);
            
            _output.WriteLine($"Total user tables in database: {count}");
        }

        [Fact]
        public async Task ExecuteQueryAsync_WithParameters_ShouldFilterResults()
        {
            // Arrange
            CheckSkipIntegrationTest();
            using var connection = CreateSqlServerConnection();
            
            // Act - Query with parameters (safe, read-only)
            var query = @"
                SELECT name, type_desc 
                FROM sys.objects 
                WHERE type = @ObjectType
                ORDER BY name";
            
            var parameters = new System.Collections.Generic.Dictionary<string, object>
            {
                { "@ObjectType", "U" } // U = User tables
            };
            
            var result = await connection.ExecuteQueryAsync(query, parameters);
            
            // Assert
            result.Should().NotBeNull();
            if (result.Rows.Count > 0)
            {
                var allUserTables = result.Rows.Cast<System.Data.DataRow>()
                    .All(row => row["type_desc"].ToString() == "USER_TABLE");
                allUserTables.Should().BeTrue();
            }
            
            _output.WriteLine($"Found {result.Rows.Count} user tables using parameterized query");
        }

        [Fact]
        public async Task OpenAsync_AndCloseAsync_ShouldWorkCorrectly()
        {
            // Arrange
            CheckSkipIntegrationTest();
            using var connection = CreateSqlServerConnection();
            
            // Act & Assert
            await connection.OpenAsync();
            
            // Execute a query to verify connection is open
            var query = "SELECT @@VERSION";
            var result = await connection.ExecuteScalarAsync(query);
            result.Should().NotBeNull();
            
            await connection.CloseAsync();
            
            // Connection should handle re-opening automatically
            var result2 = await connection.ExecuteScalarAsync(query);
            result2.Should().NotBeNull();
        }

        [Fact]
        public async Task BeginTransactionAsync_ReadOnlyTransaction_ShouldWork()
        {
            // Arrange
            CheckSkipIntegrationTest();
            using var connection = CreateSqlServerConnection();
            await connection.OpenAsync();
            
            // Act
            using var transaction = await connection.BeginTransactionAsync();
            
            // Execute read-only query within transaction
            var query = "SELECT COUNT(*) FROM sys.tables";
            var result = await connection.ExecuteScalarAsync(query);
            
            // Rollback (no changes were made anyway)
            transaction.Rollback();
            
            // Assert
            result.Should().NotBeNull();
            Convert.ToInt32(result).Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task GetDatabaseMetadata_ShouldReturnUsefulInformation()
        {
            // Arrange
            CheckSkipIntegrationTest();
            using var connection = CreateSqlServerConnection();
            
            // Act - Get various metadata (all read-only operations)
            var metadataQuery = @"
                SELECT 
                    DB_NAME() AS DatabaseName,
                    SUSER_SNAME() AS LoginName,
                    @@SERVERNAME AS ServerName,
                    @@VERSION AS ServerVersion,
                    (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0) AS UserTableCount,
                    (SELECT COUNT(*) FROM sys.procedures WHERE is_ms_shipped = 0) AS UserProcedureCount,
                    (SELECT COUNT(*) FROM sys.views WHERE is_ms_shipped = 0) AS UserViewCount";
            
            var result = await connection.ExecuteQueryAsync(metadataQuery);
            
            // Assert
            result.Should().NotBeNull();
            result.Rows.Count.Should().Be(1);
            
            var metadata = result.Rows[0];
            _output.WriteLine("Database Metadata:");
            _output.WriteLine($"  Database: {metadata["DatabaseName"]}");
            _output.WriteLine($"  Login: {metadata["LoginName"]}");
            _output.WriteLine($"  Server: {metadata["ServerName"]}");
            _output.WriteLine($"  Tables: {metadata["UserTableCount"]}");
            _output.WriteLine($"  Procedures: {metadata["UserProcedureCount"]}");
            _output.WriteLine($"  Views: {metadata["UserViewCount"]}");
        }
    }
}