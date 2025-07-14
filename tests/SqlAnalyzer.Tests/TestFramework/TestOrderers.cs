using System;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SqlAnalyzer.Tests.TestFramework
{
    /// <summary>
    /// Orders test collections by priority
    /// </summary>
    public class PriorityTestCollectionOrderer : ITestCollectionOrderer
    {
        public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections)
        {
            // Order collections by name, prioritizing certain patterns
            return testCollections.OrderBy(collection =>
            {
                var name = collection.DisplayName;
                
                // Smoke tests first
                if (name.Contains("Smoke", StringComparison.OrdinalIgnoreCase))
                    return 0;
                    
                // Unit tests second
                if (name.Contains("Unit", StringComparison.OrdinalIgnoreCase))
                    return 1;
                    
                // Integration tests third
                if (name.Contains("Integration", StringComparison.OrdinalIgnoreCase))
                    return 2;
                    
                // E2E tests fourth
                if (name.Contains("E2E", StringComparison.OrdinalIgnoreCase))
                    return 3;
                    
                // Performance tests last
                if (name.Contains("Performance", StringComparison.OrdinalIgnoreCase))
                    return 4;
                    
                // Everything else in the middle
                return 2;
            })
            .ThenBy(collection => collection.DisplayName);
        }
    }

    /// <summary>
    /// Orders test cases by priority trait
    /// </summary>
    public class PriorityTestCaseOrderer : ITestCaseOrderer
    {
        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) 
            where TTestCase : ITestCase
        {
            var priorityOrder = new Dictionary<string, int>
            {
                { TestTraits.Priority.Critical, 0 },
                { TestTraits.Priority.High, 1 },
                { TestTraits.Priority.Medium, 2 },
                { TestTraits.Priority.Low, 3 }
            };

            return testCases.OrderBy(testCase =>
            {
                var priorityTrait = testCase.Traits
                    .FirstOrDefault(t => t.Key == TestTraits.Priority.TraitName);
                
                if (priorityTrait.Value?.Count > 0)
                {
                    var priority = priorityTrait.Value.First();
                    if (priorityOrder.TryGetValue(priority, out var order))
                        return order;
                }
                
                // Default to medium priority
                return 2;
            })
            .ThenBy(testCase => testCase.TestMethod.Method.Name);
        }
    }

    /// <summary>
    /// Custom test framework for advanced configuration
    /// </summary>
    public class CustomTestFramework : XunitTestFramework
    {
        public CustomTestFramework(IMessageSink messageSink) : base(messageSink)
        {
            // Configure test framework options
            messageSink.OnMessage(new DiagnosticMessage(
                "SqlAnalyzer Custom Test Framework initialized"));
        }

        protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
        {
            return new CustomTestFrameworkExecutor(
                assemblyName, 
                SourceInformationProvider, 
                DiagnosticMessageSink);
        }
    }

    /// <summary>
    /// Custom test framework executor
    /// </summary>
    public class CustomTestFrameworkExecutor : XunitTestFrameworkExecutor
    {
        public CustomTestFrameworkExecutor(
            AssemblyName assemblyName,
            ISourceInformationProvider sourceInformationProvider,
            IMessageSink diagnosticMessageSink)
            : base(assemblyName, sourceInformationProvider, diagnosticMessageSink)
        {
        }

        protected override async void RunTestCases(
            IEnumerable<IXunitTestCase> testCases,
            IMessageSink executionMessageSink,
            ITestFrameworkExecutionOptions executionOptions)
        {
            // Apply custom filtering based on environment
            var environment = Environment.GetEnvironmentVariable("TEST_ENVIRONMENT") ?? "Development";
            
            var filteredTestCases = testCases.Where(testCase =>
            {
                // Skip tests that require specific environments
                var envTrait = testCase.Traits
                    .FirstOrDefault(t => t.Key == TestTraits.Environment.TraitName);
                    
                if (envTrait.Value?.Count > 0)
                {
                    var requiredEnv = envTrait.Value.First();
                    if (requiredEnv != TestTraits.Environment.Any && 
                        requiredEnv != environment)
                    {
                        return false;
                    }
                }
                
                // Skip external tests if not enabled
                var runExternalTests = Environment.GetEnvironmentVariable("RUN_EXTERNAL_TESTS") == "true";
                if (!runExternalTests)
                {
                    var categories = testCase.Traits
                        .Where(t => t.Key == "Category")
                        .SelectMany(t => t.Value);
                        
                    if (categories.Contains(TestCategories.External))
                        return false;
                }
                
                return true;
            });

            base.RunTestCases(filteredTestCases, executionMessageSink, executionOptions);
        }
    }

    /// <summary>
    /// Test collection for non-parallelizable tests
    /// </summary>
    [CollectionDefinition("Non-Parallel Tests", DisableParallelization = true)]
    public class NonParallelTestCollection
    {
        // Tests in this collection will not run in parallel with each other
    }

    /// <summary>
    /// Example of non-parallel test class
    /// </summary>
    [Collection("Non-Parallel Tests")]
    public class NonParallelTestExample
    {
        [Fact]
        [Trait("Category", TestCategories.Integration)]
        public void Test_That_Cannot_Run_In_Parallel()
        {
            // This test will not run in parallel with other tests in the same collection
            Assert.True(true);
        }
    }
}