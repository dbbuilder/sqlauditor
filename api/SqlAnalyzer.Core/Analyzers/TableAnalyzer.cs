using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SqlAnalyzer.Core.Connections;
using SqlAnalyzer.Core.Models;

namespace SqlAnalyzer.Core.Analyzers
{
    /// <summary>
    /// Analyzes database tables for structure, design issues, and best practices
    /// </summary>
    public class TableAnalyzer : BaseAnalyzer<TableInfo>, ISchemaAnalyzer
    {
        public override string Name => "Table Analyzer";
        public override string Description => "Analyzes table structure, identifies design issues, and checks best practices";
        public override string Category => "Schema";

        public string SchemaFilter { get; set; }
        public string ObjectNamePattern { get; set; }

        public TableAnalyzer(ISqlAnalyzerConnection connection, ILogger<TableAnalyzer> logger)
            : base(connection, logger)
        {
        }

        protected override async Task<List<TableInfo>> CollectDataAsync()
        {
            var tables = new List<TableInfo>();

            try
            {
                string query = GetTableQuery();

                // Add SchemaFilter to query if specified
                if (!string.IsNullOrWhiteSpace(SchemaFilter))
                {
                    query = query.Replace("WHERE t.type = 'U' AND t.is_ms_shipped = 0",
                                       "WHERE t.type = 'U' AND t.is_ms_shipped = 0 AND s.name = @SchemaFilter");
                }

                var parameters = new Dictionary<string, object>();
                if (!string.IsNullOrWhiteSpace(SchemaFilter))
                {
                    parameters.Add("@SchemaFilter", SchemaFilter);
                }

                var dataTable = await _connection.ExecuteQueryAsync(query, parameters);

                foreach (DataRow row in dataTable.Rows)
                {
                    tables.Add(MapRowToTableInfo(row));
                }

                _logger.LogDebug("Collected information for {TableCount} tables", tables.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting table information");
                throw;
            }

            return tables;
        }

        protected override async Task AnalyzeDataAsync(List<TableInfo> tables)
        {
            foreach (var table in tables.Where(t => !t.IsSystemTable))
            {
                // Check for tables without primary keys
                if (!table.HasPrimaryKey)
                {
                    AddFindingWithScript(
                        Severity.Critical,
                        $"Table {table.FullName} has no primary key",
                        "Every table should have a primary key to ensure data integrity and improve query performance",
                        GeneratePrimaryKeyScript(table),
                        affectedObject: table.FullName,
                        objectType: "Table"
                    );
                }

                // Check for heap tables (SQL Server specific)
                if (_connection.DatabaseType == DatabaseType.SqlServer && table.IsHeap && table.RowCount > 1000)
                {
                    AddFindingWithScript(
                        Severity.Error,
                        $"Table {table.FullName} is a heap table with {table.RowCount:N0} rows",
                        "Heap tables can cause performance issues. Consider adding a clustered index",
                        GenerateClusteredIndexScript(table),
                        affectedObject: table.FullName,
                        objectType: "Table"
                    );
                }

                // Check for wide tables
                if (table.ColumnCount > 30)
                {
                    AddFinding(
                        Severity.Warning,
                        $"Table {table.FullName} is a wide table with {table.ColumnCount} columns",
                        "Consider normalizing the table or splitting it into multiple tables",
                        affectedObject: table.FullName,
                        objectType: "Table"
                    );
                }

                // Check for tables without indexes
                if (!table.HasIndexes && table.RowCount > 5000)
                {
                    AddFinding(
                        Severity.Warning,
                        $"Table {table.FullName} has no indexes and contains {table.RowCount:N0} rows",
                        "Consider adding indexes to improve query performance",
                        affectedObject: table.FullName,
                        objectType: "Table"
                    );
                }

                // Check naming conventions
                CheckNamingConventions(table);

                // Check for large tables
                if (table.RowCount > 5_000_000 || table.TotalSizeMB > 1000)
                {
                    AddFinding(
                        Severity.Info,
                        $"Table {table.FullName} is large ({table.RowCount:N0} rows, {table.TotalSizeMB:N0} MB)",
                        "Consider partitioning or archiving strategies for large tables",
                        affectedObject: table.FullName,
                        objectType: "Table"
                    );
                }
            }

            await Task.CompletedTask;
        }

        private void CheckNamingConventions(TableInfo table)
        {
            var tableName = table.TableName;

            // Check for common bad prefixes
            if (tableName.StartsWith("tbl_", StringComparison.OrdinalIgnoreCase) ||
                tableName.StartsWith("table_", StringComparison.OrdinalIgnoreCase))
            {
                AddFinding(
                    Severity.Info,
                    $"Table {table.FullName} uses redundant prefix in name",
                    "Avoid using prefixes like 'tbl_' or 'table_' in table names",
                    affectedObject: table.FullName,
                    objectType: "Table"
                );
            }

            // Check for special characters
            if (tableName.Contains("-") || tableName.Contains(" ") || tableName.Contains("@"))
            {
                AddFinding(
                    Severity.Info,
                    $"Table {table.FullName} contains special characters in name",
                    "Use only letters, numbers, and underscores in table names",
                    affectedObject: table.FullName,
                    objectType: "Table"
                );
            }

            // Check for reserved words (simplified check)
            var reservedWords = new[] { "USER", "ORDER", "GROUP", "TABLE", "INDEX", "VIEW", "PROCEDURE" };
            if (reservedWords.Contains(tableName.ToUpper()))
            {
                AddFinding(
                    Severity.Warning,
                    $"Table {table.FullName} uses reserved word as name",
                    "Avoid using SQL reserved words as table names",
                    affectedObject: table.FullName,
                    objectType: "Table"
                );
            }
        }

        private string GetTableQuery()
        {
            return _connection.DatabaseType switch
            {
                DatabaseType.SqlServer => GetSqlServerTableQuery(),
                DatabaseType.PostgreSql => GetPostgreSqlTableQuery(),
                DatabaseType.MySql => GetMySqlTableQuery(),
                _ => throw new NotSupportedException($"Database type {_connection.DatabaseType} is not supported")
            };
        }

        private string GetSqlServerTableQuery()
        {
            return @"
                SELECT 
                    s.name AS [Schema],
                    t.name AS TableName,
                    p.rows AS [RowCount],
                    CAST(SUM(a.total_pages) * 8 / 1024.0 AS DECIMAL(10, 2)) AS SizeMB,
                    CAST(SUM(CASE WHEN a.type = 2 THEN a.total_pages ELSE 0 END) * 8 / 1024.0 AS DECIMAL(10, 2)) AS IndexSizeMB,
                    CAST(CASE WHEN EXISTS (
                        SELECT 1 FROM sys.indexes i 
                        WHERE i.object_id = t.object_id AND i.is_primary_key = 1
                    ) THEN 1 ELSE 0 END AS BIT) AS HasPrimaryKey,
                    CAST(CASE WHEN EXISTS (
                        SELECT 1 FROM sys.indexes i 
                        WHERE i.object_id = t.object_id AND i.index_id > 0
                    ) THEN 1 ELSE 0 END AS BIT) AS HasIndexes,
                    CAST(CASE WHEN EXISTS (
                        SELECT 1 FROM sys.indexes i 
                        WHERE i.object_id = t.object_id AND i.type = 1
                    ) THEN 1 ELSE 0 END AS BIT) AS HasClusteredIndex,
                    (SELECT COUNT(*) FROM sys.columns c WHERE c.object_id = t.object_id) AS ColumnCount,
                    (SELECT COUNT(*) FROM sys.indexes i WHERE i.object_id = t.object_id AND i.index_id > 0) AS IndexCount,
                    CAST(CASE WHEN NOT EXISTS (
                        SELECT 1 FROM sys.indexes i 
                        WHERE i.object_id = t.object_id AND i.type = 1
                    ) THEN 1 ELSE 0 END AS BIT) AS IsHeap,
                    CAST(t.is_ms_shipped AS BIT) AS IsSystemTable
                FROM sys.tables t
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                INNER JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0, 1)
                LEFT JOIN sys.allocation_units a ON p.partition_id = a.container_id
                WHERE t.type = 'U' AND t.is_ms_shipped = 0
                GROUP BY s.name, t.name, t.object_id, p.rows, t.is_ms_shipped
                ORDER BY s.name, t.name";
        }

        private string GetPostgreSqlTableQuery()
        {
            return @"
                SELECT 
                    n.nspname AS ""Schema"",
                    c.relname AS ""TableName"",
                    c.reltuples::BIGINT AS ""RowCount"",
                    pg_size_pretty(pg_total_relation_size(c.oid))::TEXT AS ""TotalSize"",
                    ROUND(pg_total_relation_size(c.oid) / 1024.0 / 1024.0, 2) AS ""SizeMB"",
                    ROUND((pg_total_relation_size(c.oid) - pg_relation_size(c.oid)) / 1024.0 / 1024.0, 2) AS ""IndexSizeMB"",
                    EXISTS (
                        SELECT 1 FROM pg_constraint con 
                        WHERE con.conrelid = c.oid AND con.contype = 'p'
                    ) AS ""HasPrimaryKey"",
                    EXISTS (
                        SELECT 1 FROM pg_index i 
                        WHERE i.indrelid = c.oid
                    ) AS ""HasIndexes"",
                    false AS ""HasClusteredIndex"",
                    (SELECT COUNT(*) FROM pg_attribute a WHERE a.attrelid = c.oid AND a.attnum > 0 AND NOT a.attisdropped) AS ""ColumnCount"",
                    (SELECT COUNT(*) FROM pg_index i WHERE i.indrelid = c.oid) AS ""IndexCount"",
                    false AS ""IsHeap"",
                    n.nspname IN ('pg_catalog', 'information_schema') AS ""IsSystemTable""
                FROM pg_class c
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE c.relkind = 'r'
                    AND (@SchemaFilter IS NULL OR n.nspname = @SchemaFilter)
                ORDER BY n.nspname, c.relname";
        }

        private string GetMySqlTableQuery()
        {
            return @"
                SELECT 
                    t.TABLE_SCHEMA AS `Schema`,
                    t.TABLE_NAME AS TableName,
                    t.TABLE_ROWS AS RowCount,
                    ROUND((t.DATA_LENGTH + t.INDEX_LENGTH) / 1024 / 1024, 2) AS SizeMB,
                    ROUND(t.INDEX_LENGTH / 1024 / 1024, 2) AS IndexSizeMB,
                    EXISTS (
                        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                        WHERE tc.TABLE_SCHEMA = t.TABLE_SCHEMA 
                            AND tc.TABLE_NAME = t.TABLE_NAME 
                            AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                    ) AS HasPrimaryKey,
                    EXISTS (
                        SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS s
                        WHERE s.TABLE_SCHEMA = t.TABLE_SCHEMA 
                            AND s.TABLE_NAME = t.TABLE_NAME
                    ) AS HasIndexes,
                    false AS HasClusteredIndex,
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS c 
                     WHERE c.TABLE_SCHEMA = t.TABLE_SCHEMA AND c.TABLE_NAME = t.TABLE_NAME) AS ColumnCount,
                    (SELECT COUNT(DISTINCT INDEX_NAME) FROM INFORMATION_SCHEMA.STATISTICS s
                     WHERE s.TABLE_SCHEMA = t.TABLE_SCHEMA AND s.TABLE_NAME = t.TABLE_NAME) AS IndexCount,
                    false AS IsHeap,
                    t.TABLE_SCHEMA IN ('mysql', 'information_schema', 'performance_schema', 'sys') AS IsSystemTable
                FROM INFORMATION_SCHEMA.TABLES t
                WHERE t.TABLE_TYPE = 'BASE TABLE'
                    AND (@SchemaFilter IS NULL OR t.TABLE_SCHEMA = @SchemaFilter)
                ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME";
        }

        private TableInfo MapRowToTableInfo(DataRow row)
        {
            return new TableInfo
            {
                Schema = row["Schema"]?.ToString() ?? "dbo",
                TableName = row["TableName"]?.ToString() ?? "",
                RowCount = Convert.ToInt64(row["RowCount"] ?? 0),
                SizeMB = Convert.ToDecimal(row["SizeMB"] ?? 0),
                IndexSizeMB = Convert.ToDecimal(row["IndexSizeMB"] ?? 0),
                HasPrimaryKey = Convert.ToBoolean(row["HasPrimaryKey"] ?? false),
                HasIndexes = Convert.ToBoolean(row["HasIndexes"] ?? false),
                HasClusteredIndex = Convert.ToBoolean(row["HasClusteredIndex"] ?? false),
                ColumnCount = Convert.ToInt32(row["ColumnCount"] ?? 0),
                IndexCount = Convert.ToInt32(row["IndexCount"] ?? 0),
                IsHeap = Convert.ToBoolean(row["IsHeap"] ?? false),
                IsSystemTable = Convert.ToBoolean(row["IsSystemTable"] ?? false)
            };
        }

        private string GeneratePrimaryKeyScript(TableInfo table)
        {
            return $@"-- Add primary key to {table.FullName}
-- Note: Replace 'Id' with the appropriate column(s) for your primary key
ALTER TABLE {table.FullName}
ADD CONSTRAINT PK_{table.TableName} PRIMARY KEY (Id);";
        }

        private string GenerateClusteredIndexScript(TableInfo table)
        {
            return $@"-- Create clustered index on {table.FullName}
-- Note: Replace 'Id' with the appropriate column(s) for your clustered index
CREATE CLUSTERED INDEX IX_{table.TableName}_Clustered
ON {table.FullName} (Id);";
        }
    }
}