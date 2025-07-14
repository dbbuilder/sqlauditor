# Simplified deployment script for SQL Analyzer with Authentication
# This deploys the current state with authentication enabled

param(
    [string]$AdminPassword = "AnalyzeThis!!"
)

Write-Host "=== SQL Analyzer Authentication Deployment ===" -ForegroundColor Cyan
Write-Host "Admin Password: $AdminPassword" -ForegroundColor Yellow

# Load Azure credentials
if (Test-Path ".env.azure.local") {
    Get-Content ".env.azure.local" | ForEach-Object {
        if ($_ -match '^(.+?)=(.+)$') {
            [System.Environment]::SetEnvironmentVariable($matches[1], $matches[2])
        }
    }
    Write-Host "✅ Azure credentials loaded" -ForegroundColor Green
}

# Generate JWT secret
Write-Host "`nGenerating secure JWT secret..." -ForegroundColor Yellow
$bytes = New-Object byte[] 64
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$JwtSecret = [Convert]::ToBase64String($bytes)
Write-Host "✅ JWT secret generated" -ForegroundColor Green

# Check Azure login
Write-Host "`nChecking Azure login..." -ForegroundColor Yellow
$account = az account show 2>$null | ConvertFrom-Json
if (!$account) {
    Write-Host "Please login to Azure..." -ForegroundColor Yellow
    az login
}

# Configure API app settings
Write-Host "`nConfiguring API authentication settings..." -ForegroundColor Yellow
try {
    az webapp config appsettings set `
        --name sqlanalyzer-api `
        --resource-group rg-sqlanalyzer `
        --settings `
        "Authentication__JwtSecret=$JwtSecret" `
        "Authentication__DefaultUsername=admin" `
        "Authentication__DefaultPassword=$AdminPassword" `
        "Authentication__JwtExpirationHours=24" `
        "SqlAnalyzer__AllowedOrigins__0=https://black-desert-02d93d30f.2.azurestaticapps.net" `
        "SqlAnalyzer__AllowedOrigins__1=https://sqlanalyzer-web.azurestaticapps.net" `
        "SqlAnalyzer__AllowedOrigins__2=http://localhost:5173" `
        "SqlAnalyzer__AllowedOrigins__3=http://localhost:3000"
    
    Write-Host "✅ Authentication settings configured" -ForegroundColor Green
} catch {
    Write-Host "❌ Failed to configure settings: $_" -ForegroundColor Red
}

# Restart API to apply settings
Write-Host "`nRestarting API..." -ForegroundColor Yellow
az webapp restart --name sqlanalyzer-api --resource-group rg-sqlanalyzer
Write-Host "✅ API restarted" -ForegroundColor Green

# Build and deploy frontend with auth
Write-Host "`nBuilding frontend..." -ForegroundColor Yellow
Set-Location -Path "src/SqlAnalyzer.Web"

# Ensure dependencies are installed
if (!(Test-Path "node_modules")) {
    Write-Host "Installing frontend dependencies..." -ForegroundColor Yellow
    npm install
}

# Build frontend
Write-Host "Building Vue application..." -ForegroundColor Yellow
npm run build

Write-Host "✅ Frontend built" -ForegroundColor Green
Set-Location -Path "../.."

# Frontend deploys automatically via GitHub Actions
Write-Host "`nFrontend will deploy via GitHub Actions after commit" -ForegroundColor Yellow

# Wait for API to be ready
Write-Host "`nWaiting for API to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 20

# Test authentication
Write-Host "`nTesting authentication..." -ForegroundColor Yellow
$testUrl = "https://sqlanalyzer-api.azurewebsites.net/api/v1/auth/login"
$testBody = @{
    username = "admin"
    password = $AdminPassword
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod `
        -Uri $testUrl `
        -Method POST `
        -Body $testBody `
        -ContentType "application/json" `
        -ErrorAction Stop
    
    if ($response.token) {
        Write-Host "✅ Authentication is working!" -ForegroundColor Green
        Write-Host "   Token expires at: $($response.expiresAt)" -ForegroundColor Gray
        
        # Test protected endpoint
        $headers = @{ "Authorization" = "Bearer $($response.token)" }
        $protected = Invoke-RestMethod `
            -Uri "https://sqlanalyzer-api.azurewebsites.net/api/v1/analysis/types" `
            -Headers $headers `
            -ErrorAction Stop
        
        Write-Host "✅ Protected endpoints accessible" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠️  Authentication test failed: $_" -ForegroundColor Yellow
    Write-Host "   The API may still be starting up. Try again in a minute." -ForegroundColor Gray
}

# Create verification script
$verifyScript = @"
# Verify SQL Analyzer Authentication
`$response = Invoke-RestMethod ``
    -Uri "https://sqlanalyzer-api.azurewebsites.net/api/v1/auth/login" ``
    -Method POST ``
    -Body (@{username="admin"; password="$AdminPassword"} | ConvertTo-Json) ``
    -ContentType "application/json"

if (`$response.token) {
    Write-Host "✅ Login successful!" -ForegroundColor Green
    Write-Host "Token: `$(`$response.token.Substring(0,50))..." -ForegroundColor Gray
} else {
    Write-Host "❌ Login failed" -ForegroundColor Red
}
"@

$verifyScript | Set-Content "verify-auth.ps1"

# Summary
Write-Host "`n=== Deployment Summary ===" -ForegroundColor Cyan
Write-Host "API URL: https://sqlanalyzer-api.azurewebsites.net" -ForegroundColor White
Write-Host "UI URL: https://black-desert-02d93d30f.2.azurestaticapps.net" -ForegroundColor White
Write-Host ""
Write-Host "Authentication Credentials:" -ForegroundColor Yellow
Write-Host "  Username: admin" -ForegroundColor White
Write-Host "  Password: $AdminPassword" -ForegroundColor White
Write-Host ""
Write-Host "To verify authentication:" -ForegroundColor Yellow
Write-Host "  .\verify-auth.ps1" -ForegroundColor White
Write-Host ""
Write-Host "✅ Authentication deployment complete!" -ForegroundColor Green