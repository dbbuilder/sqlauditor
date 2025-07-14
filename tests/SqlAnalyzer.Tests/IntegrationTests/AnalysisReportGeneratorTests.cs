using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SqlAnalyzer.Core.Analyzers;
using SqlAnalyzer.Core.Models;
using Xunit;
using Xunit.Abstractions;

namespace SqlAnalyzer.Tests.IntegrationTests
{
    /// <summary>
    /// Integration tests that generate comprehensive analysis reports
    /// </summary>
    public class AnalysisReportGeneratorTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        public AnalysisReportGeneratorTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task GenerateComprehensiveAnalysisReport_ShouldCreateDetailedReport()
        {
            // Arrange
            CheckSkipIntegrationTest();
            using var connection = CreateSqlServerConnection();
            var logger = ServiceProvider.GetRequiredService<ILogger<TableAnalyzer>>();
            var tableAnalyzer = new TableAnalyzer(connection, logger);

            // Act - Run analysis
            var analysisResult = await tableAnalyzer.AnalyzeAsync();

            // Generate reports
            var jsonReport = GenerateJsonReport(analysisResult);
            var htmlReport = GenerateHtmlReport(analysisResult);
            var markdownReport = GenerateMarkdownReport(analysisResult);
            var csvReport = GenerateCsvReport(analysisResult);

            // Save reports
            var reportsDir = Path.Combine("TestResults", "AnalysisReports", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(reportsDir);

            await File.WriteAllTextAsync(Path.Combine(reportsDir, "analysis_result.json"), jsonReport);
            await File.WriteAllTextAsync(Path.Combine(reportsDir, "analysis_report.html"), htmlReport);
            await File.WriteAllTextAsync(Path.Combine(reportsDir, "analysis_report.md"), markdownReport);
            await File.WriteAllTextAsync(Path.Combine(reportsDir, "findings.csv"), csvReport);

            // Assert
            analysisResult.Should().NotBeNull();
            File.Exists(Path.Combine(reportsDir, "analysis_result.json")).Should().BeTrue();
            
            _output.WriteLine($"Analysis reports generated in: {reportsDir}");
            _output.WriteLine($"- JSON Report: analysis_result.json");
            _output.WriteLine($"- HTML Report: analysis_report.html");
            _output.WriteLine($"- Markdown Report: analysis_report.md");
            _output.WriteLine($"- CSV Report: findings.csv");
        }

        private string GenerateJsonReport(AnalysisResult result)
        {
            return JsonConvert.SerializeObject(result, Formatting.Indented);
        }

        private string GenerateHtmlReport(AnalysisResult result)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("    <title>SQL Analyzer Report</title>");
            html.AppendLine("    <style>");
            html.AppendLine("        body { font-family: Arial, sans-serif; margin: 20px; }");
            html.AppendLine("        h1, h2, h3 { color: #333; }");
            html.AppendLine("        .summary { background: #f0f0f0; padding: 15px; border-radius: 5px; margin: 20px 0; }");
            html.AppendLine("        .finding { margin: 10px 0; padding: 10px; border-left: 4px solid; }");
            html.AppendLine("        .critical { border-color: #d32f2f; background: #ffebee; }");
            html.AppendLine("        .error { border-color: #f57c00; background: #fff3e0; }");
            html.AppendLine("        .warning { border-color: #fbc02d; background: #fffde7; }");
            html.AppendLine("        .info { border-color: #1976d2; background: #e3f2fd; }");
            html.AppendLine("        .metadata { color: #666; font-size: 0.9em; }");
            html.AppendLine("        pre { background: #f5f5f5; padding: 10px; overflow-x: auto; }");
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            
            // Header
            html.AppendLine($"<h1>SQL Analyzer Report - {result.DatabaseName}</h1>");
            html.AppendLine($"<div class='metadata'>");
            html.AppendLine($"    <p>Server: {result.ServerName}</p>");
            html.AppendLine($"    <p>Database Type: {result.DatabaseType}</p>");
            html.AppendLine($"    <p>Analyzer: {result.AnalyzerName}</p>");
            html.AppendLine($"    <p>Analysis Date: {result.AnalysisStartTime:yyyy-MM-dd HH:mm:ss}</p>");
            html.AppendLine($"    <p>Duration: {result.Duration.TotalSeconds:F2} seconds</p>");
            html.AppendLine($"</div>");

            // Summary
            html.AppendLine("<div class='summary'>");
            html.AppendLine($"    <h2>Summary</h2>");
            html.AppendLine($"    <p>Total Objects Analyzed: {result.Summary.TotalObjectsAnalyzed}</p>");
            html.AppendLine($"    <p>Total Findings: {result.Summary.TotalFindings}</p>");
            html.AppendLine($"    <ul>");
            html.AppendLine($"        <li>Critical: {result.Summary.CriticalFindings}</li>");
            html.AppendLine($"        <li>Error: {result.Summary.ErrorFindings}</li>");
            html.AppendLine($"        <li>Warning: {result.Summary.WarningFindings}</li>");
            html.AppendLine($"        <li>Info: {result.Summary.InfoFindings}</li>");
            html.AppendLine($"    </ul>");
            html.AppendLine("</div>");

            // Findings
            html.AppendLine("<h2>Findings</h2>");
            
            foreach (var severity in Enum.GetValues<Severity>().OrderByDescending(s => s))
            {
                var findings = result.Findings.Where(f => f.Severity == severity).ToList();
                if (findings.Any())
                {
                    html.AppendLine($"<h3>{severity} ({findings.Count})</h3>");
                    foreach (var finding in findings)
                    {
                        var cssClass = severity.ToString().ToLower();
                        html.AppendLine($"<div class='finding {cssClass}'>");
                        html.AppendLine($"    <h4>{finding.Message}</h4>");
                        html.AppendLine($"    <p><strong>Object:</strong> {finding.AffectedObject}</p>");
                        html.AppendLine($"    <p><strong>Category:</strong> {finding.Category}</p>");
                        html.AppendLine($"    <p><strong>Recommendation:</strong> {finding.Recommendation}</p>");
                        
                        if (!string.IsNullOrWhiteSpace(finding.RemediationScript))
                        {
                            html.AppendLine($"    <details>");
                            html.AppendLine($"        <summary>Remediation Script</summary>");
                            html.AppendLine($"        <pre>{System.Web.HttpUtility.HtmlEncode(finding.RemediationScript)}</pre>");
                            html.AppendLine($"    </details>");
                        }
                        
                        html.AppendLine($"</div>");
                    }
                }
            }

            html.AppendLine("</body>");
            html.AppendLine("</html>");
            
            return html.ToString();
        }

        private string GenerateMarkdownReport(AnalysisResult result)
        {
            var md = new StringBuilder();
            
            // Header
            md.AppendLine($"# SQL Analyzer Report - {result.DatabaseName}");
            md.AppendLine();
            md.AppendLine($"**Server:** {result.ServerName}  ");
            md.AppendLine($"**Database Type:** {result.DatabaseType}  ");
            md.AppendLine($"**Analyzer:** {result.AnalyzerName}  ");
            md.AppendLine($"**Analysis Date:** {result.AnalysisStartTime:yyyy-MM-dd HH:mm:ss}  ");
            md.AppendLine($"**Duration:** {result.Duration.TotalSeconds:F2} seconds  ");
            md.AppendLine();

            // Summary
            md.AppendLine("## Summary");
            md.AppendLine();
            md.AppendLine($"- **Total Objects Analyzed:** {result.Summary.TotalObjectsAnalyzed}");
            md.AppendLine($"- **Total Findings:** {result.Summary.TotalFindings}");
            md.AppendLine($"  - Critical: {result.Summary.CriticalFindings}");
            md.AppendLine($"  - Error: {result.Summary.ErrorFindings}");
            md.AppendLine($"  - Warning: {result.Summary.WarningFindings}");
            md.AppendLine($"  - Info: {result.Summary.InfoFindings}");
            md.AppendLine();

            // Findings by severity
            md.AppendLine("## Findings");
            md.AppendLine();

            foreach (var severity in Enum.GetValues<Severity>().OrderByDescending(s => s))
            {
                var findings = result.Findings.Where(f => f.Severity == severity).ToList();
                if (findings.Any())
                {
                    md.AppendLine($"### {severity} Issues ({findings.Count})");
                    md.AppendLine();
                    
                    foreach (var finding in findings)
                    {
                        md.AppendLine($"#### {finding.Message}");
                        md.AppendLine();
                        md.AppendLine($"- **Object:** `{finding.AffectedObject}`");
                        md.AppendLine($"- **Category:** {finding.Category}");
                        md.AppendLine($"- **Recommendation:** {finding.Recommendation}");
                        
                        if (!string.IsNullOrWhiteSpace(finding.RemediationScript))
                        {
                            md.AppendLine();
                            md.AppendLine("<details>");
                            md.AppendLine("<summary>Remediation Script</summary>");
                            md.AppendLine();
                            md.AppendLine("```sql");
                            md.AppendLine(finding.RemediationScript);
                            md.AppendLine("```");
                            md.AppendLine();
                            md.AppendLine("</details>");
                        }
                        
                        md.AppendLine();
                    }
                }
            }

            return md.ToString();
        }

        private string GenerateCsvReport(AnalysisResult result)
        {
            var csv = new StringBuilder();
            
            // Header
            csv.AppendLine("Severity,Category,Message,AffectedObject,ObjectType,Schema,Recommendation,DiscoveredAt");
            
            // Data
            foreach (var finding in result.Findings.OrderByDescending(f => f.Severity))
            {
                csv.AppendLine($"\"{finding.Severity}\",\"{finding.Category}\",\"{EscapeCsv(finding.Message)}\",\"{finding.AffectedObject}\",\"{finding.ObjectType}\",\"{finding.Schema}\",\"{EscapeCsv(finding.Recommendation)}\",\"{finding.DiscoveredAt:yyyy-MM-dd HH:mm:ss}\"");
            }
            
            return csv.ToString();
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            
            return value.Replace("\"", "\"\"");
        }

        [Fact]
        public async Task GenerateDatabaseHealthReport_ShouldProvideOverview()
        {
            // Arrange
            CheckSkipIntegrationTest();
            using var connection = CreateSqlServerConnection();

            // Act - Get database health metrics
            var healthReport = new StringBuilder();
            healthReport.AppendLine("# Database Health Report");
            healthReport.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            healthReport.AppendLine();

            // Database info
            var version = await connection.GetDatabaseVersionAsync();
            var sizeMB = await connection.GetDatabaseSizeAsync();
            
            healthReport.AppendLine("## Database Information");
            healthReport.AppendLine($"- **Server:** {connection.ServerName}");
            healthReport.AppendLine($"- **Database:** {connection.DatabaseName}");
            healthReport.AppendLine($"- **Version:** {version}");
            healthReport.AppendLine($"- **Size:** {sizeMB:N2} MB");
            healthReport.AppendLine();

            // Object counts
            var objectCountsQuery = @"
                SELECT 
                    'Tables' as ObjectType, COUNT(*) as Count 
                FROM sys.tables WHERE is_ms_shipped = 0
                UNION ALL
                SELECT 'Views', COUNT(*) FROM sys.views WHERE is_ms_shipped = 0
                UNION ALL
                SELECT 'Procedures', COUNT(*) FROM sys.procedures WHERE is_ms_shipped = 0
                UNION ALL
                SELECT 'Functions', COUNT(*) FROM sys.objects WHERE type IN ('FN', 'IF', 'TF') AND is_ms_shipped = 0
                UNION ALL
                SELECT 'Indexes', COUNT(*) FROM sys.indexes WHERE object_id IN (SELECT object_id FROM sys.tables WHERE is_ms_shipped = 0)";

            var objectCounts = await connection.ExecuteQueryAsync(objectCountsQuery);
            
            healthReport.AppendLine("## Object Summary");
            foreach (System.Data.DataRow row in objectCounts.Rows)
            {
                healthReport.AppendLine($"- **{row["ObjectType"]}:** {row["Count"]}");
            }
            healthReport.AppendLine();

            // Save report
            var reportPath = Path.Combine("TestResults", "database_health_report.md");
            await File.WriteAllTextAsync(reportPath, healthReport.ToString());
            
            // Assert
            File.Exists(reportPath).Should().BeTrue();
            _output.WriteLine($"Database health report saved to: {reportPath}");
            _output.WriteLine(healthReport.ToString());
        }
    }
}