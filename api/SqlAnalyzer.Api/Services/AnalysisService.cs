using System.Collections.Concurrent;
using System.Data;
using Microsoft.AspNetCore.SignalR;
using SqlAnalyzer.Api.Hubs;
using SqlAnalyzer.Api.Models;
using SqlAnalyzer.Core.Analyzers;
using SqlAnalyzer.Core.Connections;

namespace SqlAnalyzer.Api.Services
{
    public interface IAnalysisService
    {
        Task<string> StartAnalysisAsync(AnalysisRequest request);
        Task<AnalysisStatus?> GetAnalysisStatusAsync(string jobId);
        Task<AnalysisResult?> GetAnalysisResultsAsync(string jobId);
        Task<bool> CancelAnalysisAsync(string jobId);
        Task<IEnumerable<AnalysisHistoryItem>> GetAnalysisHistoryAsync(int page, int pageSize);
        Task<byte[]?> ExportResultsAsync(string jobId, string format);
    }

    public class AnalysisService : IAnalysisService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<AnalysisHub>? _hubContext;
        private readonly ILogger<AnalysisService> _logger;
        private readonly ConcurrentDictionary<string, AnalysisJob> _jobs = new();

        public AnalysisService(
            IServiceProvider serviceProvider,
            ILogger<AnalysisService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

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

        public async Task<string> StartAnalysisAsync(AnalysisRequest request)
        {
            var jobId = Guid.NewGuid().ToString();
            var job = new AnalysisJob
            {
                Id = jobId,
                Request = request,
                Status = new AnalysisStatus
                {
                    JobId = jobId,
                    Status = "Queued",
                    StartedAt = DateTime.UtcNow,
                    CurrentStep = "Initializing"
                },
                CancellationTokenSource = new CancellationTokenSource()
            };

            _jobs[jobId] = job;

            // Start analysis in background
            _ = Task.Run(async () => await RunAnalysisAsync(job), job.CancellationTokenSource.Token);

            return jobId;
        }

        private async Task RunAnalysisAsync(AnalysisJob job)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var connectionFactory = scope.ServiceProvider.GetRequiredService<IConnectionFactory>();

                // Update status
                await UpdateJobStatus(job, "Running", 0, "Connecting to database");

                using var connection = connectionFactory.CreateConnection(
                    job.Request.ConnectionString,
                    job.Request.DatabaseType);

                // Connection will be opened automatically when executing queries

                var result = new AnalysisResult
                {
                    JobId = job.Id,
                    AnalyzedAt = DateTime.UtcNow
                };

                // Step 1: Database Info (10%)
                await UpdateJobStatus(job, "Running", 10, "Gathering database information");
                result.Database = await GatherDatabaseInfo(connection);

                // Step 2: Table Analysis (20%)
                if (job.Request.Options.IncludeIndexAnalysis)
                {
                    await UpdateJobStatus(job, "Running", 20, "Analyzing tables and indexes");
                    await AnalyzeTables(connection, result);
                }

                // Step 3: Performance Analysis (40%)
                if (job.Request.Options.IncludeQueryPerformance)
                {
                    await UpdateJobStatus(job, "Running", 40, "Analyzing query performance");
                    result.Performance = await AnalyzePerformance(connection, job.Request.Options);
                }

                // Step 4: Security Audit (60%)
                if (job.Request.Options.IncludeSecurityAudit)
                {
                    await UpdateJobStatus(job, "Running", 60, "Performing security audit");
                    result.Security = await PerformSecurityAudit(connection);
                }

                // Step 5: Generate Recommendations (80%)
                await UpdateJobStatus(job, "Running", 80, "Generating recommendations");
                result.Recommendations = GenerateRecommendations(result);

                // Step 6: Complete (100%)
                job.Result = result;
                await UpdateJobStatus(job, "Completed", 100, "Analysis completed successfully");
            }
            catch (OperationCanceledException)
            {
                await UpdateJobStatus(job, "Cancelled", job.Status.ProgressPercentage, "Analysis cancelled by user");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Analysis failed for job {JobId}", job.Id);
                job.Status.ErrorMessage = ex.Message;
                await UpdateJobStatus(job, "Failed", job.Status.ProgressPercentage, $"Analysis failed: {ex.Message}");
            }
        }

        private async Task UpdateJobStatus(AnalysisJob job, string status, double progress, string currentStep)
        {
            job.Status.Status = status;
            job.Status.ProgressPercentage = progress;
            job.Status.CurrentStep = currentStep;

            if (status == "Completed" || status == "Failed" || status == "Cancelled")
            {
                job.Status.CompletedAt = DateTime.UtcNow;
            }

            // Send real-time update via SignalR to job-specific group (if enabled)
            if (_hubContext != null)
            {
                await _hubContext.Clients.Group($"job-{job.Id}").SendAsync("AnalysisProgress", job.Status);
            }
        }

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
            if (result.Performance.MissingIndexes.Any())
            {
                recommendations.Add(new Recommendation
                {
                    Category = "Performance",
                    Title = "Create Missing Indexes",
                    Description = $"Found {result.Performance.MissingIndexes.Count} missing indexes that could improve query performance",
                    Priority = "High",
                    EstimatedImpact = "20-50% query performance improvement",
                    Actions = result.Performance.MissingIndexes.Select(i =>
                        $"Create index on {i.TableName} ({string.Join(", ", i.EqualityColumns)})").ToList()
                });
            }

            if (result.Performance.FragmentedIndexes.Any())
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
            if (result.Security.HasElevatedPermissions)
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

        public async Task<AnalysisStatus?> GetAnalysisStatusAsync(string jobId)
        {
            return _jobs.TryGetValue(jobId, out var job) ? job.Status : null;
        }

        public async Task<AnalysisResult?> GetAnalysisResultsAsync(string jobId)
        {
            return _jobs.TryGetValue(jobId, out var job) ? job.Result : null;
        }

        public async Task<bool> CancelAnalysisAsync(string jobId)
        {
            if (_jobs.TryGetValue(jobId, out var job))
            {
                job.CancellationTokenSource.Cancel();
                return true;
            }
            return false;
        }

        public async Task<IEnumerable<AnalysisHistoryItem>> GetAnalysisHistoryAsync(int page, int pageSize)
        {
            return _jobs.Values
                .Select(j => new AnalysisHistoryItem
                {
                    JobId = j.Id,
                    DatabaseName = j.Result?.Database.Name ?? "Unknown",
                    AnalysisType = j.Request.AnalysisType,
                    StartedAt = j.Status.StartedAt,
                    CompletedAt = j.Status.CompletedAt,
                    Status = j.Status.Status,
                    FindingsCount = j.Result?.Findings.Count ?? 0,
                    ErrorMessage = j.Status.ErrorMessage
                })
                .OrderByDescending(h => h.StartedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);
        }

        public async Task<byte[]?> ExportResultsAsync(string jobId, string format)
        {
            if (!_jobs.TryGetValue(jobId, out var job) || job.Result == null)
                return null;

            return format.ToLower() switch
            {
                "json" => System.Text.Encoding.UTF8.GetBytes(
                    System.Text.Json.JsonSerializer.Serialize(job.Result, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    })),
                _ => null
            };
        }

        private class AnalysisJob
        {
            public string Id { get; set; } = string.Empty;
            public AnalysisRequest Request { get; set; } = new();
            public AnalysisStatus Status { get; set; } = new();
            public AnalysisResult? Result { get; set; }
            public CancellationTokenSource CancellationTokenSource { get; set; } = new();
        }
    }
}