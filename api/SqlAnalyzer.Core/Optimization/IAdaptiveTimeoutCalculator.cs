namespace SqlAnalyzer.Core.Optimization
{
    /// <summary>
    /// Interface for calculating adaptive timeouts based on various factors
    /// </summary>
    public interface IAdaptiveTimeoutCalculator
    {
        /// <summary>
        /// Calculates timeout based on database size
        /// </summary>
        int CalculateTimeout(decimal databaseSizeMB, TimeoutCalculationOptions options = null);

        /// <summary>
        /// Calculates timeout for analysis operations
        /// </summary>
        int CalculateAnalysisTimeout(AnalysisContext context);

        /// <summary>
        /// Records execution time for future predictions
        /// </summary>
        void RecordExecutionTime(string operation, decimal databaseSizeMB, int actualExecutionSeconds);

        /// <summary>
        /// Gets suggested timeout based on historical data
        /// </summary>
        int GetSuggestedTimeout(string operation, decimal databaseSizeMB);

        /// <summary>
        /// Adjusts timeout based on network latency
        /// </summary>
        int AdjustForNetworkLatency(int baseTimeout, int networkLatencyMs);

        /// <summary>
        /// Adjusts timeout based on system load
        /// </summary>
        int AdjustForSystemLoad(int baseTimeout, double cpuUsage);

        /// <summary>
        /// Gets dynamic timeout considering all factors
        /// </summary>
        int GetDynamicTimeout(DynamicTimeoutContext context);
    }

    /// <summary>
    /// Options for timeout calculation
    /// </summary>
    public class TimeoutCalculationOptions
    {
        public int MinTimeout { get; set; } = 30;
        public int MaxTimeout { get; set; } = 600;
        public decimal BaseFactor { get; set; } = 0.01m;
        public bool EnableAdaptive { get; set; } = true;
    }

    /// <summary>
    /// Context for analysis timeout calculation
    /// </summary>
    public class AnalysisContext
    {
        public decimal DatabaseSizeMB { get; set; }
        public int TableCount { get; set; }
        public int TotalObjectCount { get; set; }
        public bool HasComplexQueries { get; set; }
        public int EstimatedRowCount { get; set; }
    }

    /// <summary>
    /// Context for dynamic timeout calculation
    /// </summary>
    public class DynamicTimeoutContext
    {
        public string Operation { get; set; }
        public decimal DatabaseSizeMB { get; set; }
        public int ObjectCount { get; set; }
        public int NetworkLatencyMs { get; set; }
        public double SystemCpuUsage { get; set; }
        public int[] HistoricalExecutionTimes { get; set; }
    }
}