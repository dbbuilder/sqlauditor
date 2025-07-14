using Xunit;

namespace SqlAnalyzer.Tests.TestFramework
{
    /// <summary>
    /// Examples of using test categories and traits
    /// </summary>
    public class CategoryExamples
    {
        [Fact]
        [Trait("Category", TestCategories.Unit)]
        [Trait(TestTraits.Priority.TraitName, TestTraits.Priority.High)]
        [Trait(TestTraits.Feature.TraitName, TestTraits.Feature.ConnectionManagement)]
        public void Example_UnitTest()
        {
            // This is a high-priority unit test for connection management
            Assert.True(true);
        }

        [Fact]
        [Trait("Category", TestCategories.Integration)]
        [Trait("Category", TestCategories.SqlServer)]
        [Trait("Category", TestCategories.External)]
        [Trait(TestTraits.Environment.TraitName, TestTraits.Environment.CI)]
        public void Example_IntegrationTest()
        {
            // This integration test requires SQL Server and runs in CI environment
            Assert.True(true);
        }

        [Fact]
        [Trait("Category", TestCategories.Performance)]
        [Trait("Category", TestCategories.Slow)]
        [Trait(TestTraits.Owner.TraitName, TestTraits.Owner.Performance)]
        public void Example_PerformanceTest()
        {
            // This is a slow performance test owned by the performance team
            Assert.True(true);
        }

        [Theory]
        [InlineData("test1")]
        [InlineData("test2")]
        [Trait("Category", TestCategories.Unit)]
        [Trait("Category", TestCategories.Smoke)]
        [Trait(TestTraits.Priority.TraitName, TestTraits.Priority.Critical)]
        public void Example_SmokeTest(string testData)
        {
            // This is a critical smoke test with multiple test cases
            Assert.NotNull(testData);
        }

        [Fact(Skip = "Requires database connection")]
        [Trait("Category", TestCategories.E2E)]
        [Trait("Category", TestCategories.External)]
        [Trait("Category", TestCategories.Flaky)]
        [Retry(3)] // Using our custom retry attribute
        public void Example_FlakyE2ETest()
        {
            // This is a flaky E2E test that might need retries
            Assert.True(true);
        }
    }

    /// <summary>
    /// Collection fixture for tests that share expensive setup
    /// </summary>
    [CollectionDefinition("Database Collection")]
    public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    /// <summary>
    /// Example database fixture for shared test context
    /// </summary>
    public class DatabaseFixture : IDisposable
    {
        public string ConnectionString { get; private set; }

        public DatabaseFixture()
        {
            // Initialize shared database connection
            ConnectionString = "Server=test;Database=test;";
        }

        public void Dispose()
        {
            // Cleanup shared resources
        }
    }

    /// <summary>
    /// Example of tests using shared collection fixture
    /// </summary>
    [Collection("Database Collection")]
    [Trait("Category", TestCategories.Integration)]
    public class DatabaseTests
    {
        private readonly DatabaseFixture _fixture;

        public DatabaseTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        [Trait(TestTraits.Feature.TraitName, TestTraits.Feature.DatabaseAnalysis)]
        public void TestWithSharedDatabase()
        {
            // Use shared database connection
            Assert.NotNull(_fixture.ConnectionString);
        }
    }
}