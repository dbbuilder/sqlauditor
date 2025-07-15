using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SqlAnalyzer.Core.Optimization
{
    /// <summary>
    /// Calculates adaptive timeouts based on database size, complexity, and historical performance
    /// </summary>
    public class AdaptiveTimeoutCalculator : IAdaptiveTimeoutCalculator
    {
        private readonly ConcurrentDictionary<string, List<ExecutionRecord>> _executionHistory;
        private const int DefaultTimeout = 30;
        private const int MaxHistorySize = 100;

        public AdaptiveTimeoutCalculator()
        {
            _executionHistory = new ConcurrentDictionary<string, List<ExecutionRecord>>();
        }

        public int CalculateTimeout(decimal databaseSizeMB, TimeoutCalculationOptions options = null)
        {
            options ??= new TimeoutCalculationOptions();

            if (databaseSizeMB <= 0)
                return DefaultTimeout;

            // Base calculation: 30 seconds + (size in GB * 10 seconds)
            var sizeInGB = databaseSizeMB / 1024m;
            var calculatedTimeout = (int)(DefaultTimeout + (sizeInGB * 10));

            // Apply factor if specified
            if (options.BaseFactor > 0)
            {
                calculatedTimeout = (int)(DefaultTimeout + (databaseSizeMB * options.BaseFactor));
            }

            // Ensure within bounds
            return Math.Max(options.MinTimeout, Math.Min(calculatedTimeout, options.MaxTimeout));
        }

        public int CalculateAnalysisTimeout(AnalysisContext context)
        {
            // Start with base timeout for database size
            var baseTimeout = CalculateTimeout(context.DatabaseSizeMB);

            // Adjust for object count
            var objectFactor = 0;
            if (context.TotalObjectCount > 100)
            {
                objectFactor = (int)Math.Log10(context.TotalObjectCount) * 5;
            }

            // Adjust for complexity
            var complexityFactor = context.HasComplexQueries ? 20 : 0;

            // Calculate total
            var totalTimeout = baseTimeout + objectFactor + complexityFactor;

            return Math.Min(totalTimeout, 300); // Cap at 5 minutes for analysis
        }

        public void RecordExecutionTime(string operation, decimal databaseSizeMB, int actualExecutionSeconds)
        {
            var record = new ExecutionRecord
            {
                DatabaseSizeMB = databaseSizeMB,
                ExecutionTimeSeconds = actualExecutionSeconds,
                RecordedAt = DateTime.UtcNow
            };

            _executionHistory.AddOrUpdate(operation,
                new List<ExecutionRecord> { record },
                (key, list) =>
                {
                    list.Add(record);

                    // Keep only recent history
                    if (list.Count > MaxHistorySize)
                    {
                        list.RemoveAt(0);
                    }

                    return list;
                });
        }

        public int GetSuggestedTimeout(string operation, decimal databaseSizeMB)
        {
            if (_executionHistory.TryGetValue(operation, out var history) && history.Any())
            {
                // Find similar sized databases
                var similarExecutions = history
                    .Where(h => Math.Abs(h.DatabaseSizeMB - databaseSizeMB) < databaseSizeMB * 0.2m)
                    .ToList();

                if (similarExecutions.Any())
                {
                    // Use 95th percentile of execution times
                    var times = similarExecutions.Select(e => e.ExecutionTimeSeconds).OrderBy(t => t).ToList();
                    var percentileIndex = (int)(times.Count * 0.95);
                    var percentileTime = times[Math.Min(percentileIndex, times.Count - 1)];

                    // Add 20% buffer
                    return (int)(percentileTime * 1.2);
                }
            }

            // No history, use calculated timeout
            return CalculateTimeout(databaseSizeMB);
        }

        public int AdjustForNetworkLatency(int baseTimeout, int networkLatencyMs)
        {
            if (networkLatencyMs <= 0)
                return baseTimeout;

            // Add network overhead: 1 second per 50ms of latency
            var networkOverhead = (networkLatencyMs / 50);
            return baseTimeout + networkOverhead;
        }

        public int AdjustForSystemLoad(int baseTimeout, double cpuUsage)
        {
            if (cpuUsage < 0 || cpuUsage > 1)
                return baseTimeout;

            // High CPU usage (>70%) - reduce timeout by 20%
            // Low CPU usage (<30%) - increase timeout by 20%
            // Medium usage - no change
            if (cpuUsage > 0.7)
            {
                return (int)(baseTimeout * 0.8);
            }
            else if (cpuUsage < 0.3)
            {
                return (int)(baseTimeout * 1.2);
            }

            return baseTimeout;
        }

        public int GetDynamicTimeout(DynamicTimeoutContext context)
        {
            // Start with base calculation
            var baseTimeout = CalculateTimeout(context.DatabaseSizeMB);

            // Adjust for historical data if available
            if (context.HistoricalExecutionTimes?.Any() == true)
            {
                var maxHistorical = context.HistoricalExecutionTimes.Max();
                baseTimeout = Math.Max(baseTimeout, (int)(maxHistorical * 1.3)); // 30% buffer
            }

            // Adjust for network latency
            baseTimeout = AdjustForNetworkLatency(baseTimeout, context.NetworkLatencyMs);

            // Adjust for system load
            baseTimeout = AdjustForSystemLoad(baseTimeout, context.SystemCpuUsage);

            // Add complexity factor based on object count
            if (context.ObjectCount > 1000)
            {
                baseTimeout += (int)(Math.Log10(context.ObjectCount) * 10);
            }

            // Ensure reasonable bounds
            return Math.Max(30, Math.Min(baseTimeout, 600));
        }

        private class ExecutionRecord
        {
            public decimal DatabaseSizeMB { get; set; }
            public int ExecutionTimeSeconds { get; set; }
            public DateTime RecordedAt { get; set; }
        }
    }
}