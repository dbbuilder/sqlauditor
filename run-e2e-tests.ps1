# SQL Analyzer E2E Test Runner
param(
    [string]$TestType = "all",  # all, api, web
    [switch]$Verbose,
    [switch]$KeepContainers
)

Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║            SQL ANALYZER E2E TEST RUNNER                      ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

$startTime = Get-Date
$exitCode = 0

# Function to check if Docker is running
function Test-DockerRunning {
    try {
        docker version | Out-Null
        return $true
    }
    catch {
        Write-Host "ERROR: Docker is not running or not installed" -ForegroundColor Red
        Write-Host "Please start Docker Desktop and try again" -ForegroundColor Yellow
        return $false
    }
}

# Function to check if ports are available
function Test-PortAvailable {
    param([int]$Port)
    
    try {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Any, $Port)
        $listener.Start()
        $listener.Stop()
        return $true
    }
    catch {
        return $false
    }
}

# Function to cleanup containers
function Remove-TestContainers {
    Write-Host "Cleaning up test containers..." -ForegroundColor Yellow
    docker ps -a --filter "name=sqlanalyzer-test-" --format "{{.Names}}" | ForEach-Object {
        docker rm -f $_ 2>$null
    }
}

# Check prerequisites
Write-Host "▶ CHECKING PREREQUISITES" -ForegroundColor Yellow

if (-not (Test-DockerRunning)) {
    exit 1
}

# Check critical ports
$criticalPorts = @(15500, 15525, 15565)
$portsInUse = @()

foreach ($port in $criticalPorts) {
    if (-not (Test-PortAvailable $port)) {
        $portsInUse += $port
    }
}

if ($portsInUse.Count -gt 0) {
    Write-Host "ERROR: The following ports are in use: $($portsInUse -join ', ')" -ForegroundColor Red
    Write-Host "Please free these ports and try again" -ForegroundColor Yellow
    exit 1
}

Write-Host "  ✓ Docker is running" -ForegroundColor Green
Write-Host "  ✓ Required ports are available" -ForegroundColor Green

# Cleanup any existing test containers
if (-not $KeepContainers) {
    Remove-TestContainers
}

# Run API E2E Tests
if ($TestType -eq "all" -or $TestType -eq "api") {
    Write-Host ""
    Write-Host "▶ RUNNING API E2E TESTS" -ForegroundColor Yellow
    Write-Host "  Using ports: 15500-15540" -ForegroundColor Gray
    
    try {
        Push-Location "tests/SqlAnalyzer.Api.Tests.E2E"
        
        # Restore packages
        Write-Host "  Restoring packages..." -ForegroundColor Gray
        dotnet restore
        
        # Build
        Write-Host "  Building test project..." -ForegroundColor Gray
        dotnet build --no-restore
        
        # Run tests
        Write-Host "  Running tests..." -ForegroundColor Gray
        if ($Verbose) {
            dotnet test --no-build --logger "console;verbosity=detailed" `
                        --logger "html;LogFileName=api-test-results.html" `
                        --results-directory ./TestResults
        }
        else {
            dotnet test --no-build --logger "console;verbosity=normal" `
                        --logger "html;LogFileName=api-test-results.html" `
                        --results-directory ./TestResults
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ API E2E tests passed" -ForegroundColor Green
        }
        else {
            Write-Host "  ✗ API E2E tests failed" -ForegroundColor Red
            $exitCode = 1
        }
    }
    catch {
        Write-Host "  ✗ API E2E tests failed with error: $_" -ForegroundColor Red
        $exitCode = 1
    }
    finally {
        Pop-Location
    }
}

# Run Web UI E2E Tests
if ($TestType -eq "all" -or $TestType -eq "web") {
    Write-Host ""
    Write-Host "▶ RUNNING WEB UI E2E TESTS" -ForegroundColor Yellow
    Write-Host "  Using ports: 15561-15580" -ForegroundColor Gray
    
    try {
        Push-Location "tests/SqlAnalyzer.Web.Tests.E2E"
        
        # Install dependencies
        Write-Host "  Installing npm packages..." -ForegroundColor Gray
        npm install
        
        # Install Playwright browsers if needed
        Write-Host "  Installing Playwright browsers..." -ForegroundColor Gray
        npx playwright install
        
        # Run tests
        Write-Host "  Running tests..." -ForegroundColor Gray
        if ($Verbose) {
            npm run test -- --reporter=list,html
        }
        else {
            npm run test
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ Web UI E2E tests passed" -ForegroundColor Green
        }
        else {
            Write-Host "  ✗ Web UI E2E tests failed" -ForegroundColor Red
            $exitCode = 1
        }
    }
    catch {
        Write-Host "  ✗ Web UI E2E tests failed with error: $_" -ForegroundColor Red
        $exitCode = 1
    }
    finally {
        Pop-Location
    }
}

# Cleanup
if (-not $KeepContainers) {
    Remove-TestContainers
}

# Summary
$duration = (Get-Date) - $startTime
Write-Host ""
Write-Host "▶ E2E TEST SUMMARY" -ForegroundColor Cyan
Write-Host "  Duration: $($duration.ToString('mm\:ss'))" -ForegroundColor White
Write-Host "  Test Type: $TestType" -ForegroundColor White

if ($exitCode -eq 0) {
    Write-Host "  Result: PASSED ✓" -ForegroundColor Green
}
else {
    Write-Host "  Result: FAILED ✗" -ForegroundColor Red
}

# Open test results if available
if (Test-Path "tests/SqlAnalyzer.Api.Tests.E2E/TestResults/api-test-results.html") {
    Write-Host ""
    Write-Host "API test results: tests/SqlAnalyzer.Api.Tests.E2E/TestResults/api-test-results.html" -ForegroundColor Gray
}

if (Test-Path "tests/SqlAnalyzer.Web.Tests.E2E/playwright-report/index.html") {
    Write-Host "Web UI test results: tests/SqlAnalyzer.Web.Tests.E2E/playwright-report/index.html" -ForegroundColor Gray
}

exit $exitCode