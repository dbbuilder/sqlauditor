using System.Collections.Generic;
using SqlAnalyzer.Core.Connections;

namespace SqlAnalyzer.Core.Optimization
{
    /// <summary>
    /// Interface for query optimization
    /// </summary>
    public interface IQueryOptimizer
    {
        /// <summary>
        /// Adds NOLOCK hint to SQL Server queries for read-only operations
        /// </summary>
        string AddNoLockHint(string query, DatabaseType databaseType);

        /// <summary>
        /// Adds pagination to a query
        /// </summary>
        string AddPagination(string query, int pageSize, int offset, DatabaseType databaseType);

        /// <summary>
        /// Optimizes a query for read-only operations
        /// </summary>
        string OptimizeForReadOnly(string query, DatabaseType databaseType, QueryOptimizationOptions options = null);

        /// <summary>
        /// Creates batched queries for large IN clauses
        /// </summary>
        List<BatchedQuery> CreateBatchedQuery<T>(string query, string parameterName, IEnumerable<T> values, int batchSize = 1000);

        /// <summary>
        /// Estimates the relative cost of a query
        /// </summary>
        int EstimateQueryCost(string query);

        /// <summary>
        /// Suggests indexes based on query analysis
        /// </summary>
        List<string> SuggestIndexes(string query, string tableName);
    }

    /// <summary>
    /// Options for query optimization
    /// </summary>
    public class QueryOptimizationOptions
    {
        public bool AddNoLock { get; set; } = true;
        public bool EnablePagination { get; set; } = false;
        public int PageSize { get; set; } = 100;
        public int Offset { get; set; } = 0;
    }

    /// <summary>
    /// Represents a batched query with parameter values
    /// </summary>
    public class BatchedQuery
    {
        public string Query { get; set; }
        public List<object> ParameterValues { get; set; } = new List<object>();
    }
}