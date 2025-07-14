using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SqlAnalyzer.Core.Connections;
using SqlAnalyzer.Core.Models;

namespace SqlAnalyzer.Core.Analyzers
{
    /// <summary>
    /// Base class for all analyzers
    /// </summary>
    /// <typeparam name="T">Type of data the analyzer works with</typeparam>
    public abstract class BaseAnalyzer<T> : IAnalyzer<T>
    {
        protected readonly ISqlAnalyzerConnection _connection;
        protected readonly ILogger _logger;
        protected readonly List<Finding> _findings;
        protected AnalysisResult _result;

        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract string Category { get; }

        protected BaseAnalyzer(ISqlAnalyzerConnection connection, ILogger logger)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _findings = new List<Finding>();
        }

        public virtual async Task<AnalysisResult> AnalyzeAsync()
        {
            _logger.LogInformation("Starting {AnalyzerName} analysis for database: {DatabaseName}", 
                Name, _connection.DatabaseName);

            var stopwatch = Stopwatch.StartNew();
            
            _result = new AnalysisResult
            {
                AnalyzerName = Name,
                DatabaseName = _connection.DatabaseName,
                ServerName = _connection.ServerName,
                DatabaseType = _connection.DatabaseType.ToString(),
                AnalysisStartTime = DateTime.UtcNow
            };

            try
            {
                // Collect data
                _logger.LogDebug("Collecting data for analysis");
                var data = await CollectDataAsync();
                _result.Summary.TotalObjectsAnalyzed = data?.Count ?? 0;

                // Analyze data
                if (data != null && data.Count > 0)
                {
                    _logger.LogDebug("Analyzing {Count} objects", data.Count);
                    await AnalyzeDataAsync(data);
                }
                else
                {
                    _logger.LogWarning("No data collected for analysis");
                }

                // Process findings
                ProcessFindings();
                
                _result.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during {AnalyzerName} analysis", Name);
                _result.Success = false;
                _result.ErrorMessage = ex.Message;
            }
            finally
            {
                stopwatch.Stop();
                _result.AnalysisEndTime = DateTime.UtcNow;
                
                _logger.LogInformation(
                    "Completed {AnalyzerName} analysis in {Duration}ms. Found {FindingCount} issues", 
                    Name, stopwatch.ElapsedMilliseconds, _findings.Count);
            }

            return _result;
        }

        public virtual bool IsApplicable()
        {
            // By default, all analyzers are applicable
            // Override in derived classes for database-specific analyzers
            return true;
        }

        /// <summary>
        /// Collects data from the database for analysis
        /// </summary>
        protected abstract Task<List<T>> CollectDataAsync();

        /// <summary>
        /// Analyzes the collected data and generates findings
        /// </summary>
        protected abstract Task AnalyzeDataAsync(List<T> data);

        async Task<List<T>> IAnalyzer<T>.CollectDataAsync()
        {
            return await CollectDataAsync();
        }

        /// <summary>
        /// Adds a finding to the results
        /// </summary>
        protected void AddFinding(Severity severity, string message, string recommendation, 
            string category = null, string affectedObject = null, string objectType = null)
        {
            var finding = new Finding
            {
                Severity = severity,
                Message = message,
                Recommendation = recommendation,
                Category = category ?? Category,
                AffectedObject = affectedObject,
                ObjectType = objectType,
                Schema = ExtractSchema(affectedObject),
                DiscoveredAt = DateTime.UtcNow
            };

            _findings.Add(finding);
            
            _logger.LogDebug("Added {Severity} finding: {Message}", severity, message);
        }

        /// <summary>
        /// Adds a finding with a remediation script
        /// </summary>
        protected void AddFindingWithScript(Severity severity, string message, string recommendation, 
            string remediationScript, string category = null, string affectedObject = null, string objectType = null)
        {
            var finding = new Finding
            {
                Severity = severity,
                Message = message,
                Recommendation = recommendation,
                RemediationScript = remediationScript,
                Category = category ?? Category,
                AffectedObject = affectedObject,
                ObjectType = objectType,
                Schema = ExtractSchema(affectedObject),
                DiscoveredAt = DateTime.UtcNow
            };

            _findings.Add(finding);
            
            _logger.LogDebug("Added {Severity} finding with script: {Message}", severity, message);
        }

        /// <summary>
        /// Processes findings and updates summary statistics
        /// </summary>
        private void ProcessFindings()
        {
            _result.Findings = _findings;
            
            foreach (var finding in _findings)
            {
                switch (finding.Severity)
                {
                    case Severity.Critical:
                        _result.Summary.CriticalFindings++;
                        break;
                    case Severity.Error:
                        _result.Summary.ErrorFindings++;
                        break;
                    case Severity.Warning:
                        _result.Summary.WarningFindings++;
                        break;
                    case Severity.Info:
                        _result.Summary.InfoFindings++;
                        break;
                }
            }
        }

        /// <summary>
        /// Extracts schema name from a qualified object name
        /// </summary>
        private string ExtractSchema(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return null;

            var parts = objectName.Split('.');
            return parts.Length > 1 ? parts[0] : "dbo"; // Default to dbo for SQL Server
        }
    }
}