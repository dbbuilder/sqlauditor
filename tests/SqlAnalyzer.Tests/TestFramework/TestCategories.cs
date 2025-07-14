namespace SqlAnalyzer.Tests.TestFramework
{
    /// <summary>
    /// Standard test categories for organizing and filtering tests
    /// </summary>
    public static class TestCategories
    {
        /// <summary>
        /// Unit tests that test individual components in isolation
        /// </summary>
        public const string Unit = "Unit";

        /// <summary>
        /// Integration tests that test component interactions
        /// </summary>
        public const string Integration = "Integration";

        /// <summary>
        /// End-to-end tests that test complete scenarios
        /// </summary>
        public const string E2E = "E2E";

        /// <summary>
        /// Performance tests that measure execution time and resource usage
        /// </summary>
        public const string Performance = "Performance";

        /// <summary>
        /// Security tests that verify security measures
        /// </summary>
        public const string Security = "Security";

        /// <summary>
        /// Tests that require a SQL Server database connection
        /// </summary>
        public const string SqlServer = "SqlServer";

        /// <summary>
        /// Tests that require a PostgreSQL database connection
        /// </summary>
        public const string PostgreSql = "PostgreSql";

        /// <summary>
        /// Tests that require a MySQL database connection
        /// </summary>
        public const string MySql = "MySql";

        /// <summary>
        /// Tests that are known to be slow (> 1 second)
        /// </summary>
        public const string Slow = "Slow";

        /// <summary>
        /// Tests that use external resources (databases, files, network)
        /// </summary>
        public const string External = "External";

        /// <summary>
        /// Tests that can be flaky and might need retries
        /// </summary>
        public const string Flaky = "Flaky";

        /// <summary>
        /// Smoke tests for basic functionality verification
        /// </summary>
        public const string Smoke = "Smoke";

        /// <summary>
        /// Tests for connection-related functionality
        /// </summary>
        public const string Connection = "Connection";

        /// <summary>
        /// Tests for analyzer functionality
        /// </summary>
        public const string Analyzer = "Analyzer";

        /// <summary>
        /// Tests for caching functionality
        /// </summary>
        public const string Caching = "Caching";

        /// <summary>
        /// Tests for resilience patterns (retry, circuit breaker)
        /// </summary>
        public const string Resilience = "Resilience";

        /// <summary>
        /// Tests for configuration functionality
        /// </summary>
        public const string Configuration = "Configuration";
    }

    /// <summary>
    /// Custom traits for additional test metadata
    /// </summary>
    public static class TestTraits
    {
        /// <summary>
        /// Trait for test priority
        /// </summary>
        public static class Priority
        {
            public const string TraitName = "Priority";
            public const string Critical = "Critical";
            public const string High = "High";
            public const string Medium = "Medium";
            public const string Low = "Low";
        }

        /// <summary>
        /// Trait for required environment
        /// </summary>
        public static class Environment
        {
            public const string TraitName = "Environment";
            public const string Development = "Development";
            public const string CI = "CI";
            public const string Production = "Production";
            public const string Any = "Any";
        }

        /// <summary>
        /// Trait for test owner/team
        /// </summary>
        public static class Owner
        {
            public const string TraitName = "Owner";
            public const string Core = "Core";
            public const string Integration = "Integration";
            public const string Security = "Security";
            public const string Performance = "Performance";
        }

        /// <summary>
        /// Trait for bug/issue tracking
        /// </summary>
        public static class Bug
        {
            public const string TraitName = "Bug";
            // Add bug IDs as needed
        }

        /// <summary>
        /// Trait for feature tracking
        /// </summary>
        public static class Feature
        {
            public const string TraitName = "Feature";
            public const string ConnectionManagement = "ConnectionManagement";
            public const string DatabaseAnalysis = "DatabaseAnalysis";
            public const string SecurityValidation = "SecurityValidation";
            public const string PerformanceOptimization = "PerformanceOptimization";
            public const string Caching = "Caching";
            public const string Resilience = "Resilience";
        }
    }
}