using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SqlAnalyzer.Core.Connections;

namespace SqlAnalyzer.Core.Optimization
{
    /// <summary>
    /// Implements query optimization strategies for different database types
    /// </summary>
    public class QueryOptimizer : IQueryOptimizer
    {
        private static readonly Regex TablePattern = new Regex(
            @"FROM\s+(\[?[\w\s]+\]?\.?\[?[\w\s]+\]?)\s*(?:AS\s+)?(\w+)?",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private static readonly Regex JoinPattern = new Regex(
            @"JOIN\s+(\[?[\w\s]+\]?\.?\[?[\w\s]+\]?)\s*(?:AS\s+)?(\w+)?",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private static readonly Regex WherePattern = new Regex(
            @"WHERE\s+(.*?)(?:GROUP\s+BY|ORDER\s+BY|HAVING|$)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        public string AddNoLockHint(string query, DatabaseType databaseType)
        {
            if (databaseType != DatabaseType.SqlServer)
                return query;

            // Check if NOLOCK already exists
            if (query.Contains("WITH (NOLOCK)", StringComparison.OrdinalIgnoreCase))
                return query;

            var modifiedQuery = query;

            // Add NOLOCK to FROM clause
            modifiedQuery = TablePattern.Replace(modifiedQuery, match =>
            {
                var table = match.Groups[1].Value;
                var alias = match.Groups[2].Value;

                if (!string.IsNullOrWhiteSpace(alias))
                    return $"FROM {table} {alias} WITH (NOLOCK)";
                else
                    return $"FROM {table} WITH (NOLOCK)";
            });

            // Add NOLOCK to JOIN clauses
            modifiedQuery = JoinPattern.Replace(modifiedQuery, match =>
            {
                var table = match.Groups[1].Value;
                var alias = match.Groups[2].Value;
                var joinType = match.Value.Substring(0, match.Value.IndexOf(' '));

                if (!string.IsNullOrWhiteSpace(alias))
                    return $"{joinType} {table} {alias} WITH (NOLOCK)";
                else
                    return $"{joinType} {table} WITH (NOLOCK)";
            });

            return modifiedQuery;
        }

        public string AddPagination(string query, int pageSize, int offset, DatabaseType databaseType)
        {
            if (pageSize <= 0 || pageSize > 10000)
                throw new ArgumentException("Page size must be between 1 and 10000", nameof(pageSize));

            if (offset < 0)
                throw new ArgumentException("Offset cannot be negative", nameof(offset));

            return databaseType switch
            {
                DatabaseType.SqlServer => AddSqlServerPagination(query, pageSize, offset),
                DatabaseType.PostgreSql => AddPostgreSqlPagination(query, pageSize, offset),
                DatabaseType.MySql => AddMySqlPagination(query, pageSize, offset),
                _ => query
            };
        }

        private string AddSqlServerPagination(string query, int pageSize, int offset)
        {
            // Check if ORDER BY exists
            var hasOrderBy = query.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase);

            if (!hasOrderBy)
            {
                query += " ORDER BY 1"; // Default ordering by first column
            }

            return $"{query} OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
        }

        private string AddPostgreSqlPagination(string query, int pageSize, int offset)
        {
            return $"{query} LIMIT {pageSize} OFFSET {offset}";
        }

        private string AddMySqlPagination(string query, int pageSize, int offset)
        {
            return $"{query} LIMIT {offset}, {pageSize}";
        }

        public string OptimizeForReadOnly(string query, DatabaseType databaseType, QueryOptimizationOptions options = null)
        {
            options ??= new QueryOptimizationOptions();
            var optimizedQuery = query;

            if (options.AddNoLock)
            {
                optimizedQuery = AddNoLockHint(optimizedQuery, databaseType);
            }

            if (options.EnablePagination)
            {
                optimizedQuery = AddPagination(optimizedQuery, options.PageSize, options.Offset, databaseType);
            }

            return optimizedQuery;
        }

        public List<BatchedQuery> CreateBatchedQuery<T>(string query, string parameterName, IEnumerable<T> values, int batchSize = 1000)
        {
            var valuesList = values.ToList();
            var batches = new List<BatchedQuery>();

            for (int i = 0; i < valuesList.Count; i += batchSize)
            {
                var batchValues = valuesList.Skip(i).Take(batchSize).ToList();
                var batch = new BatchedQuery
                {
                    Query = query,
                    ParameterValues = batchValues.Cast<object>().ToList()
                };
                batches.Add(batch);
            }

            return batches;
        }

        public int EstimateQueryCost(string query)
        {
            var cost = 1; // Base cost
            var upperQuery = query.ToUpper();

            // Increase cost for joins
            cost += Regex.Matches(upperQuery, @"\bJOIN\b").Count * 2;

            // Increase cost for subqueries
            cost += Regex.Matches(upperQuery, @"\(SELECT\b").Count * 3;

            // Increase cost for aggregations
            if (upperQuery.Contains("GROUP BY"))
                cost += 2;

            // Increase cost for HAVING
            if (upperQuery.Contains("HAVING"))
                cost += 1;

            // Increase cost for ORDER BY
            if (upperQuery.Contains("ORDER BY"))
                cost += 1;

            // Increase cost for DISTINCT
            if (upperQuery.Contains("DISTINCT"))
                cost += 2;

            // Increase cost for wildcards in WHERE
            if (Regex.IsMatch(upperQuery, @"WHERE.*LIKE.*'%"))
                cost += 2;

            return cost;
        }

        public List<string> SuggestIndexes(string query, string tableName)
        {
            var suggestions = new List<string>();
            var upperQuery = query.ToUpper();

            // Extract WHERE clause conditions
            var whereMatch = WherePattern.Match(query);
            if (whereMatch.Success)
            {
                var whereClause = whereMatch.Groups[1].Value;
                var columnPattern = new Regex(@"(\w+)\s*=", RegexOptions.IgnoreCase);
                var columns = new List<string>();

                foreach (Match match in columnPattern.Matches(whereClause))
                {
                    var column = match.Groups[1].Value;
                    if (!column.StartsWith("@") && !column.StartsWith("'"))
                    {
                        columns.Add(column);
                    }
                }

                if (columns.Any())
                {
                    var indexColumns = string.Join(", ", columns);
                    suggestions.Add($"CREATE INDEX IX_{tableName}_{string.Join("_", columns)} ON {tableName} ({indexColumns})");
                }
            }

            // Check for JOIN conditions
            if (upperQuery.Contains("JOIN"))
            {
                var joinPattern = new Regex(@"ON\s+\w+\.(\w+)\s*=\s*\w+\.(\w+)", RegexOptions.IgnoreCase);
                foreach (Match match in joinPattern.Matches(query))
                {
                    var column1 = match.Groups[1].Value;
                    var column2 = match.Groups[2].Value;

                    suggestions.Add($"-- Consider index on {tableName}.{column1} or {column2} for JOIN performance");
                }
            }

            // Check for ORDER BY
            if (upperQuery.Contains("ORDER BY"))
            {
                var orderByPattern = new Regex(@"ORDER\s+BY\s+([\w\s,]+?)(?:ASC|DESC|$)", RegexOptions.IgnoreCase);
                var orderMatch = orderByPattern.Match(query);
                if (orderMatch.Success)
                {
                    var orderColumns = orderMatch.Groups[1].Value.Split(',').Select(c => c.Trim()).ToList();
                    if (orderColumns.Any())
                    {
                        suggestions.Add($"-- Consider index on {tableName} ({string.Join(", ", orderColumns)}) for ORDER BY performance");
                    }
                }
            }

            return suggestions;
        }
    }
}