using System;
using FluentAssertions;
using SqlAnalyzer.Core.Connections;
using SqlAnalyzer.Core.Optimization;
using Xunit;

namespace SqlAnalyzer.Tests.Optimization
{
    public class QueryOptimizerTests
    {
        private readonly QueryOptimizer _optimizer;

        public QueryOptimizerTests()
        {
            _optimizer = new QueryOptimizer();
        }

        [Theory]
        [InlineData(DatabaseType.SqlServer, "SELECT * FROM Users", "SELECT * FROM Users WITH (NOLOCK)")]
        [InlineData(DatabaseType.SqlServer, "SELECT * FROM dbo.Orders", "SELECT * FROM dbo.Orders WITH (NOLOCK)")]
        [InlineData(DatabaseType.SqlServer, "SELECT * FROM [Sales].[Orders]", "SELECT * FROM [Sales].[Orders] WITH (NOLOCK)")]
        public void AddNoLockHint_ForSqlServer_ShouldAddHint(DatabaseType dbType, string query, string expected)
        {
            // Act
            var result = _optimizer.AddNoLockHint(query, dbType);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(DatabaseType.PostgreSql, "SELECT * FROM users")]
        [InlineData(DatabaseType.MySql, "SELECT * FROM orders")]
        public void AddNoLockHint_ForNonSqlServer_ShouldReturnOriginalQuery(DatabaseType dbType, string query)
        {
            // Act
            var result = _optimizer.AddNoLockHint(query, dbType);

            // Assert
            result.Should().Be(query);
        }

        [Fact]
        public void AddNoLockHint_WithExistingNoLock_ShouldNotDuplicate()
        {
            // Arrange
            var query = "SELECT * FROM Users WITH (NOLOCK)";

            // Act
            var result = _optimizer.AddNoLockHint(query, DatabaseType.SqlServer);

            // Assert
            result.Should().Be(query);
        }

        [Fact]
        public void AddNoLockHint_WithMultipleTables_ShouldAddToAll()
        {
            // Arrange
            var query = @"
                SELECT u.*, o.*
                FROM Users u
                INNER JOIN Orders o ON u.Id = o.UserId";

            // Act
            var result = _optimizer.AddNoLockHint(query, DatabaseType.SqlServer);

            // Assert
            result.Should().Contain("Users u WITH (NOLOCK)");
            result.Should().Contain("Orders o WITH (NOLOCK)");
        }

        [Theory]
        [InlineData("SELECT * FROM Users", 10, 0, "SELECT * FROM Users ORDER BY 1 OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY")]
        [InlineData("SELECT * FROM Users", 20, 40, "SELECT * FROM Users ORDER BY 1 OFFSET 40 ROWS FETCH NEXT 20 ROWS ONLY")]
        public void AddPagination_ForSqlServer_ShouldAddOffsetFetch(string query, int pageSize, int offset, string expected)
        {
            // Act
            var result = _optimizer.AddPagination(query, pageSize, offset, DatabaseType.SqlServer);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("SELECT * FROM users", 10, 0, "SELECT * FROM users LIMIT 10 OFFSET 0")]
        [InlineData("SELECT * FROM users", 20, 40, "SELECT * FROM users LIMIT 20 OFFSET 40")]
        public void AddPagination_ForPostgreSQL_ShouldAddLimitOffset(string query, int pageSize, int offset, string expected)
        {
            // Act
            var result = _optimizer.AddPagination(query, pageSize, offset, DatabaseType.PostgreSql);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("SELECT * FROM users", 10, 0, "SELECT * FROM users LIMIT 0, 10")]
        [InlineData("SELECT * FROM users", 20, 40, "SELECT * FROM users LIMIT 40, 20")]
        public void AddPagination_ForMySQL_ShouldAddLimit(string query, int pageSize, int offset, string expected)
        {
            // Act
            var result = _optimizer.AddPagination(query, pageSize, offset, DatabaseType.MySql);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void AddPagination_WithExistingOrderBy_ShouldNotAddDefault()
        {
            // Arrange
            var query = "SELECT * FROM Users ORDER BY Name";

            // Act
            var result = _optimizer.AddPagination(query, 10, 0, DatabaseType.SqlServer);

            // Assert
            result.Should().Be("SELECT * FROM Users ORDER BY Name OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(10001)]
        public void AddPagination_WithInvalidPageSize_ShouldThrow(int pageSize)
        {
            // Act & Assert
            var act = () => _optimizer.AddPagination("SELECT * FROM Users", pageSize, 0, DatabaseType.SqlServer);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void AddPagination_WithNegativeOffset_ShouldThrow()
        {
            // Act & Assert
            var act = () => _optimizer.AddPagination("SELECT * FROM Users", 10, -1, DatabaseType.SqlServer);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void OptimizeForReadOnly_ShouldCombineOptimizations()
        {
            // Arrange
            var query = "SELECT * FROM Users";
            var options = new QueryOptimizationOptions
            {
                AddNoLock = true,
                EnablePagination = true,
                PageSize = 100,
                Offset = 0
            };

            // Act
            var result = _optimizer.OptimizeForReadOnly(query, DatabaseType.SqlServer, options);

            // Assert
            result.Should().Contain("WITH (NOLOCK)");
            result.Should().Contain("OFFSET 0 ROWS FETCH NEXT 100 ROWS ONLY");
        }

        [Fact]
        public void CreateBatchedQuery_ShouldSplitLargeInClauses()
        {
            // Arrange
            var ids = new int[2500]; // More than typical IN clause limit
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = i + 1;
            }

            var query = "SELECT * FROM Users WHERE Id IN (@Ids)";

            // Act
            var batches = _optimizer.CreateBatchedQuery(query, "@Ids", ids, 1000);

            // Assert
            batches.Should().HaveCount(3);
            batches[0].ParameterValues.Should().HaveCount(1000);
            batches[1].ParameterValues.Should().HaveCount(1000);
            batches[2].ParameterValues.Should().HaveCount(500);
        }

        [Fact]
        public void EstimateQueryCost_ShouldReturnHigherCostForComplexQueries()
        {
            // Arrange
            var simpleQuery = "SELECT * FROM Users";
            var complexQuery = @"
                SELECT u.*, o.*, p.*
                FROM Users u
                INNER JOIN Orders o ON u.Id = o.UserId
                INNER JOIN Products p ON o.ProductId = p.Id
                WHERE u.CreatedDate > '2023-01-01'
                GROUP BY u.Id
                HAVING COUNT(o.Id) > 5
                ORDER BY u.Name";

            // Act
            var simpleCost = _optimizer.EstimateQueryCost(simpleQuery);
            var complexCost = _optimizer.EstimateQueryCost(complexQuery);

            // Assert
            complexCost.Should().BeGreaterThan(simpleCost);
        }

        [Fact]
        public void SuggestIndexes_ForWhereClause_ShouldRecommendIndex()
        {
            // Arrange
            var query = "SELECT * FROM Users WHERE Email = @Email AND IsActive = 1";

            // Act
            var suggestions = _optimizer.SuggestIndexes(query, "Users");

            // Assert
            suggestions.Should().NotBeEmpty();
            suggestions.Should().Contain(s => s.Contains("Email") && s.Contains("IsActive"));
        }
    }
}