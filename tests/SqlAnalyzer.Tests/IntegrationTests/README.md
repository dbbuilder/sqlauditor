# SQL Analyzer Integration Tests

This directory contains integration tests that verify the SQL Analyzer functionality against real databases.

## ⚠️ Important: Read-Only Tests

**All integration tests are designed to be READ-ONLY and will not modify any data in your database.**

The tests only:
- Query system tables and views
- Read table metadata
- Analyze database structure
- Generate reports

## Setup

1. **Create a `.env` file** in the project root with your test database connection:

```env
# SQL Server Test Database Connection
SQLSERVER_TEST_CONNECTION=Server=your-server;Database=your-db;User Id=your-user;Password=your-password;TrustServerCertificate=true;

# Test Configuration
RUN_INTEGRATION_TESTS=true
TEST_TIMEOUT_SECONDS=30
```

2. **Ensure `.env` is git-ignored** (already configured in `.gitignore`)

## Running Tests

### Using Scripts

#### Windows (PowerShell):
```powershell
.\run-integration-tests.ps1
```

#### Linux/Mac (Bash):
```bash
./run-integration-tests.sh
```

### Using dotnet CLI

Run all integration tests:
```bash
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

Run specific test class:
```bash
dotnet test --filter "FullyQualifiedName~IntegrationTests.SqlServerConnectionIntegrationTests"
```

Run with coverage:
```bash
dotnet test --filter "FullyQualifiedName~IntegrationTests" --collect:"XPlat Code Coverage"
```

## Test Categories

### 1. Connection Tests (`SqlServerConnectionIntegrationTests`)
- Verifies database connectivity
- Tests query execution
- Validates connection pooling
- Ensures read-only operations

### 2. Analyzer Tests (`TableAnalyzerIntegrationTests`)
- Tests table analysis functionality
- Verifies finding detection
- Validates remediation scripts
- Performance benchmarks

### 3. Factory Tests (`ConnectionFactoryIntegrationTests`)
- Tests connection creation
- Validates auto-detection
- Verifies disposal patterns

### 4. Report Generation (`AnalysisReportGeneratorTests`)
- Generates comprehensive analysis reports
- Creates multiple output formats (JSON, HTML, Markdown, CSV)
- Provides database health overview

## Output

Test results and reports are saved to:
- `./TestResults/` - Test execution results and coverage
- `./TestResults/AnalysisReports/` - Generated analysis reports

## Troubleshooting

### Tests are skipped
- Ensure `RUN_INTEGRATION_TESTS=true` in `.env`
- Check that `.env` file exists in project root

### Connection failures
- Verify connection string in `.env`
- Ensure database server is accessible
- Check firewall rules
- Verify SQL Server authentication is enabled

### Timeout issues
- Increase `TEST_TIMEOUT_SECONDS` in `.env`
- Check network latency to database server

## Security Notes

- Never commit `.env` files to source control
- Use read-only database accounts for testing
- Consider using a dedicated test database
- Rotate credentials regularly

## Writing New Integration Tests

When adding new integration tests:

1. Extend `IntegrationTestBase` class
2. Use `CheckSkipIntegrationTest()` to respect configuration
3. Ensure all operations are read-only
4. Add appropriate logging with `ITestOutputHelper`
5. Handle connection disposal properly

Example:
```csharp
public class MyIntegrationTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;

    public MyIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task MyTest_ShouldWork()
    {
        // Arrange
        CheckSkipIntegrationTest();
        using var connection = CreateSqlServerConnection();
        
        // Act - Only read operations!
        var result = await connection.ExecuteQueryAsync("SELECT ...");
        
        // Assert
        result.Should().NotBeNull();
        _output.WriteLine($"Result: {result.Rows.Count} rows");
    }
}
```