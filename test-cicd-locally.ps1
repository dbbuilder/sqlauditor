# Local CI/CD Testing Script
# This simulates the GitHub Actions workflow locally

param(
    [string]$RunNumber = "99",
    [switch]$SkipBuild,
    [switch]$SkipTests
)

Write-Host "🔧 SQL Analyzer - Local CI/CD Test" -ForegroundColor Cyan
Write-Host "===================================" -ForegroundColor Cyan

# Simulate GitHub Actions environment
$env:GITHUB_SHA = (git rev-parse HEAD 2>$null) ?? "local-test-sha"
$env:GITHUB_RUN_NUMBER = $RunNumber
$env:GITHUB_REF_NAME = (git branch --show-current 2>$null) ?? "local"

# Generate version info
$VersionDate = Get-Date -Format "yyyy.MM.dd"
$Version = "$VersionDate.$RunNumber"
$ShortSha = $env:GITHUB_SHA.Substring(0, [Math]::Min(7, $env:GITHUB_SHA.Length))
$DeploymentId = "$RunNumber-$ShortSha"
$BuildTimestamp = Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ"

Write-Host "`n📋 Version Information:" -ForegroundColor Yellow
Write-Host "  Version: $Version" -ForegroundColor Green
Write-Host "  Commit: $ShortSha" -ForegroundColor Green
Write-Host "  Deployment ID: $DeploymentId" -ForegroundColor Green
Write-Host "  Build Timestamp: $BuildTimestamp" -ForegroundColor Green

# Set environment variables for the build
$env:BUILD_TIMESTAMP = $BuildTimestamp
$env:DEPLOYMENT_ID = $DeploymentId
$env:VERSION = $Version

if (-not $SkipBuild) {
    Write-Host "`n🔨 Building API..." -ForegroundColor Yellow
    
    # Build with version info
    dotnet build src/SqlAnalyzer.Api/SqlAnalyzer.Api.csproj `
        --configuration Release `
        -p:Version=$Version `
        -p:AssemblyVersion=$Version `
        -p:FileVersion=$Version `
        -p:InformationalVersion="$Version+$ShortSha"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Build successful!" -ForegroundColor Green
}

if (-not $SkipTests) {
    Write-Host "`n🧪 Running tests..." -ForegroundColor Yellow
    dotnet test --no-restore --verbosity normal
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Tests failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Tests passed!" -ForegroundColor Green
}

Write-Host "`n📦 Publishing API..." -ForegroundColor Yellow
dotnet publish src/SqlAnalyzer.Api/SqlAnalyzer.Api.csproj `
    -c Release `
    -o ./publish/test `
    -p:Version=$Version `
    -p:InformationalVersion="$Version+$ShortSha"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Publish failed!" -ForegroundColor Red
    exit 1
}

# Create version.json file
$versionJson = @{
    version = $Version
    commit = $ShortSha
    buildNumber = $RunNumber
    deploymentId = $DeploymentId
    timestamp = $BuildTimestamp
    branch = $env:GITHUB_REF_NAME
    repository = "sqlauditor"
} | ConvertTo-Json -Depth 10

$versionJson | Out-File -FilePath "./publish/test/version.json" -Encoding UTF8
Write-Host "✅ Created version.json" -ForegroundColor Green

# Run the API locally to test
Write-Host "`n🚀 Starting API locally for testing..." -ForegroundColor Yellow
$apiProcess = Start-Process -FilePath "dotnet" `
    -ArgumentList "run", "--project", "src/SqlAnalyzer.Api/SqlAnalyzer.Api.csproj", "--", "--urls=http://localhost:5234" `
    -PassThru `
    -NoNewWindow

Start-Sleep -Seconds 5

# Test the version endpoint
Write-Host "`n🔍 Testing version endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5234/api/version" -Method Get
    Write-Host "API Response:" -ForegroundColor Cyan
    $response | ConvertTo-Json -Depth 10 | Write-Host
    
    # Verify version matches
    if ($response.deployment.deploymentId -eq $DeploymentId) {
        Write-Host "`n✅ Version verification passed!" -ForegroundColor Green
    } else {
        Write-Host "`n❌ Version verification failed!" -ForegroundColor Red
        Write-Host "Expected: $DeploymentId" -ForegroundColor Red
        Write-Host "Got: $($response.deployment.deploymentId)" -ForegroundColor Red
    }
    
    # Test health endpoint
    Write-Host "`n🏥 Testing health endpoint..." -ForegroundColor Yellow
    $health = Invoke-RestMethod -Uri "http://localhost:5234/api/version/health" -Method Get
    Write-Host "Health Status: $($health.status)" -ForegroundColor Green
    
} catch {
    Write-Host "❌ Failed to test endpoints: $_" -ForegroundColor Red
} finally {
    # Stop the API
    if ($apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
        Write-Host "`n🛑 API process stopped" -ForegroundColor Yellow
    }
}

Write-Host "`n📊 Test Summary:" -ForegroundColor Cyan
Write-Host "=================" -ForegroundColor Cyan
Write-Host "Version: $Version" -ForegroundColor Green
Write-Host "Deployment ID: $DeploymentId" -ForegroundColor Green
Write-Host "Output: ./publish/test" -ForegroundColor Green

Write-Host "`n💡 Next Steps:" -ForegroundColor Yellow
Write-Host "1. Review the test results above" -ForegroundColor White
Write-Host "2. If everything looks good, commit and push to trigger GitHub Actions" -ForegroundColor White
Write-Host "3. Monitor the deployment at: https://github.com/[your-repo]/actions" -ForegroundColor White