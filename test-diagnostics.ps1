# PowerShell script to run tests with detailed diagnostics and timing

$ErrorActionPreference = "Continue"
$startTime = Get-Date

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "SQL Analyzer - Test Diagnostics Runner" -ForegroundColor Cyan
Write-Host "Started at: $startTime" -ForegroundColor Yellow
Write-Host "========================================`n" -ForegroundColor Cyan

# Function to measure and report execution time
function Measure-TestExecution {
    param(
        [string]$TestName,
        [scriptblock]$TestCommand
    )
    
    Write-Host "`n--- Running: $TestName ---" -ForegroundColor Green
    $testStart = Get-Date
    
    try {
        & $TestCommand
        $success = $LASTEXITCODE -eq 0
    }
    catch {
        Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
        $success = $false
    }
    
    $testEnd = Get-Date
    $duration = ($testEnd - $testStart).TotalSeconds
    
    if ($success) {
        Write-Host "✓ Completed in $duration seconds" -ForegroundColor Green
    } else {
        Write-Host "✗ Failed after $duration seconds" -ForegroundColor Red
    }
    
    return @{
        Name = $TestName
        Success = $success
        Duration = $duration
        StartTime = $testStart
        EndTime = $testEnd
    }
}

# Collect system information
Write-Host "System Information:" -ForegroundColor Yellow
Write-Host "  OS: $([System.Environment]::OSVersion.VersionString)"
Write-Host "  .NET SDKs installed:"
try {
    dotnet --list-sdks | ForEach-Object { Write-Host "    $_" }
} catch {
    Write-Host "    ERROR: dotnet CLI not found" -ForegroundColor Red
}

# Check prerequisites
Write-Host "`nChecking prerequisites..." -ForegroundColor Yellow

# Check if .env exists
if (Test-Path ".env") {
    Write-Host "  ✓ .env file found" -ForegroundColor Green
    
    # Load and validate environment variables
    $envContent = Get-Content ".env" | Where-Object { $_ -notmatch '^\s*#' -and $_ -match '=' }
    foreach ($line in $envContent) {
        $name, $value = $line -split '=', 2
        [Environment]::SetEnvironmentVariable($name.Trim(), $value.Trim(), 'Process')
    }
    
    $runTests = [Environment]::GetEnvironmentVariable("RUN_INTEGRATION_TESTS", 'Process')
    if ($runTests -eq "true") {
        Write-Host "  ✓ Integration tests enabled" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ Integration tests disabled (RUN_INTEGRATION_TESTS != true)" -ForegroundColor Yellow
    }
    
    $connStr = [Environment]::GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION", 'Process')
    if ($connStr) {
        Write-Host "  ✓ SQL Server connection string found" -ForegroundColor Green
        # Test connection
        Write-Host "  Testing database connection..." -ForegroundColor Yellow
        # We'll test this in the actual tests
    } else {
        Write-Host "  ✗ SQL Server connection string not found" -ForegroundColor Red
    }
} else {
    Write-Host "  ✗ .env file not found" -ForegroundColor Red
}

# Restore packages
Write-Host "`nRestoring NuGet packages..." -ForegroundColor Yellow
$restoreResult = Measure-TestExecution -TestName "Package Restore" -TestCommand {
    dotnet restore --verbosity minimal
}

# Build solution
Write-Host "`nBuilding solution..." -ForegroundColor Yellow
$buildResult = Measure-TestExecution -TestName "Build Solution" -TestCommand {
    dotnet build --no-restore --configuration Release --verbosity minimal
}

# Run different test categories
$testResults = @()

# Unit Tests
Write-Host "`n`n=== UNIT TESTS ===" -ForegroundColor Magenta
$unitTestResult = Measure-TestExecution -TestName "Unit Tests" -TestCommand {
    dotnet test --no-build --configuration Release `
        --filter "FullyQualifiedName!~IntegrationTests" `
        --logger "console;verbosity=normal" `
        --collect:"XPlat Code Coverage" `
        --results-directory "./TestResults/UnitTests"
}
$testResults += $unitTestResult

# Integration Tests - if enabled
if ($runTests -eq "true") {
    Write-Host "`n`n=== INTEGRATION TESTS ===" -ForegroundColor Magenta
    
    # Individual integration test classes for detailed timing
    $integrationTestClasses = @(
        "SqlServerConnectionIntegrationTests",
        "TableAnalyzerIntegrationTests",
        "ConnectionFactoryIntegrationTests",
        "AnalysisReportGeneratorTests"
    )
    
    foreach ($testClass in $integrationTestClasses) {
        $result = Measure-TestExecution -TestName "Integration: $testClass" -TestCommand {
            dotnet test --no-build --configuration Release `
                --filter "FullyQualifiedName~IntegrationTests.$testClass" `
                --logger "trx;LogFileName=$testClass.trx" `
                --results-directory "./TestResults/IntegrationTests"
        }
        $testResults += $result
    }
}

# Performance test - measure analyzer performance
Write-Host "`n`n=== PERFORMANCE ANALYSIS ===" -ForegroundColor Magenta
if ($runTests -eq "true") {
    Write-Host "Running performance analysis on TableAnalyzer..." -ForegroundColor Yellow
    $perfResult = Measure-TestExecution -TestName "Performance: TableAnalyzer" -TestCommand {
        dotnet test --no-build --configuration Release `
            --filter "FullyQualifiedName~TableAnalyzerIntegrationTests.AnalyzeAsync_PerformanceTest" `
            --logger "console;verbosity=detailed"
    }
    $testResults += $perfResult
}

# Generate summary report
$endTime = Get-Date
$totalDuration = ($endTime - $startTime).TotalSeconds

Write-Host "`n`n========================================" -ForegroundColor Cyan
Write-Host "TEST EXECUTION SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Total Duration: $totalDuration seconds" -ForegroundColor Yellow
Write-Host "Completed at: $endTime" -ForegroundColor Yellow

Write-Host "`nTest Results:" -ForegroundColor Yellow
$successCount = 0
$failureCount = 0
$totalTestTime = 0

foreach ($result in $testResults) {
    $status = if ($result.Success) { "PASS" } else { "FAIL" }
    $color = if ($result.Success) { "Green" } else { "Red" }
    $totalTestTime += $result.Duration
    
    if ($result.Success) { $successCount++ } else { $failureCount++ }
    
    Write-Host ("  {0,-40} {1,6} ({2,6:F2}s)" -f $result.Name, $status, $result.Duration) -ForegroundColor $color
}

Write-Host "`nSummary:" -ForegroundColor Yellow
Write-Host "  Total Tests Run: $($testResults.Count)"
Write-Host "  Passed: $successCount" -ForegroundColor Green
Write-Host "  Failed: $failureCount" -ForegroundColor Red
Write-Host "  Total Test Time: $($totalTestTime)s"
Write-Host "  Overhead Time: $($totalDuration - $totalTestTime)s"

# Check for test output files
Write-Host "`nGenerated Files:" -ForegroundColor Yellow
if (Test-Path "./TestResults") {
    $testFiles = Get-ChildItem -Path "./TestResults" -Recurse -File | Where-Object { $_.LastWriteTime -gt $startTime }
    foreach ($file in $testFiles) {
        Write-Host "  - $($file.FullName.Replace($PWD, '.'))"
    }
}

# Look for any warning or error patterns in output
Write-Host "`nChecking for common issues..." -ForegroundColor Yellow

# Check for timeout warnings
$logFiles = Get-ChildItem -Path "./TestResults" -Filter "*.trx" -Recurse -ErrorAction SilentlyContinue
$timeoutCount = 0
$connectionFailures = 0

foreach ($logFile in $logFiles) {
    $content = Get-Content $logFile.FullName -Raw
    if ($content -match "timeout") {
        $timeoutCount++
    }
    if ($content -match "connection.*failed|unable.*connect") {
        $connectionFailures++
    }
}

if ($timeoutCount -gt 0) {
    Write-Host "  ⚠ Found $timeoutCount timeout issues" -ForegroundColor Yellow
}
if ($connectionFailures -gt 0) {
    Write-Host "  ⚠ Found $connectionFailures connection failures" -ForegroundColor Yellow
}

# Save diagnostic report
$reportPath = "./TestResults/diagnostic-report-$(Get-Date -Format 'yyyyMMdd-HHmmss').txt"
$report = @"
SQL Analyzer Test Diagnostics Report
====================================
Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
Total Duration: $totalDuration seconds

System Information:
- OS: $([System.Environment]::OSVersion.VersionString)
- Machine: $env:COMPUTERNAME
- User: $env:USERNAME

Test Execution Summary:
- Total Tests: $($testResults.Count)
- Passed: $successCount
- Failed: $failureCount
- Total Test Time: $totalTestTime seconds

Individual Test Results:
$($testResults | ForEach-Object { "- $($_.Name): $($_.Duration)s - $(if($_.Success){'PASS'}else{'FAIL'})" } | Out-String)

Issues Detected:
- Timeouts: $timeoutCount
- Connection Failures: $connectionFailures

Environment Configuration:
- Integration Tests Enabled: $($runTests -eq 'true')
- Connection String Present: $($null -ne $connStr)
"@

$report | Out-File -FilePath $reportPath
Write-Host "`nDiagnostic report saved to: $reportPath" -ForegroundColor Green

# Exit with appropriate code
if ($failureCount -gt 0) {
    Write-Host "`nTEST RUN FAILED" -ForegroundColor Red
    exit 1
} else {
    Write-Host "`nTEST RUN SUCCEEDED" -ForegroundColor Green
    exit 0
}