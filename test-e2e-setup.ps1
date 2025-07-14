# Test E2E Setup Verification Script

Write-Host "SQL Analyzer E2E Test Setup Verification" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""

$hasErrors = $false

# Check .NET SDK
Write-Host "Checking .NET SDK..." -NoNewline
try {
    $dotnetVersion = dotnet --version
    Write-Host " ✓ Found .NET $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host " ✗ .NET SDK not found" -ForegroundColor Red
    $hasErrors = $true
}

# Check Node.js
Write-Host "Checking Node.js..." -NoNewline
try {
    $nodeVersion = node --version
    Write-Host " ✓ Found Node.js $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host " ✗ Node.js not found" -ForegroundColor Red
    $hasErrors = $true
}

# Check Docker
Write-Host "Checking Docker..." -NoNewline
try {
    docker --version | Out-Null
    Write-Host " ✓ Docker is installed" -ForegroundColor Green
} catch {
    Write-Host " ✗ Docker not found" -ForegroundColor Red
    $hasErrors = $true
}

# Check PowerShell version
Write-Host "Checking PowerShell..." -NoNewline
$psVersion = $PSVersionTable.PSVersion
Write-Host " ✓ PowerShell $($psVersion.Major).$($psVersion.Minor)" -ForegroundColor Green

# Check test projects
Write-Host ""
Write-Host "Checking test projects..." -ForegroundColor Yellow

$testProjects = @(
    @{Path = "tests/SqlAnalyzer.Api.Tests.E2E/SqlAnalyzer.Api.Tests.E2E.csproj"; Type = "API E2E Tests"},
    @{Path = "tests/SqlAnalyzer.Web.Tests.E2E/package.json"; Type = "Web UI E2E Tests"}
)

foreach ($project in $testProjects) {
    Write-Host "  $($project.Type)..." -NoNewline
    if (Test-Path $project.Path) {
        Write-Host " ✓" -ForegroundColor Green
    } else {
        Write-Host " ✗ Not found" -ForegroundColor Red
        $hasErrors = $true
    }
}

# Check ports
Write-Host ""
Write-Host "Checking port availability..." -ForegroundColor Yellow
$ports = @(15500, 15510, 15525, 15565)

foreach ($port in $ports) {
    Write-Host "  Port ${port}..." -NoNewline
    try {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Any, $port)
        $listener.Start()
        $listener.Stop()
        Write-Host " ✓ Available" -ForegroundColor Green
    } catch {
        Write-Host " ✗ In use" -ForegroundColor Red
    }
}

# Summary
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
if ($hasErrors) {
    Write-Host "  Some prerequisites are missing. Please install missing components." -ForegroundColor Yellow
} else {
    Write-Host "  All prerequisites are met. Ready to run E2E tests!" -ForegroundColor Green
}

Write-Host ""
Write-Host "To run E2E tests:" -ForegroundColor White
Write-Host "  API tests only:    .\run-e2e-tests.ps1 -TestType api" -ForegroundColor Gray
Write-Host "  Web UI tests only: .\run-e2e-tests.ps1 -TestType web" -ForegroundColor Gray
Write-Host "  All tests:         .\run-e2e-tests.ps1" -ForegroundColor Gray
Write-Host ""
Write-Host "For Docker-based testing:" -ForegroundColor White
Write-Host "  docker-compose -f docker-compose.e2e.yml up" -ForegroundColor Gray