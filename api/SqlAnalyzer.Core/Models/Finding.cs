using System;
using System.Collections.Generic;

namespace SqlAnalyzer.Core.Models
{
    /// <summary>
    /// Represents a single finding from an analysis
    /// </summary>
    public class Finding
    {
        /// <summary>
        /// Unique identifier for the finding
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Severity level of the finding
        /// </summary>
        public Severity Severity { get; set; }

        /// <summary>
        /// Category of the finding (e.g., Performance, Security, BestPractice)
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Brief description of the finding
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Detailed description of the issue
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Recommended action to address the finding
        /// </summary>
        public string Recommendation { get; set; }

        /// <summary>
        /// Impact of not addressing this finding
        /// </summary>
        public string Impact { get; set; }

        /// <summary>
        /// Database object affected by this finding
        /// </summary>
        public string AffectedObject { get; set; }

        /// <summary>
        /// Type of the affected object (Table, Index, Procedure, etc.)
        /// </summary>
        public string ObjectType { get; set; }

        /// <summary>
        /// Schema of the affected object
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// SQL script to fix the issue (if applicable)
        /// </summary>
        public string RemediationScript { get; set; }

        /// <summary>
        /// Timestamp when the finding was discovered
        /// </summary>
        public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional properties specific to the finding
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Tags for categorization and filtering
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
    }

    /// <summary>
    /// Severity levels for findings
    /// </summary>
    public enum Severity
    {
        /// <summary>
        /// Informational finding, no action required
        /// </summary>
        Info = 0,

        /// <summary>
        /// Warning that should be investigated
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Error that should be fixed
        /// </summary>
        Error = 2,

        /// <summary>
        /// Critical issue requiring immediate attention
        /// </summary>
        Critical = 3
    }
}