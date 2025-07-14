namespace SqlAnalyzer.Core.Analyzers
{
    /// <summary>
    /// Interface for analyzers that analyze database schema objects
    /// </summary>
    public interface ISchemaAnalyzer : IAnalyzer
    {
        /// <summary>
        /// Schema to analyze (null for all schemas)
        /// </summary>
        string SchemaFilter { get; set; }

        /// <summary>
        /// Pattern for object names to include (supports wildcards)
        /// </summary>
        string ObjectNamePattern { get; set; }
    }
}