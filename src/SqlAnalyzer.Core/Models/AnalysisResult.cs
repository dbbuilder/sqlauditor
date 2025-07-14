using System;
using System.Collections.Generic;

namespace SqlAnalyzer.Core.Models
{
    /// <summary>
    /// Represents the result of a database analysis
    /// </summary>
    public class AnalysisResult
    {
        /// <summary>
        /// Name of the analyzer that produced this result
        /// </summary>
        public string AnalyzerName { get; set; }

        /// <summary>
        /// Name of the database analyzed
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Server name where the database resides
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Type of database (SQL Server, PostgreSQL, MySQL)
        /// </summary>
        public string DatabaseType { get; set; }

        /// <summary>
        /// Time when analysis started
        /// </summary>
        public DateTime AnalysisStartTime { get; set; }

        /// <summary>
        /// Time when analysis completed
        /// </summary>
        public DateTime AnalysisEndTime { get; set; }

        /// <summary>
        /// Total duration of the analysis
        /// </summary>
        public TimeSpan Duration => AnalysisEndTime - AnalysisStartTime;

        /// <summary>
        /// Whether the analysis completed successfully
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Error message if analysis failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// List of findings from the analysis
        /// </summary>
        public List<Finding> Findings { get; set; } = new List<Finding>();

        /// <summary>
        /// Summary statistics about the analysis
        /// </summary>
        public AnalysisSummary Summary { get; set; } = new AnalysisSummary();

        /// <summary>
        /// Additional metadata about the analysis
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Summary statistics for an analysis
    /// </summary>
    public class AnalysisSummary
    {
        /// <summary>
        /// Total number of objects analyzed
        /// </summary>
        public int TotalObjectsAnalyzed { get; set; }

        /// <summary>
        /// Number of critical findings
        /// </summary>
        public int CriticalFindings { get; set; }

        /// <summary>
        /// Number of error findings
        /// </summary>
        public int ErrorFindings { get; set; }

        /// <summary>
        /// Number of warning findings
        /// </summary>
        public int WarningFindings { get; set; }

        /// <summary>
        /// Number of informational findings
        /// </summary>
        public int InfoFindings { get; set; }

        /// <summary>
        /// Total number of findings
        /// </summary>
        public int TotalFindings => CriticalFindings + ErrorFindings + WarningFindings + InfoFindings;

        /// <summary>
        /// Total number of rows across all tables
        /// </summary>
        public long TotalRows { get; set; }
    }
}