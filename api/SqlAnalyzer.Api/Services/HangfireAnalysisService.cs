using Hangfire;
using Microsoft.AspNetCore.SignalR;
using SqlAnalyzer.Api.Hubs;
using SqlAnalyzer.Api.Models;
using SqlAnalyzer.Core.Analyzers;
using SqlAnalyzer.Core.Connections;
using System.Collections.Concurrent;
using System.Data;

namespace SqlAnalyzer.Api.Services;

public interface IHangfireAnalysisService
{
    string StartAnalysis(AnalysisRequest request);
    Task<AnalysisStatus?> GetAnalysisStatusAsync(string jobId);
    Task<AnalysisResult?> GetAnalysisResultAsync(string jobId);
    void CancelAnalysis(string jobId);
}

public class HangfireAnalysisService : IHangfireAnalysisService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<AnalysisHub>? _hubContext;
    private readonly ILogger<HangfireAnalysisService> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;

    // In-memory cache for status updates (Hangfire stores the actual job data)
    private static readonly ConcurrentDictionary<string, AnalysisStatus> _statusCache = new();
    private static readonly ConcurrentDictionary<string, AnalysisResult> _resultsCache = new();

    public HangfireAnalysisService(
        IServiceProvider serviceProvider,
        ILogger<HangfireAnalysisService> logger,
        IBackgroundJobClient backgroundJobClient)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;

        // Try to get HubContext if SignalR is enabled
        try
        {
            _hubContext = serviceProvider.GetService<IHubContext<AnalysisHub>>();
        }
        catch
        {
            _logger.LogInformation("SignalR is disabled, real-time updates will not be available");
        }
    }

    public string StartAnalysis(AnalysisRequest request)
    {
        var jobId = Guid.NewGuid().ToString();

        // Initialize status
        var status = new AnalysisStatus
        {
            JobId = jobId,
            Status = "Queued",
            StartedAt = DateTime.UtcNow,
            CurrentStep = "Initializing",
            ProgressPercentage = 0
        };
        _statusCache[jobId] = status;

        // Enqueue the job with Hangfire
        var hangfireJobId = _backgroundJobClient.Enqueue(() => RunAnalysis(jobId, request));

        _logger.LogInformation("Analysis job {JobId} enqueued with Hangfire ID {HangfireJobId}", jobId, hangfireJobId);

        return jobId;
    }

    [JobDisplayName("Analysis: {1}")]
    public async Task RunAnalysis(string jobId, AnalysisRequest request)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var connectionFactory = scope.ServiceProvider.GetRequiredService<IConnectionFactory>();
            var emailService = scope.ServiceProvider.GetService<IEmailService>();

            // Update status
            await UpdateJobStatus(jobId, "Running", 0, "Connecting to database");

            using var connection = connectionFactory.CreateConnection(
                request.ConnectionString,
                request.DatabaseType);

            var result = new AnalysisResult
            {
                JobId = jobId,
                AnalyzedAt = DateTime.UtcNow
            };

            // Step 1: Database Info (10%)
            await UpdateJobStatus(jobId, "Running", 10, "Gathering database information");
            result.Database = await GatherDatabaseInfo(connection);

            // Step 2: Table Analysis (20%)
            if (request.Options.IncludeIndexAnalysis)
            {
                await UpdateJobStatus(jobId, "Running", 20, "Analyzing tables and indexes");
                await AnalyzeTables(connection, result);
            }

            // Step 3: Performance Analysis (40%)
            if (request.Options.IncludeQueryPerformance)
            {
                await UpdateJobStatus(jobId, "Running", 40, "Analyzing query performance");
                result.Performance = await AnalyzePerformance(connection, request.Options);
            }

            // Step 4: Security Audit (60%)
            if (request.Options.IncludeSecurityAudit)
            {
                await UpdateJobStatus(jobId, "Running", 60, "Performing security audit");
                result.Security = await PerformSecurityAudit(connection);
            }

            // Step 5: Generate Recommendations (80%)
            await UpdateJobStatus(jobId, "Running", 80, "Generating recommendations");
            result.Recommendations = GenerateRecommendations(result);

            // Step 6: Complete (100%)
            _resultsCache[jobId] = result;
            await UpdateJobStatus(jobId, "Completed", 100, "Analysis completed successfully");

            // Send email notification if requested
            if (!string.IsNullOrEmpty(request.NotificationEmail) && emailService != null)
            {
                try
                {
                    await emailService.SendAnalysisReportAsync(request.NotificationEmail, jobId, result);
                    _logger.LogInformation("Analysis report sent to {Email} for job {JobId}", request.NotificationEmail, jobId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send analysis report email for job {JobId}", jobId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analysis failed for job {JobId}", jobId);

            if (_statusCache.TryGetValue(jobId, out var status))
            {
                status.ErrorMessage = ex.Message;
            }

            await UpdateJobStatus(jobId, "Failed", 0, $"Analysis failed: {ex.Message}");

            // Send failure notification if email was requested
            if (!string.IsNullOrEmpty(request.NotificationEmail))
            {
                using var scope = _serviceProvider.CreateScope();
                var emailService = scope.ServiceProvider.GetService<IEmailService>();
                if (emailService != null)
                {
                    try
                    {
                        await emailService.SendAnalysisFailureNotificationAsync(request.NotificationEmail, jobId, ex.Message);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send failure notification email for job {JobId}", jobId);
                    }
                }
            }

            throw; // Re-throw to let Hangfire handle retry logic
        }
    }

    private async Task UpdateJobStatus(string jobId, string status, double progress, string currentStep)
    {
        if (_statusCache.TryGetValue(jobId, out var jobStatus))
        {
            jobStatus.Status = status;
            jobStatus.ProgressPercentage = progress;
            jobStatus.CurrentStep = currentStep;

            if (status == "Completed" || status == "Failed" || status == "Cancelled")
            {
                jobStatus.CompletedAt = DateTime.UtcNow;
            }

            // Send real-time update via SignalR (if enabled)
            if (_hubContext != null)
            {
                await _hubContext.Clients.Group($"job-{jobId}").SendAsync("AnalysisProgress", jobStatus);
            }
        }
    }

    public async Task<AnalysisStatus?> GetAnalysisStatusAsync(string jobId)
    {
        return _statusCache.TryGetValue(jobId, out var status) ? status : null;
    }

    public async Task<AnalysisResult?> GetAnalysisResultAsync(string jobId)
    {
        return _resultsCache.TryGetValue(jobId, out var result) ? result : null;
    }

    public void CancelAnalysis(string jobId)
    {
        // Delete the Hangfire job
        BackgroundJob.Delete(jobId);

        if (_statusCache.TryGetValue(jobId, out var status))
        {
            status.Status = "Cancelled";
            status.CompletedAt = DateTime.UtcNow;
        }
    }

    // Copy all the analysis methods from the original AnalysisService
    private async Task<DatabaseInfo> GatherDatabaseInfo(ISqlAnalyzerConnection connection)
    {
        var info = new DatabaseInfo();

        var dbName = await connection.ExecuteScalarAsync("SELECT DB_NAME()");
        info.Name = dbName?.ToString() ?? "Unknown";

        // Get server info
        var serverInfo = await connection.ExecuteScalarAsync(@"
            SELECT 
                SERVERPROPERTY('ProductVersion') + ' - ' + 
                SERVERPROPERTY('Edition')
        ");
        info.ServerVersion = serverInfo?.ToString() ?? "Unknown";

        // Get counts
        info.TableCount = Convert.ToInt32(await connection.ExecuteScalarAsync(
            "SELECT COUNT(*) FROM sys.tables"));
        info.IndexCount = Convert.ToInt32(await connection.ExecuteScalarAsync(
            "SELECT COUNT(*) FROM sys.indexes WHERE object_id IN (SELECT object_id FROM sys.tables)"));
        info.ProcedureCount = Convert.ToInt32(await connection.ExecuteScalarAsync(
            "SELECT COUNT(*) FROM sys.procedures"));
        info.ViewCount = Convert.ToInt32(await connection.ExecuteScalarAsync(
            "SELECT COUNT(*) FROM sys.views"));

        // Get database size
        var sizeResult = await connection.ExecuteScalarAsync(@"
            SELECT SUM(CAST(size AS BIGINT)) * 8 / 1024 
            FROM sys.database_files 
            WHERE type = 0
        ");
        info.SizeMB = Convert.ToInt64(sizeResult ?? 0);

        // Get total row count
        var rowResult = await connection.ExecuteScalarAsync(@"
            SELECT SUM(p.rows)
            FROM sys.partitions p
            INNER JOIN sys.tables t ON p.object_id = t.object_id
            WHERE p.index_id IN (0, 1)
        ");
        info.TotalRows = Convert.ToInt64(rowResult ?? 0);

        return info;
    }

    private async Task AnalyzeTables(ISqlAnalyzerConnection connection, AnalysisResult result)
    {
        var analyzer = new TableAnalyzer(connection,
            _serviceProvider.GetRequiredService<ILogger<TableAnalyzer>>());

        var analysisResult = await analyzer.AnalyzeAsync();

        // Convert analyzer findings to API findings
        foreach (var finding in analysisResult.Findings)
        {
            result.Findings.Add(new Finding
            {
                Category = "Tables",
                Severity = finding.Severity.ToString(),
                Title = finding.Message,
                Description = finding.Message,
                Impact = finding.Impact ?? "Performance may be affected"
            });
        }

        result.Database.TotalRows = analysisResult.Summary.TotalRows;
    }

    private async Task<PerformanceMetrics> AnalyzePerformance(ISqlAnalyzerConnection connection, AnalysisOptions options)
    {
        var metrics = new PerformanceMetrics();

        if (options.IncludeIndexAnalysis)
        {
            // Missing indexes
            var missingIndexes = await connection.ExecuteQueryAsync(@"
                SELECT TOP 10
                    ROUND(s.avg_total_user_cost * s.avg_user_impact * (s.user_seeks + s.user_scans), 0) AS ImpactScore,
                    d.statement AS TableName,
                    d.equality_columns,
                    d.inequality_columns,
                    d.included_columns
                FROM sys.dm_db_missing_index_details d
                INNER JOIN sys.dm_db_missing_index_groups g ON d.index_handle = g.index_handle
                INNER JOIN sys.dm_db_missing_index_group_stats s ON g.index_group_handle = s.group_handle
                WHERE database_id = DB_ID()
                ORDER BY ImpactScore DESC
            ");

            foreach (DataRow row in missingIndexes.Rows)
            {
                metrics.MissingIndexes.Add(new MissingIndex
                {
                    TableName = row["TableName"]?.ToString() ?? "",
                    ImpactScore = Convert.ToDouble(row["ImpactScore"]),
                    EqualityColumns = ParseColumns(row["equality_columns"]?.ToString()),
                    InequalityColumns = ParseColumns(row["inequality_columns"]?.ToString()),
                    IncludedColumns = ParseColumns(row["included_columns"]?.ToString())
                });
            }
        }

        if (options.IncludeFragmentation)
        {
            // Fragmented indexes
            var fragmented = await connection.ExecuteQueryAsync(@"
                SELECT TOP 20
                    OBJECT_NAME(ps.object_id) AS TableName,
                    i.name AS IndexName,
                    ps.avg_fragmentation_in_percent,
                    ps.page_count
                FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ps
                INNER JOIN sys.indexes i ON ps.object_id = i.object_id AND ps.index_id = i.index_id
                WHERE ps.avg_fragmentation_in_percent > 30
                    AND ps.page_count > 100
                ORDER BY ps.avg_fragmentation_in_percent DESC
            ");

            foreach (DataRow row in fragmented.Rows)
            {
                metrics.FragmentedIndexes.Add(new FragmentedIndex
                {
                    TableName = row["TableName"]?.ToString() ?? "",
                    IndexName = row["IndexName"]?.ToString() ?? "",
                    FragmentationPercent = Convert.ToDouble(row["avg_fragmentation_in_percent"]),
                    PageCount = Convert.ToInt32(row["page_count"])
                });
            }
        }

        return metrics;
    }

    private async Task<SecurityAudit> PerformSecurityAudit(ISqlAnalyzerConnection connection)
    {
        var audit = new SecurityAudit();

        // Check permissions
        var permissions = await connection.ExecuteQueryAsync(@"
            SELECT 
                p.permission_name,
                p.state_desc,
                pr.name AS principal_name,
                pr.type_desc AS principal_type
            FROM sys.database_permissions p
            INNER JOIN sys.database_principals pr ON p.grantee_principal_id = pr.principal_id
            WHERE p.major_id = 0
                AND pr.name NOT IN ('public', 'dbo', 'guest', 'sys', 'INFORMATION_SCHEMA')
            ORDER BY pr.name, p.permission_name
        ");

        foreach (DataRow row in permissions.Rows)
        {
            var permission = new UserPermission
            {
                PrincipalName = row["principal_name"]?.ToString() ?? "",
                PrincipalType = row["principal_type"]?.ToString() ?? "",
                PermissionName = row["permission_name"]?.ToString() ?? "",
                State = row["state_desc"]?.ToString() ?? ""
            };

            audit.Permissions.Add(permission);

            // Check for elevated permissions
            if (permission.PermissionName.Contains("CONTROL") ||
                permission.PermissionName.Contains("ALTER"))
            {
                audit.HasElevatedPermissions = true;
            }
        }

        return audit;
    }

    private List<Recommendation> GenerateRecommendations(AnalysisResult result)
    {
        var recommendations = new List<Recommendation>();

        // Performance recommendations
        if (result.Performance?.MissingIndexes?.Any() == true)
        {
            recommendations.Add(new Recommendation
            {
                Category = "Performance",
                Title = "Create Missing Indexes",
                Description = $"Found {result.Performance.MissingIndexes.Count} missing indexes that could improve query performance",
                Priority = "High",
                EstimatedImpact = "20-50% query performance improvement",
                Actions = result.Performance.MissingIndexes.Select(i =>
                    $"CREATE INDEX [IX_{i.TableName.Replace("[", "").Replace("]", "")}_{string.Join("_", i.EqualityColumns).Replace("[", "").Replace("]", "")}] ON {i.TableName} ({string.Join(", ", i.EqualityColumns)})").ToList()
            });
        }

        if (result.Performance?.FragmentedIndexes?.Any() == true)
        {
            recommendations.Add(new Recommendation
            {
                Category = "Maintenance",
                Title = "Rebuild Fragmented Indexes",
                Description = $"Found {result.Performance.FragmentedIndexes.Count} highly fragmented indexes",
                Priority = "Medium",
                EstimatedImpact = "10-30% I/O performance improvement",
                Actions = result.Performance.FragmentedIndexes.Select(i =>
                    $"ALTER INDEX [{i.IndexName}] ON {i.TableName} REBUILD").ToList()
            });
        }

        // Security recommendations
        if (result.Security?.HasElevatedPermissions == true)
        {
            recommendations.Add(new Recommendation
            {
                Category = "Security",
                Title = "Review Elevated Permissions",
                Description = "Found users with elevated database permissions",
                Priority = "High",
                EstimatedImpact = "Reduced security risk",
                Actions = new List<string> { "Review and audit all CONTROL and ALTER permissions" }
            });
        }

        return recommendations;
    }

    private List<string> ParseColumns(string? columnString)
    {
        if (string.IsNullOrWhiteSpace(columnString))
            return new List<string>();

        return columnString
            .Trim('[', ']')
            .Split(',')
            .Select(c => c.Trim())
            .ToList();
    }
}