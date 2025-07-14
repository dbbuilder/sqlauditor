# SQL Analyzer E2E Testing Guide

This guide covers end-to-end testing for both the API and Web UI components of SQL Analyzer.

## Architecture

The E2E tests use ports 15500-15600 to avoid conflicts:
- **15500-15520**: API servers
- **15521-15540**: Test databases (SQL Server, PostgreSQL, MySQL)
- **15541-15560**: Mock services
- **15561-15580**: Web UI servers
- **15581-15600**: Redis and other services

## Prerequisites

1. **.NET 8.0 SDK**
   ```bash
   # Windows
   winget install Microsoft.DotNet.SDK.8
   
   # macOS
   brew install dotnet-sdk
   
   # Linux
   sudo apt-get install dotnet-sdk-8.0
   ```

2. **Node.js 18+**
   ```bash
   # Using nvm
   nvm install 18
   nvm use 18
   ```

3. **Docker Desktop**
   - Required for test database containers
   - Download from https://www.docker.com/products/docker-desktop

4. **PowerShell Core** (for Windows test scripts)
   ```bash
   winget install Microsoft.PowerShell
   ```

## Quick Start

### Running All Tests

```bash
# Windows PowerShell
.\run-e2e-tests.ps1

# Linux/macOS
./run-e2e-tests.sh
```

### Running Specific Test Suites

```bash
# API tests only
.\run-e2e-tests.ps1 -TestType api

# Web UI tests only
.\run-e2e-tests.ps1 -TestType web

# With verbose output
.\run-e2e-tests.ps1 -Verbose

# Keep containers after tests
.\run-e2e-tests.ps1 -KeepContainers
```

### Using Docker Compose

```bash
# Start all services for E2E testing
docker-compose -f docker-compose.e2e.yml up -d

# Run tests against Docker services
dotnet test tests/SqlAnalyzer.Api.Tests.E2E
npm test --prefix tests/SqlAnalyzer.Web.Tests.E2E

# Cleanup
docker-compose -f docker-compose.e2e.yml down
```

## API E2E Tests

### Test Structure

```
tests/SqlAnalyzer.Api.Tests.E2E/
├── Fixtures/
│   └── ApiTestFixture.cs      # WebApplicationFactory setup
├── Tests/
│   └── AnalysisApiE2ETests.cs # Test scenarios
└── TestConfiguration.cs       # Port management
```

### Test Scenarios

1. **Health Check**: Verify API is running
2. **Connection Testing**: Test database connections
3. **Analysis Workflow**: Complete analysis from start to finish
4. **Real-time Updates**: SignalR hub functionality
5. **Export Functionality**: PDF and JSON exports
6. **Error Handling**: Invalid inputs and error scenarios
7. **Concurrency**: Multiple simultaneous analyses

### Running Individual Tests

```bash
cd tests/SqlAnalyzer.Api.Tests.E2E

# Run all tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~HealthCheck"

# With detailed output
dotnet test --logger "console;verbosity=detailed"
```

## Web UI E2E Tests

### Test Structure

```
tests/SqlAnalyzer.Web.Tests.E2E/
├── tests/
│   ├── fixtures/
│   │   └── test-database.ts    # Test database setup
│   └── analysis-workflow.spec.ts # Test scenarios
├── playwright.config.ts         # Playwright configuration
└── package.json
```

### Test Scenarios

1. **Homepage Navigation**: Basic UI elements
2. **New Analysis Form**: Form validation and submission
3. **Connection Testing**: Database connection UI
4. **Analysis Progress**: Real-time progress updates
5. **Results Display**: Tabs and data visualization
6. **Export Functionality**: Download results
7. **History View**: Past analyses
8. **Responsive Design**: Mobile/tablet/desktop views

### Running Playwright Tests

```bash
cd tests/SqlAnalyzer.Web.Tests.E2E

# Install dependencies
npm install
npx playwright install

# Run all tests
npm test

# Run with UI mode
npm run test:ui

# Run specific browser
npm test -- --project=chromium

# Debug mode
npm run test:debug
```

## CI/CD Integration

### GitHub Actions

The repository includes a workflow for automated E2E testing:

```yaml
# .github/workflows/e2e-tests.yml
- Runs on push to main/develop
- Tests both API and Web UI
- Uploads test artifacts
- Generates test reports
```

### Running in CI

```bash
# Set environment variables
export CI=true
export GITHUB_ACTIONS=true

# Run tests
./run-e2e-tests.sh all
```

## Test Data Setup

### SQL Server Test Database

```sql
-- Created automatically by tests
CREATE DATABASE TestDB;
GO

USE TestDB;
GO

CREATE TABLE TestTable1 (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100),
    CreatedDate DATETIME DEFAULT GETDATE()
);

CREATE INDEX IX_TestTable1_Name ON TestTable1(Name);

-- Insert test data
INSERT INTO TestTable1 (Name) 
SELECT 'Test ' + CAST(number AS VARCHAR(10))
FROM master..spt_values 
WHERE type = 'P' AND number BETWEEN 1 AND 1000;
```

## Debugging Failed Tests

### API Test Failures

1. Check test output:
   ```bash
   cat tests/SqlAnalyzer.Api.Tests.E2E/TestResults/api-test-results.html
   ```

2. Enable detailed logging:
   ```bash
   dotnet test --logger "console;verbosity=diagnostic"
   ```

3. Check Docker containers:
   ```bash
   docker ps -a | grep sqlanalyzer-test
   docker logs sqlanalyzer-test-db
   ```

### Web UI Test Failures

1. View Playwright report:
   ```bash
   npx playwright show-report
   ```

2. Check screenshots/videos:
   ```
   tests/SqlAnalyzer.Web.Tests.E2E/test-results/
   ```

3. Run in headed mode:
   ```bash
   npm run test:headed
   ```

## Performance Considerations

- Tests use isolated database containers
- Parallel execution is enabled where possible
- Connection pooling is configured for efficiency
- Cleanup runs automatically after tests

## Troubleshooting

### Port Conflicts

```bash
# Check what's using a port
netstat -an | grep 15500

# Windows
netstat -an | findstr 15500

# Kill process using port (Linux/macOS)
lsof -ti:15500 | xargs kill -9
```

### Docker Issues

```bash
# Reset Docker containers
docker system prune -a

# Check Docker daemon
docker version
systemctl status docker  # Linux
```

### Test Timeouts

Increase timeouts in test configuration:
- API: `TestTimeoutSeconds` in `TestConfiguration.cs`
- Web UI: `timeout` in `playwright.config.ts`

## Best Practices

1. **Isolation**: Each test should be independent
2. **Cleanup**: Always clean up test data
3. **Assertions**: Use descriptive assertion messages
4. **Waits**: Use explicit waits, not sleep
5. **Logging**: Add context to test failures
6. **Screenshots**: Capture on failure for UI tests

## Contributing

When adding new E2E tests:

1. Use designated port ranges
2. Follow existing test patterns
3. Add to appropriate test suite
4. Update this documentation
5. Ensure CI passes

## Resources

- [xUnit Documentation](https://xunit.net/)
- [Playwright Documentation](https://playwright.dev/)
- [Testcontainers](https://www.testcontainers.org/)
- [ASP.NET Core Testing](https://docs.microsoft.com/en-us/aspnet/core/test/)