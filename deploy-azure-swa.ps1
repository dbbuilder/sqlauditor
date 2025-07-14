# SQL Analyzer - Azure Static Web App Deployment Script
param(
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroup = "rg-sqlanalyzer",
    
    [Parameter(Mandatory=$false)]
    [string]$StaticWebAppName = "sqlanalyzer-web",
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "eastus2",
    
    [Parameter(Mandatory=$false)]
    [string]$ApiUrl = "https://sqlanalyzer-api.azurewebsites.net",
    
    [switch]$SkipBuild
)

Write-Host "SQL Analyzer - Azure Static Web App Deployment" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host ""

# Check Azure CLI
Write-Host -NoNewline "Checking Azure CLI..."
if (Get-Command az -ErrorAction SilentlyContinue) {
    Write-Host " ✓" -ForegroundColor Green
} else {
    Write-Host " ✗" -ForegroundColor Red
    Write-Host "Please install Azure CLI" -ForegroundColor Yellow
    exit 1
}

# Build the Web UI
if (-not $SkipBuild) {
    Write-Host "`nBuilding Web UI..." -ForegroundColor Yellow
    Push-Location "src/SqlAnalyzer.Web"
    
    # Install dependencies
    Write-Host "Installing dependencies..."
    npm install
    
    # Set production environment variables
    $env:VITE_API_URL = $ApiUrl
    $env:VITE_ENABLE_MOCK_DATA = "false"
    
    # Build for production
    Write-Host "Building for production..."
    npm run build
    
    Pop-Location
    Write-Host "✓ Build completed" -ForegroundColor Green
}

# Create Static Web App
Write-Host "`nCreating Azure Static Web App..." -ForegroundColor Yellow

# Check if SWA exists
$swaExists = az staticwebapp show `
    --name $StaticWebAppName `
    --resource-group $ResourceGroup `
    --query "name" -o tsv 2>$null

if (-not $swaExists) {
    Write-Host "Creating new Static Web App..."
    
    # Create the Static Web App
    $deployment = az staticwebapp create `
        --name $StaticWebAppName `
        --resource-group $ResourceGroup `
        --location $Location `
        --sku Free `
        --output json | ConvertFrom-Json
    
    Write-Host "✓ Static Web App created" -ForegroundColor Green
    
    # Get deployment token
    $deploymentToken = az staticwebapp secrets list `
        --name $StaticWebAppName `
        --resource-group $ResourceGroup `
        --query "properties.apiKey" -o tsv
    
    Write-Host "Deployment token retrieved" -ForegroundColor Green
} else {
    Write-Host "Static Web App already exists" -ForegroundColor Yellow
    
    # Get deployment token
    $deploymentToken = az staticwebapp secrets list `
        --name $StaticWebAppName `
        --resource-group $ResourceGroup `
        --query "properties.apiKey" -o tsv
}

# Deploy to Static Web App
Write-Host "`nDeploying to Azure Static Web App..." -ForegroundColor Yellow

Push-Location "src/SqlAnalyzer.Web"

# Use SWA CLI for deployment
Write-Host "Installing SWA CLI if needed..."
npm install -g @azure/static-web-apps-cli

Write-Host "Deploying application..."
swa deploy ./dist `
    --deployment-token $deploymentToken `
    --env production

Pop-Location

# Configure app settings
Write-Host "`nConfiguring Static Web App settings..." -ForegroundColor Yellow

az staticwebapp appsettings set `
    --name $StaticWebAppName `
    --resource-group $ResourceGroup `
    --setting-names `
        VITE_API_URL=$ApiUrl `
        VITE_ENABLE_MOCK_DATA=false

# Get the URL
$swaUrl = az staticwebapp show `
    --name $StaticWebAppName `
    --resource-group $ResourceGroup `
    --query "defaultHostname" -o tsv

Write-Host "`n✅ Deployment Complete!" -ForegroundColor Green
Write-Host "=======================`n" -ForegroundColor Green
Write-Host "Static Web App URL: https://$swaUrl" -ForegroundColor Cyan
Write-Host "API URL: $ApiUrl" -ForegroundColor Cyan

Write-Host "`nNext Steps:" -ForegroundColor Yellow
Write-Host "1. Update API CORS settings to allow: https://$swaUrl" -ForegroundColor White
Write-Host "2. Test the application at: https://$swaUrl" -ForegroundColor White
Write-Host "3. Configure custom domain if needed" -ForegroundColor White