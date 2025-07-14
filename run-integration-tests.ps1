# PowerShell script to run integration tests with SQL Server

Write-Host "SQL Analyzer - Integration Tests Runner" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green

# Check if .env file exists
if (-not (Test-Path ".env")) {
    Write-Host "ERROR: .env file not found!" -ForegroundColor Red
    Write-Host "Please create a .env file with your test database connection string." -ForegroundColor Yellow
    exit 1
}

# Load environment variables
Write-Host "`nLoading environment variables from .env file..." -ForegroundColor Yellow
$envContent = Get-Content ".env" | Where-Object { $_ -notmatch '^\s*#' -and $_ -match '=' }
foreach ($line in $envContent) {
    $name, $value = $line -split '=', 2
    [Environment]::SetEnvironmentVariable($name.Trim(), $value.Trim(), 'Process')
}

# Check if integration tests are enabled
$runTests = [Environment]::GetEnvironmentVariable("RUN_INTEGRATION_TESTS", 'Process')
if ($runTests -ne "true") {
    Write-Host "`nIntegration tests are disabled." -ForegroundColor Yellow
    Write-Host "Set RUN_INTEGRATION_TESTS=true in .env file to enable them." -ForegroundColor Yellow
    exit 0
}

# Display connection info (masked)
$connStr = [Environment]::GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION", 'Process')
if ($connStr) {
    $maskedConn = $connStr -replace 'Password=[^;]+', 'Password=****'
    Write-Host "`nUsing connection: $maskedConn" -ForegroundColor Cyan
}

# Run integration tests
Write-Host "`nRunning integration tests..." -ForegroundColor Green
Write-Host "Note: These tests are READ-ONLY and will not modify any data.`n" -ForegroundColor Yellow

# Run specific integration test classes
$testClasses = @(
    "SqlServerConnectionIntegrationTests",
    "TableAnalyzerIntegrationTests", 
    "ConnectionFactoryIntegrationTests"
)

foreach ($testClass in $testClasses) {
    Write-Host "`nRunning $testClass..." -ForegroundColor Cyan
    dotnet test --filter "FullyQualifiedName~IntegrationTests.$testClass" --logger "console;verbosity=normal"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Tests failed for $testClass!" -ForegroundColor Red
    }
}

# Run all integration tests with coverage
Write-Host "`n`nRunning all integration tests with code coverage..." -ForegroundColor Green
dotnet test --filter "FullyQualifiedName~IntegrationTests" --collect:"XPlat Code Coverage" --results-directory ./TestResults

Write-Host "`n`nIntegration tests completed!" -ForegroundColor Green
Write-Host "Check ./TestResults for coverage reports." -ForegroundColor Yellow