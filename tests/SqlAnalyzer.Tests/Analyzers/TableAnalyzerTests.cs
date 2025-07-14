using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SqlAnalyzer.Core.Analyzers;
using SqlAnalyzer.Core.Connections;
using SqlAnalyzer.Core.Models;
using Xunit;

namespace SqlAnalyzer.Tests.Analyzers
{
    public class TableAnalyzerTests
    {
        private readonly Mock<ISqlAnalyzerConnection> _mockConnection;
        private readonly Mock<ILogger<TableAnalyzer>> _mockLogger;
        private readonly TableAnalyzer _analyzer;

        public TableAnalyzerTests()
        {
            _mockConnection = new Mock<ISqlAnalyzerConnection>();
            _mockLogger = new Mock<ILogger<TableAnalyzer>>();
            
            _mockConnection.Setup(c => c.DatabaseName).Returns("TestDB");
            _mockConnection.Setup(c => c.ServerName).Returns("TestServer");
            _mockConnection.Setup(c => c.DatabaseType).Returns(DatabaseType.SqlServer);

            _analyzer = new TableAnalyzer(_mockConnection.Object, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_ShouldInitializeProperties()
        {
            // Assert
            _analyzer.Name.Should().Be("Table Analyzer");
            _analyzer.Description.Should().Contain("table structure");
            _analyzer.Category.Should().Be("Schema");
        }

        [Fact]
        public async Task AnalyzeAsync_WithNoTables_ShouldReturnEmptyFindings()
        {
            // Arrange
            SetupEmptyTableQuery();

            // Act
            var result = await _analyzer.AnalyzeAsync();

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Findings.Should().BeEmpty();
            result.Summary.TotalObjectsAnalyzed.Should().Be(0);
        }

        [Fact]
        public async Task AnalyzeAsync_WithTableWithoutPrimaryKey_ShouldCreateCriticalFinding()
        {
            // Arrange
            var tables = new List<TableInfo>
            {
                new TableInfo
                {
                    Schema = "dbo",
                    TableName = "Users",
                    HasPrimaryKey = false,
                    RowCount = 1000
                }
            };
            SetupTableQuery(tables);

            // Act
            var result = await _analyzer.AnalyzeAsync();

            // Assert
            result.Findings.Should().HaveCount(1);
            var finding = result.Findings[0];
            finding.Severity.Should().Be(Severity.Critical);
            finding.Message.Should().Contain("no primary key");
            finding.AffectedObject.Should().Be("dbo.Users");
        }

        [Fact]
        public async Task AnalyzeAsync_WithHeapTable_ShouldCreateErrorFinding()
        {
            // Arrange
            var tables = new List<TableInfo>
            {
                new TableInfo
                {
                    Schema = "dbo",
                    TableName = "Products",
                    HasPrimaryKey = true,
                    IsHeap = true,
                    HasClusteredIndex = false,
                    RowCount = 5000
                }
            };
            SetupTableQuery(tables);

            // Act
            var result = await _analyzer.AnalyzeAsync();

            // Assert
            result.Findings.Should().Contain(f => 
                f.Severity == Severity.Error && 
                f.Message.Contains("heap table"));
        }

        [Fact]
        public async Task AnalyzeAsync_WithWideTable_ShouldCreateWarningFinding()
        {
            // Arrange
            var tables = new List<TableInfo>
            {
                new TableInfo
                {
                    Schema = "dbo",
                    TableName = "WideTable",
                    HasPrimaryKey = true,
                    ColumnCount = 50,
                    RowCount = 1000
                }
            };
            SetupTableQuery(tables);

            // Act
            var result = await _analyzer.AnalyzeAsync();

            // Assert
            result.Findings.Should().Contain(f => 
                f.Severity == Severity.Warning && 
                f.Message.Contains("wide table"));
        }

        [Fact]
        public async Task AnalyzeAsync_WithTableWithoutIndexes_ShouldCreateWarningFinding()
        {
            // Arrange
            var tables = new List<TableInfo>
            {
                new TableInfo
                {
                    Schema = "dbo",
                    TableName = "Orders",
                    HasPrimaryKey = true,
                    HasIndexes = false,
                    IndexCount = 0,
                    RowCount = 10000
                }
            };
            SetupTableQuery(tables);

            // Act
            var result = await _analyzer.AnalyzeAsync();

            // Assert
            result.Findings.Should().Contain(f => 
                f.Severity == Severity.Warning && 
                f.Message.Contains("no indexes"));
        }

        [Fact]
        public async Task AnalyzeAsync_WithNamingConventionViolation_ShouldCreateInfoFinding()
        {
            // Arrange
            var tables = new List<TableInfo>
            {
                new TableInfo
                {
                    Schema = "dbo",
                    TableName = "tbl_users", // Bad prefix
                    HasPrimaryKey = true,
                    RowCount = 100
                },
                new TableInfo
                {
                    Schema = "dbo",
                    TableName = "USER-DATA", // Contains hyphen
                    HasPrimaryKey = true,
                    RowCount = 100
                }
            };
            SetupTableQuery(tables);

            // Act
            var result = await _analyzer.AnalyzeAsync();

            // Assert
            result.Findings.Should().Contain(f => 
                f.Severity == Severity.Info && 
                f.Message.Contains("naming convention"));
        }

        [Fact]
        public async Task AnalyzeAsync_WithLargeTable_ShouldCreateInfoFinding()
        {
            // Arrange
            var tables = new List<TableInfo>
            {
                new TableInfo
                {
                    Schema = "dbo",
                    TableName = "AuditLog",
                    HasPrimaryKey = true,
                    RowCount = 10_000_000,
                    SizeMB = 5000
                }
            };
            SetupTableQuery(tables);

            // Act
            var result = await _analyzer.AnalyzeAsync();

            // Assert
            result.Findings.Should().Contain(f => 
                f.Severity == Severity.Info && 
                f.Message.Contains("large table"));
        }

        [Fact]
        public async Task AnalyzeAsync_ShouldProvideRemediationScripts()
        {
            // Arrange
            var tables = new List<TableInfo>
            {
                new TableInfo
                {
                    Schema = "dbo",
                    TableName = "NoKeyTable",
                    HasPrimaryKey = false,
                    RowCount = 1000
                }
            };
            SetupTableQuery(tables);

            // Act
            var result = await _analyzer.AnalyzeAsync();

            // Assert
            var finding = result.Findings.FirstOrDefault(f => f.Message.Contains("no primary key"));
            finding.Should().NotBeNull();
            finding.RemediationScript.Should().Contain("ALTER TABLE");
            finding.RemediationScript.Should().Contain("ADD CONSTRAINT");
        }

        private void SetupEmptyTableQuery()
        {
            var emptyDataTable = new DataTable();
            _mockConnection.Setup(c => c.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(emptyDataTable);
        }

        private void SetupTableQuery(List<TableInfo> tables)
        {
            var dataTable = CreateTableDataTable(tables);
            _mockConnection.Setup(c => c.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync(dataTable);
        }

        private DataTable CreateTableDataTable(List<TableInfo> tables)
        {
            var dt = new DataTable();
            dt.Columns.Add("Schema", typeof(string));
            dt.Columns.Add("TableName", typeof(string));
            dt.Columns.Add("RowCount", typeof(long));
            dt.Columns.Add("SizeMB", typeof(decimal));
            dt.Columns.Add("IndexSizeMB", typeof(decimal));
            dt.Columns.Add("HasPrimaryKey", typeof(bool));
            dt.Columns.Add("HasIndexes", typeof(bool));
            dt.Columns.Add("HasClusteredIndex", typeof(bool));
            dt.Columns.Add("ColumnCount", typeof(int));
            dt.Columns.Add("IndexCount", typeof(int));
            dt.Columns.Add("IsHeap", typeof(bool));
            dt.Columns.Add("IsSystemTable", typeof(bool));

            foreach (var table in tables)
            {
                dt.Rows.Add(
                    table.Schema,
                    table.TableName,
                    table.RowCount,
                    table.SizeMB,
                    table.IndexSizeMB,
                    table.HasPrimaryKey,
                    table.HasIndexes,
                    table.HasClusteredIndex,
                    table.ColumnCount,
                    table.IndexCount,
                    table.IsHeap,
                    table.IsSystemTable
                );
            }

            return dt;
        }
    }
}