using System.Threading.Tasks;
using SqlAnalyzer.Core.Models;

namespace SqlAnalyzer.Core.Analyzers
{
    /// <summary>
    /// Base interface for all analyzers
    /// </summary>
    public interface IAnalyzer
    {
        /// <summary>
        /// Name of the analyzer
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Description of what the analyzer checks
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Category of the analyzer (Schema, Performance, Security, etc.)
        /// </summary>
        string Category { get; }

        /// <summary>
        /// Performs the analysis
        /// </summary>
        /// <returns>Analysis result with findings</returns>
        Task<AnalysisResult> AnalyzeAsync();

        /// <summary>
        /// Checks if the analyzer is applicable to the current database type
        /// </summary>
        /// <returns>True if analyzer can run on the current database</returns>
        bool IsApplicable();
    }

    /// <summary>
    /// Generic interface for analyzers that work with specific data types
    /// </summary>
    /// <typeparam name="T">Type of data the analyzer works with</typeparam>
    public interface IAnalyzer<T> : IAnalyzer
    {
        /// <summary>
        /// Collects data for analysis
        /// </summary>
        /// <returns>Collection of data to analyze</returns>
        Task<System.Collections.Generic.List<T>> CollectDataAsync();
    }
}