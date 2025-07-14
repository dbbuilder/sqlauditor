using System;
using System.Collections.Generic;

namespace SqlAnalyzer.Core.Models
{
    /// <summary>
    /// Represents information about a database table
    /// </summary>
    public class TableInfo
    {
        /// <summary>
        /// Schema name
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// Table name
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Full qualified name (schema.table)
        /// </summary>
        public string FullName => $"{Schema}.{TableName}";

        /// <summary>
        /// Number of rows in the table
        /// </summary>
        public long RowCount { get; set; }

        /// <summary>
        /// Size of the table in MB
        /// </summary>
        public decimal SizeMB { get; set; }

        /// <summary>
        /// Size of indexes in MB
        /// </summary>
        public decimal IndexSizeMB { get; set; }

        /// <summary>
        /// Total size (data + indexes) in MB
        /// </summary>
        public decimal TotalSizeMB => SizeMB + IndexSizeMB;

        /// <summary>
        /// Whether the table has a primary key
        /// </summary>
        public bool HasPrimaryKey { get; set; }

        /// <summary>
        /// Whether the table has any indexes
        /// </summary>
        public bool HasIndexes { get; set; }

        /// <summary>
        /// Whether the table has a clustered index
        /// </summary>
        public bool HasClusteredIndex { get; set; }

        /// <summary>
        /// Number of columns in the table
        /// </summary>
        public int ColumnCount { get; set; }

        /// <summary>
        /// Number of indexes on the table
        /// </summary>
        public int IndexCount { get; set; }

        /// <summary>
        /// Date when the table was created
        /// </summary>
        public DateTime? CreatedDate { get; set; }

        /// <summary>
        /// Date when the table was last modified
        /// </summary>
        public DateTime? ModifiedDate { get; set; }

        /// <summary>
        /// Whether the table is a heap (SQL Server specific)
        /// </summary>
        public bool IsHeap { get; set; }

        /// <summary>
        /// Whether the table is a system table
        /// </summary>
        public bool IsSystemTable { get; set; }

        /// <summary>
        /// Table type (BASE TABLE, VIEW, etc.)
        /// </summary>
        public string TableType { get; set; }

        /// <summary>
        /// List of column names
        /// </summary>
        public List<string> Columns { get; set; } = new List<string>();

        /// <summary>
        /// Additional properties specific to the database type
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}