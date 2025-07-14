using Xunit;

// Configure test parallelization at the assembly level
[assembly: CollectionBehavior(
    DisableTestParallelization = false,
    MaxParallelThreads = 4)]

// Configure test collection behavior
[assembly: TestCollectionOrderer(
    "SqlAnalyzer.Tests.TestFramework.PriorityTestCollectionOrderer",
    "SqlAnalyzer.Tests")]

// Configure test case orderer for priority-based execution
[assembly: TestCaseOrderer(
    "SqlAnalyzer.Tests.TestFramework.PriorityTestCaseOrderer",
    "SqlAnalyzer.Tests")]

// Test framework configuration
[assembly: TestFramework(
    "SqlAnalyzer.Tests.TestFramework.CustomTestFramework",
    "SqlAnalyzer.Tests")]