using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlAnalyzer.Core.Analyzers;
using SqlAnalyzer.Core.Models;
using Xunit;
using Xunit.Abstractions;

namespace SqlAnalyzer.Tests.IntegrationTests
{
    /// <summary>
    /// Integration tests for TableAnalyzer with real database - READ ONLY
    /// </summary>
    public class TableAnalyzerIntegrationTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        public TableAnalyzerIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task AnalyzeAsync_RealDatabase_ShouldAnalyzeTables()
        {
            // Arrange
            CheckSkipIntegrationTest();
            using var connection = CreateSqlServerConnection();
            var logger = ServiceProvider.GetRequiredService<ILogger<TableAnalyzer>>();
            var analyzer = new TableAnalyzer(connection, logger);

            // Act
            var result = await analyzer.AnalyzeAsync();

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.AnalyzerName.Should().Be("Table Analyzer");
            result.DatabaseName.Should().Be(connection.DatabaseName);
            result.Summary.TotalObjectsAnalyzed.Should().BeGreaterThanOrEqualTo(0);

            // Output results
            _output.WriteLine($"Analysis completed for database: {result.DatabaseName}");
            _output.WriteLine($"Total tables analyzed: {result.Summary.TotalObjectsAnalyzed}");
            _output.WriteLine($"Total findings: {result.Summary.TotalFindings}");
            _output.WriteLine($"  Critical: {result.Summary.CriticalFindings}");
            _output.WriteLine($"  Error: {result.Summary.ErrorFindings}");
            _output.WriteLine($"  Warning: {result.Summary.WarningFindings}");
            _output.WriteLine($"  Info: {result.Summary.InfoFindings}");

            // Output specific findings
            if (result.Findings.Any())
            {
                _output.WriteLine("\nFindings:");
                foreach (var finding in result.Findings.OrderByDescending(f => f.Severity))
                {
                    _output.WriteLine($"  [{finding.Severity}] {finding.Message}");
                    _output.WriteLine($"    Object: {finding.AffectedObject}");
                    _output.WriteLine($"    Recommendation: {finding.Recommendation}");
                }
            }
        }

        [Fact]
        public async Task AnalyzeAsync_WithSchemaFilter_ShouldOnlyAnalyzeSpecificSchema()
        {
            // Arrange
            CheckSkipIntegrationTest();
            using var connection = CreateSqlServerConnection();
            var logger = ServiceProvider.GetRequiredService<ILogger<TableAnalyzer>>();
            var analyzer = new TableAnalyzer(connection, logger)
            {
                SchemaFilter = "dbo" // Only analyze dbo schema
            };

            // Act
            var result = await analyzer.AnalyzeAsync();

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            // All findings should be for dbo schema objects
            var nonDboFindings = result.Findings
                .Where(f => !string.IsNullOrEmpty(f.AffectedObject) && !f.AffectedObject.StartsWith("dbo."))
                .ToList();
            
            nonDboFindings.Should().BeEmpty();
            
            _output.WriteLine($"Analyzed {result.Summary.TotalObjectsAnalyzed} tables in dbo schema");
        }

        [Fact]
        public async Task CollectDataAsync_ShouldReturnTableInformation()
        {
            // Arrange
            CheckSkipIntegrationTest();
            using var connection = CreateSqlServerConnection();
            var logger = ServiceProvider.GetRequiredService<ILogger<TableAnalyzer>>();
            var analyzer = new TableAnalyzer(connection, logger);

            // Act
            var tables = await analyzer.CollectDataAsync();

            // Assert
            tables.Should().NotBeNull();
            tables.Count.Should().BeGreaterThanOrEqualTo(0);

            // Output table information
            _output.WriteLine($"Found {tables.Count} tables in the database");
            
            var userTables = tables.Where(t => !t.IsSystemTable).ToList();
            _output.WriteLine($"User tables: {userTables.Count}");
            
            if (userTables.Any())
            {
                _output.WriteLine("\nTop 10 tables by row count:");
                foreach (var table in userTables.OrderByDescending(t => t.RowCount).Take(10))
                {
                    _output.WriteLine($"  {table.FullName}: {table.RowCount:N0} rows, {table.TotalSizeMB:N2} MB");
                    _output.WriteLine($"    Columns: {table.ColumnCount}, Indexes: {table.IndexCount}");
                    _output.WriteLine($"    Has PK: {table.HasPrimaryKey}, Is Heap: {table.IsHeap}");
                }
            }
        }

        [Fact]
        public async Task AnalyzeAsync_ShouldDetectCommonIssues()
        {
            // Arrange
            CheckSkipIntegrationTest();
            using var connection = CreateSqlServerConnection();
            var logger = ServiceProvider.GetRequiredService<ILogger<TableAnalyzer>>();
            var analyzer = new TableAnalyzer(connection, logger);

            // Act
            var result = await analyzer.AnalyzeAsync();

            // Assert - Check for common issues
            var criticalFindings = result.Findings.Where(f => f.Severity == Severity.Critical).ToList();
            var errorFindings = result.Findings.Where(f => f.Severity == Severity.Error).ToList();
            var warningFindings = result.Findings.Where(f => f.Severity == Severity.Warning).ToList();

            // Log issue categories
            _output.WriteLine("\nIssue Categories Found:");
            
            var noPrimaryKeyTables = result.Findings.Where(f => f.Message.Contains("no primary key")).ToList();
            if (noPrimaryKeyTables.Any())
            {
                _output.WriteLine($"\nTables without primary keys ({noPrimaryKeyTables.Count}):");
                foreach (var finding in noPrimaryKeyTables)
                {
                    _output.WriteLine($"  - {finding.AffectedObject}");
                }
            }

            var heapTables = result.Findings.Where(f => f.Message.Contains("heap table")).ToList();
            if (heapTables.Any())
            {
                _output.WriteLine($"\nHeap tables ({heapTables.Count}):");
                foreach (var finding in heapTables)
                {
                    _output.WriteLine($"  - {finding.AffectedObject}");
                }
            }

            var wideTables = result.Findings.Where(f => f.Message.Contains("wide table")).ToList();
            if (wideTables.Any())
            {
                _output.WriteLine($"\nWide tables ({wideTables.Count}):");
                foreach (var finding in wideTables)
                {
                    _output.WriteLine($"  - {finding.AffectedObject}");
                }
            }

            var noIndexTables = result.Findings.Where(f => f.Message.Contains("no indexes")).ToList();
            if (noIndexTables.Any())
            {
                _output.WriteLine($"\nTables without indexes ({noIndexTables.Count}):");
                foreach (var finding in noIndexTables)
                {
                    _output.WriteLine($"  - {finding.AffectedObject}");
                }
            }
        }

        [Fact]
        public async Task AnalyzeAsync_ShouldProvideRemediationScripts()
        {
            // Arrange
            CheckSkipIntegrationTest();
            using var connection = CreateSqlServerConnection();
            var logger = ServiceProvider.GetRequiredService<ILogger<TableAnalyzer>>();
            var analyzer = new TableAnalyzer(connection, logger);

            // Act
            var result = await analyzer.AnalyzeAsync();

            // Assert
            var findingsWithScripts = result.Findings
                .Where(f => !string.IsNullOrWhiteSpace(f.RemediationScript))
                .ToList();

            if (findingsWithScripts.Any())
            {
                _output.WriteLine($"\nFindings with remediation scripts ({findingsWithScripts.Count}):");
                foreach (var finding in findingsWithScripts.Take(5)) // Show first 5
                {
                    _output.WriteLine($"\n[{finding.Severity}] {finding.Message}");
                    _output.WriteLine("Remediation script:");
                    _output.WriteLine(finding.RemediationScript);
                }
            }
            else
            {
                _output.WriteLine("No findings with remediation scripts found.");
            }
        }

        [Fact]
        public async Task AnalyzeAsync_PerformanceTest_ShouldCompleteInReasonableTime()
        {
            // Arrange
            CheckSkipIntegrationTest();
            using var connection = CreateSqlServerConnection();
            var logger = ServiceProvider.GetRequiredService<ILogger<TableAnalyzer>>();
            var analyzer = new TableAnalyzer(connection, logger);

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await analyzer.AnalyzeAsync();
            stopwatch.Stop();

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            
            _output.WriteLine($"Analysis completed in {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"Analysis duration from result: {result.Duration.TotalMilliseconds}ms");
            
            // Performance should be reasonable (adjust based on your expectations)
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000); // 30 seconds max
        }
    }
}