using SqlAnalyzer.Core.Connections;

namespace SqlAnalyzer.Api.Models
{
    public class AnalysisRequest
    {
        public string ConnectionString { get; set; } = string.Empty;
        public DatabaseType DatabaseType { get; set; } = DatabaseType.SqlServer;
        public string AnalysisType { get; set; } = "comprehensive";
        public AnalysisOptions Options { get; set; } = new();
    }

    public class AnalysisOptions
    {
        public bool IncludeIndexAnalysis { get; set; } = true;
        public bool IncludeFragmentation { get; set; } = true;
        public bool IncludeStatistics { get; set; } = true;
        public bool IncludeSecurityAudit { get; set; } = true;
        public bool IncludeQueryPerformance { get; set; } = true;
        public bool IncludeDependencies { get; set; } = true;
        public int TimeoutMinutes { get; set; } = 30;
    }

    public class ConnectionTestRequest
    {
        public string ConnectionString { get; set; } = string.Empty;
        public DatabaseType DatabaseType { get; set; } = DatabaseType.SqlServer;
    }

    public class AnalysisStatus
    {
        public string JobId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Queued, Running, Completed, Failed, Cancelled
        public double ProgressPercentage { get; set; }
        public string CurrentStep { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;
        public string? ErrorMessage { get; set; }
    }

    public class AnalysisResult
    {
        public string JobId { get; set; } = string.Empty;
        public DatabaseInfo Database { get; set; } = new();
        public List<Finding> Findings { get; set; } = new();
        public PerformanceMetrics Performance { get; set; } = new();
        public SecurityAudit Security { get; set; } = new();
        public List<Recommendation> Recommendations { get; set; } = new();
        public DateTime AnalyzedAt { get; set; }
    }

    public class DatabaseInfo
    {
        public string Name { get; set; } = string.Empty;
        public string ServerVersion { get; set; } = string.Empty;
        public string Edition { get; set; } = string.Empty;
        public long SizeMB { get; set; }
        public int TableCount { get; set; }
        public int IndexCount { get; set; }
        public int ProcedureCount { get; set; }
        public int ViewCount { get; set; }
        public long TotalRows { get; set; }
    }

    public class Finding
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Category { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // Critical, High, Medium, Low, Info
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
        public Dictionary<string, object> Details { get; set; } = new();
    }

    public class PerformanceMetrics
    {
        public List<MissingIndex> MissingIndexes { get; set; } = new();
        public List<FragmentedIndex> FragmentedIndexes { get; set; } = new();
        public List<OutdatedStatistic> OutdatedStatistics { get; set; } = new();
        public List<SlowQuery> SlowQueries { get; set; } = new();
    }

    public class MissingIndex
    {
        public string TableName { get; set; } = string.Empty;
        public double ImpactScore { get; set; }
        public List<string> EqualityColumns { get; set; } = new();
        public List<string> InequalityColumns { get; set; } = new();
        public List<string> IncludedColumns { get; set; } = new();
        public string CreateStatement { get; set; } = string.Empty;
    }

    public class FragmentedIndex
    {
        public string TableName { get; set; } = string.Empty;
        public string IndexName { get; set; } = string.Empty;
        public double FragmentationPercent { get; set; }
        public int PageCount { get; set; }
        public string RebuildStatement { get; set; } = string.Empty;
    }

    public class OutdatedStatistic
    {
        public string TableName { get; set; } = string.Empty;
        public string StatisticName { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public int DaysSinceUpdate { get; set; }
        public long RowCount { get; set; }
        public long ModificationCount { get; set; }
        public string UpdateStatement { get; set; } = string.Empty;
    }

    public class SlowQuery
    {
        public string QueryText { get; set; } = string.Empty;
        public long ExecutionCount { get; set; }
        public double AvgDurationMs { get; set; }
        public long TotalDurationMs { get; set; }
        public long AvgLogicalReads { get; set; }
        public string QueryPlan { get; set; } = string.Empty;
    }

    public class SecurityAudit
    {
        public List<UserPermission> Permissions { get; set; } = new();
        public List<SecurityVulnerability> Vulnerabilities { get; set; } = new();
        public bool HasElevatedPermissions { get; set; }
        public bool HasWeakPasswords { get; set; }
    }

    public class UserPermission
    {
        public string PrincipalName { get; set; } = string.Empty;
        public string PrincipalType { get; set; } = string.Empty;
        public string PermissionName { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ObjectName { get; set; } = string.Empty;
    }

    public class SecurityVulnerability
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Remediation { get; set; } = string.Empty;
    }

    public class Recommendation
    {
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string EstimatedImpact { get; set; } = string.Empty;
        public List<string> Actions { get; set; } = new();
    }

    public class AnalysisHistoryItem
    {
        public string JobId { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string AnalysisType { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int FindingsCount { get; set; }
        public string? ErrorMessage { get; set; }
    }
}