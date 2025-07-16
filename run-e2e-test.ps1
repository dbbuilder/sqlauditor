Write-Host "SQL Analyzer - End-to-End Test Runner" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""

# Set environment variables
Write-Host "Setting up test environment..." -ForegroundColor Yellow
$env:SQLSERVER_TEST_CONNECTION = "Server=sqltest.schoolvision.net,14333;Database=SVDB_CaptureT;User Id=sv;Password=Gv51076!;TrustServerCertificate=true;"
$env:RUN_INTEGRATION_TESTS = "true"
$env:RUN_EXTERNAL_TESTS = "true"
$env:TEST_ENVIRONMENT = "E2E"

Write-Host "Environment configured:" -ForegroundColor Green
Write-Host "  - SQL Server: sqltest.schoolvision.net,14333" -ForegroundColor Cyan
Write-Host "  - Database: SVDB_CaptureT" -ForegroundColor Cyan
Write-Host "  - Integration Tests: Enabled" -ForegroundColor Cyan
Write-Host "  - External Tests: Enabled" -ForegroundColor Cyan
Write-Host ""

# Build the solution first
Write-Host "Building solution..." -ForegroundColor Yellow
$buildResult = dotnet build SqlAnalyzer.sln --configuration Release 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    Write-Host $buildResult | Out-String
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green
Write-Host ""

# Create a minimal E2E test that doesn't rely on test framework
Write-Host "Creating simplified E2E test..." -ForegroundColor Yellow

$e2eTestCode = @'
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlAnalyzer.Core.Analyzers;
using SqlAnalyzer.Core.Caching;
using SqlAnalyzer.Core.Configuration;
using SqlAnalyzer.Core.Connections;
using SqlAnalyzer.Core.Optimization;
using SqlAnalyzer.Core.Resilience;
using SqlAnalyzer.Core.Security;

class E2ETestProgram
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=== SQL Analyzer End-to-End Test ===");
        Console.WriteLine($"Started at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();

        var stopwatch = Stopwatch.StartNew();
        var testsPassed = 0;
        var testsFailed = 0;

        try
        {
            // Setup services
            var services = new ServiceCollection();
            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();

            // Run tests
            await RunTest("Security Validation", async () => await TestSecurityValidation(serviceProvider), ref testsPassed, ref testsFailed);
            await RunTest("Connection Management", async () => await TestConnectionManagement(serviceProvider), ref testsPassed, ref testsFailed);
            await RunTest("Query Optimization", async () => await TestQueryOptimization(serviceProvider), ref testsPassed, ref testsFailed);
            await RunTest("Database Analysis", async () => await TestDatabaseAnalysis(serviceProvider), ref testsPassed, ref testsFailed);
            await RunTest("Caching", async () => await TestCaching(serviceProvider), ref testsPassed, ref testsFailed);
            await RunTest("Circuit Breaker", async () => await TestCircuitBreaker(serviceProvider), ref testsPassed, ref testsFailed);
            await RunTest("Connection Pool", async () => await TestConnectionPool(serviceProvider), ref testsPassed, ref testsFailed);
            await RunTest("Adaptive Timeout", async () => await TestAdaptiveTimeout(serviceProvider), ref testsPassed, ref testsFailed);

            stopwatch.Stop();
            Console.WriteLine();
            Console.WriteLine("=== Test Summary ===");
            Console.WriteLine($"Total tests: {testsPassed + testsFailed}");
            Console.WriteLine($"Passed: {testsPassed}");
            Console.WriteLine($"Failed: {testsFailed}");
            Console.WriteLine($"Duration: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
            Console.WriteLine();

            return testsFailed > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    static async Task RunTest(string testName, Func<Task> test, ref int passed, ref int failed)
    {
        Console.Write($"Running {testName}... ");
        try
        {
            await test();
            Console.WriteLine("[PASSED]");
            passed++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FAILED] - {ex.Message}");
            failed++;
        }
    }

    static async Task TestSecurityValidation(IServiceProvider services)
    {
        var validator = services.GetRequiredService<IConnectionStringValidator>();
        
        var result = validator.Validate("Server=localhost;Database=test;User Id=sa;Password=Str0ng!P@ssw0rd#2024;");
        if (!result.IsValid)
            throw new Exception("Security validation failed");
            
        Console.Write($"(Security Level: {result.SecurityLevel}) ");
    }

    static async Task TestConnectionManagement(IServiceProvider services)
    {
        var factory = services.GetRequiredService<IConnectionFactory>();
        var connectionString = Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION");
        
        using var connection = factory.CreateConnection(connectionString, DatabaseType.SqlServer);
        await connection.OpenAsync();
        
        var dbName = await connection.ExecuteScalarAsync("SELECT DB_NAME()");
        Console.Write($"(Connected to: {dbName}) ");
    }

    static async Task TestQueryOptimization(IServiceProvider services)
    {
        var optimizer = services.GetRequiredService<IQueryOptimizer>();
        
        var query = "SELECT * FROM sys.tables";
        var optimized = optimizer.AddNoLockHint(query, DatabaseType.SqlServer);
        
        if (!optimized.Contains("WITH (NOLOCK)"))
            throw new Exception("Query optimization failed");
            
        Console.Write("(NOLOCK added) ");
    }

    static async Task TestDatabaseAnalysis(IServiceProvider services)
    {
        var factory = services.GetRequiredService<IConnectionFactory>();
        var connectionString = Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION");
        
        using var connection = factory.CreateConnection(connectionString, DatabaseType.SqlServer);
        await connection.OpenAsync();
        
        var logger = services.GetRequiredService<ILogger<TableAnalyzer>>();
        var analyzer = new TableAnalyzer(connection, logger);
        var result = await analyzer.AnalyzeAsync();
        
        if (!result.Success)
            throw new Exception($"Analysis failed: {result.ErrorMessage}");
            
        Console.Write($"(Analyzed {result.Summary.TotalObjectsAnalyzed} tables) ");
    }

    static async Task TestCaching(IServiceProvider services)
    {
        var cache = services.GetRequiredService<IQueryCache>();
        
        var key = "test_key_" + Guid.NewGuid();
        var value = new { Name = "Test", Value = 123 };
        
        await cache.SetAsync(key, value);
        var result = await cache.GetAsync<object>(key);
        
        if (!result.IsHit)
            throw new Exception("Cache miss on stored value");
            
        Console.Write("(Cache hit verified) ");
    }

    static async Task TestCircuitBreaker(IServiceProvider services)
    {
        var factory = services.GetRequiredService<ICircuitBreakerFactory>();
        var cb = factory.GetOrCreateCircuitBreaker("test");
        
        // Test successful execution
        var result = await cb.ExecuteAsync(async () => { 
            await Task.Delay(10); 
            return "success"; 
        });
        
        if (result != "success")
            throw new Exception("Circuit breaker execution failed");
            
        Console.Write($"(State: {cb.State}) ");
    }

    static async Task TestConnectionPool(IServiceProvider services)
    {
        var poolManager = services.GetRequiredService<IConnectionPoolManager>();
        var connectionString = Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION");
        
        var settings = new ConnectionPoolSettings
        {
            MinPoolSize = 5,
            MaxPoolSize = 20,
            PoolingEnabled = true
        };
        
        var configured = poolManager.ConfigurePool(connectionString, settings, DatabaseType.SqlServer);
        if (!configured.Contains("Min Pool Size=5"))
            throw new Exception("Pool configuration failed");
            
        Console.Write("(Pool configured) ");
    }

    static async Task TestAdaptiveTimeout(IServiceProvider services)
    {
        var calculator = services.GetRequiredService<IAdaptiveTimeoutCalculator>();
        
        var timeout = calculator.CalculateTimeout(1000); // 1GB
        if (timeout <= 0)
            throw new Exception("Invalid timeout calculation");
            
        Console.Write($"(Timeout: {timeout}s for 1GB) ");
    }

    static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IConnectionFactory, ConnectionFactory>();
        services.AddSingleton<IConnectionStringValidator, ConnectionStringValidator>();
        services.AddSingleton<IQueryOptimizer, QueryOptimizer>();
        services.AddSingleton<IAdaptiveTimeoutCalculator, AdaptiveTimeoutCalculator>();
        services.AddSingleton<IQueryCache, MemoryQueryCache>();
        services.AddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>();
        services.AddSingleton<ICircuitBreakerFactory, CircuitBreakerFactory>();
        services.AddSingleton<IConnectionPoolManager, ConnectionPoolManager>();
        services.AddSingleton<IConfigurationProviderFactory, ConfigurationProviderFactory>();
    }
}
'@

# Create project for E2E test
$projectContent = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="src\SqlAnalyzer.Core\SqlAnalyzer.Core.csproj" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="8.0.0" />
  </ItemGroup>
</Project>
'@

# Save files
$e2eTestCode | Out-File -FilePath "E2ETest.cs" -Encoding UTF8
$projectContent | Out-File -FilePath "E2ETest.csproj" -Encoding UTF8

Write-Host "Building E2E test..." -ForegroundColor Yellow
dotnet build E2ETest.csproj --configuration Release --no-restore

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Running E2E test..." -ForegroundColor Green
    Write-Host "===================" -ForegroundColor Green
    Write-Host ""
    
    dotnet run --project E2ETest.csproj --no-build
    $testResult = $LASTEXITCODE
    
    # Cleanup
    Remove-Item "E2ETest.cs" -ErrorAction SilentlyContinue
    Remove-Item "E2ETest.csproj" -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force "bin" -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force "obj" -ErrorAction SilentlyContinue
    
    if ($testResult -eq 0) {
        Write-Host ""
        Write-Host "All E2E tests passed!" -ForegroundColor Green
        Write-Host ""
    } else {
        Write-Host ""
        Write-Host "Some E2E tests failed!" -ForegroundColor Red
        Write-Host ""
        exit 1
    }
} else {
    Write-Host "E2E test build failed!" -ForegroundColor Red
    
    # Cleanup
    Remove-Item "E2ETest.cs" -ErrorAction SilentlyContinue
    Remove-Item "E2ETest.csproj" -ErrorAction SilentlyContinue
    
    exit 1
}

Write-Host "E2E test execution complete!" -ForegroundColor Green