# Complete SQL Analyzer Deployment Script with Authentication
# This script builds, tests, and deploys the entire application

param(
    [string]$JwtSecret = "",
    [string]$AdminPassword = "SqlAnalyzer2024!",
    [switch]$SkipTests = $false
)

Write-Host "=== SQL Analyzer Complete Deployment ===" -ForegroundColor Cyan
Write-Host "Starting at: $(Get-Date)" -ForegroundColor Gray

# Check prerequisites
Write-Host "`nChecking prerequisites..." -ForegroundColor Yellow
$hasErrors = $false

# Check .NET SDK
if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "❌ .NET SDK not found. Please install from https://dotnet.microsoft.com/download" -ForegroundColor Red
    $hasErrors = $true
}

# Check Node.js
if (!(Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Node.js not found. Please install from https://nodejs.org/" -ForegroundColor Red
    $hasErrors = $true
}

# Check Azure CLI
if (!(Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Azure CLI not found. Please install from https://aka.ms/installazurecliwindows" -ForegroundColor Red
    $hasErrors = $true
}

if ($hasErrors) {
    Write-Host "`nPlease install missing prerequisites and run again." -ForegroundColor Red
    exit 1
}

Write-Host "✅ All prerequisites found" -ForegroundColor Green

# Load environment variables
if (Test-Path ".env.azure.local") {
    Write-Host "`nLoading Azure credentials..." -ForegroundColor Yellow
    Get-Content ".env.azure.local" | ForEach-Object {
        if ($_ -match '^(.+?)=(.+)$') {
            [System.Environment]::SetEnvironmentVariable($matches[1], $matches[2])
        }
    }
    Write-Host "✅ Azure credentials loaded" -ForegroundColor Green
}

# Generate JWT secret if not provided
if ([string]::IsNullOrEmpty($JwtSecret)) {
    Write-Host "`nGenerating secure JWT secret..." -ForegroundColor Yellow
    $bytes = New-Object byte[] 64
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $JwtSecret = [Convert]::ToBase64String($bytes)
    Write-Host "✅ JWT secret generated" -ForegroundColor Green
}

# Step 1: Build and test backend
Write-Host "`n=== Building Backend ===" -ForegroundColor Cyan
Set-Location -Path "src/SqlAnalyzer.Api"

Write-Host "Restoring packages..." -ForegroundColor Yellow
dotnet restore

Write-Host "Building API..." -ForegroundColor Yellow
dotnet build -c Release

if (!$SkipTests) {
    Write-Host "Running tests..." -ForegroundColor Yellow
    Set-Location -Path "../../tests/SqlAnalyzer.Tests"
    dotnet test --no-restore
    Set-Location -Path "../../src/SqlAnalyzer.Api"
}

Write-Host "Publishing API..." -ForegroundColor Yellow
dotnet publish -c Release -o ./publish

Write-Host "✅ Backend build complete" -ForegroundColor Green

# Step 2: Build frontend
Write-Host "`n=== Building Frontend ===" -ForegroundColor Cyan
Set-Location -Path "../SqlAnalyzer.Web"

Write-Host "Installing dependencies..." -ForegroundColor Yellow
npm install

Write-Host "Building Vue app..." -ForegroundColor Yellow
npm run build

Write-Host "✅ Frontend build complete" -ForegroundColor Green

# Step 3: Deploy to Azure
Write-Host "`n=== Deploying to Azure ===" -ForegroundColor Cyan
Set-Location -Path "../.."

# Login to Azure if needed
Write-Host "`nChecking Azure login..." -ForegroundColor Yellow
$account = az account show 2>$null | ConvertFrom-Json
if (!$account) {
    Write-Host "Please login to Azure..." -ForegroundColor Yellow
    az login
}

# Deploy API
Write-Host "`nDeploying API to Azure App Service..." -ForegroundColor Yellow
Set-Location -Path "src/SqlAnalyzer.Api/publish"

# Create ZIP for deployment
Write-Host "Creating deployment package..." -ForegroundColor Yellow
Compress-Archive -Path * -DestinationPath ../api-deploy.zip -Force

# Deploy using ZIP deploy
Write-Host "Deploying API..." -ForegroundColor Yellow
az webapp deployment source config-zip `
    --resource-group rg-sqlanalyzer `
    --name sqlanalyzer-api `
    --src ../api-deploy.zip

# Configure app settings
Write-Host "`nConfiguring API settings..." -ForegroundColor Yellow
az webapp config appsettings set `
    --name sqlanalyzer-api `
    --resource-group rg-sqlanalyzer `
    --settings `
    "Authentication__JwtSecret=$JwtSecret" `
    "Authentication__DefaultPassword=$AdminPassword" `
    "Authentication__JwtExpirationHours=24" `
    "SqlAnalyzer__AllowedOrigins__0=https://black-desert-02d93d30f.2.azurestaticapps.net" `
    "SqlAnalyzer__AllowedOrigins__1=https://sqlanalyzer-web.azurestaticapps.net"

# Restart API
Write-Host "Restarting API..." -ForegroundColor Yellow
az webapp restart --name sqlanalyzer-api --resource-group rg-sqlanalyzer

Write-Host "✅ API deployed" -ForegroundColor Green

# Deploy Frontend
Write-Host "`nDeploying frontend to Static Web App..." -ForegroundColor Yellow
Set-Location -Path "../../.."

# The SWA should auto-deploy via GitHub Actions, but we'll trigger it
Write-Host "Frontend will deploy automatically via GitHub Actions" -ForegroundColor Yellow
Write-Host "Check status at: https://github.com/[your-repo]/actions" -ForegroundColor Gray

# Step 4: Verify deployment
Write-Host "`n=== Verifying Deployment ===" -ForegroundColor Cyan

# Test API health
Write-Host "`nTesting API health..." -ForegroundColor Yellow
Start-Sleep -Seconds 10  # Wait for restart

try {
    $health = Invoke-RestMethod -Uri "https://sqlanalyzer-api.azurewebsites.net/health" -ErrorAction Stop
    Write-Host "✅ API is healthy: $health" -ForegroundColor Green
} catch {
    Write-Host "⚠️  API health check failed. It may still be starting up." -ForegroundColor Yellow
}

# Test authentication endpoint
Write-Host "`nTesting authentication..." -ForegroundColor Yellow
try {
    $loginTest = @{
        username = "admin"
        password = $AdminPassword
    } | ConvertTo-Json

    $response = Invoke-RestMethod `
        -Uri "https://sqlanalyzer-api.azurewebsites.net/api/v1/auth/login" `
        -Method POST `
        -Body $loginTest `
        -ContentType "application/json" `
        -ErrorAction Stop

    if ($response.token) {
        Write-Host "✅ Authentication is working" -ForegroundColor Green
        Write-Host "   Token expires at: $($response.expiresAt)" -ForegroundColor Gray
    }
} catch {
    Write-Host "⚠️  Authentication test failed: $_" -ForegroundColor Yellow
}

# Output summary
Write-Host "`n=== Deployment Summary ===" -ForegroundColor Cyan
Write-Host "API URL: https://sqlanalyzer-api.azurewebsites.net" -ForegroundColor White
Write-Host "UI URL: https://black-desert-02d93d30f.2.azurestaticapps.net" -ForegroundColor White
Write-Host ""
Write-Host "Authentication:" -ForegroundColor Yellow
Write-Host "  Username: admin" -ForegroundColor White
Write-Host "  Password: $AdminPassword" -ForegroundColor White
Write-Host ""
Write-Host "JWT Secret has been set in Azure (not shown for security)" -ForegroundColor Gray
Write-Host ""
Write-Host "✅ Deployment complete at: $(Get-Date)" -ForegroundColor Green

# Save deployment info
$deploymentInfo = @{
    DeploymentTime = Get-Date
    ApiUrl = "https://sqlanalyzer-api.azurewebsites.net"
    UiUrl = "https://black-desert-02d93d30f.2.azurestaticapps.net"
    JwtSecretConfigured = $true
    AdminPasswordSet = ($AdminPassword -ne "SqlAnalyzer2024!")
}

$deploymentInfo | ConvertTo-Json | Set-Content "deployment-info.json"
Write-Host "`nDeployment info saved to deployment-info.json" -ForegroundColor Gray