# SQL Analyzer Azure Deployment Script
param(
    [Parameter(Mandatory=$false)]
    [string]$SubscriptionId = "7b2beff3-b38a-4516-a75f-3216725cc4e9",
    
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroup = "rg-sqlanalyzer",
    
    [Parameter(Mandatory=$false)]
    [string]$AppServiceName = "sqlanalyzer-api",
    
    [Parameter(Mandatory=$false)]
    [string]$AppServicePlan = "asp-sqlanalyzer-linux",
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "eastus2",
    
    [Parameter(Mandatory=$false)]
    [string]$VercelToken = "3wRsfj8ZPCOe9CbvQf2qh6XA",
    
    [switch]$SkipInfrastructure,
    [switch]$SkipApi,
    [switch]$SkipWeb,
    [switch]$Production
)

# Colors
function Write-Color {
    param($Text, $Color = "White")
    Write-Host $Text -ForegroundColor $Color
}

Write-Color "SQL Analyzer - Azure Deployment" "Cyan"
Write-Color "================================" "Cyan"
Write-Color ""

# Check Azure CLI
Write-Host -NoNewline "Checking Azure CLI..."
if (Get-Command az -ErrorAction SilentlyContinue) {
    Write-Color " ✓" "Green"
} else {
    Write-Color " ✗" "Red"
    Write-Color "Please install Azure CLI: https://aka.ms/installazurecliwindows" "Yellow"
    exit 1
}

# Login to Azure
Write-Color "`nLogging in to Azure..." "Yellow"
az login --only-show-errors

# Set subscription
Write-Color "Setting subscription..." "Yellow"
az account set --subscription $SubscriptionId

# Create Infrastructure
if (-not $SkipInfrastructure) {
    Write-Color "`nCreating Azure infrastructure..." "Yellow"
    
    # Create Resource Group
    Write-Host -NoNewline "Creating resource group..."
    az group create `
        --name $ResourceGroup `
        --location $Location `
        --output none
    Write-Color " ✓" "Green"
    
    # Create App Service Plan
    Write-Host -NoNewline "Creating App Service Plan (Basic B1)..."
    az appservice plan create `
        --name $AppServicePlan `
        --resource-group $ResourceGroup `
        --location $Location `
        --sku B1 `
        --is-linux `
        --output none
    Write-Color " ✓" "Green"
    
    # Create Web App
    Write-Host -NoNewline "Creating Web App..."
    az webapp create `
        --name $AppServiceName `
        --resource-group $ResourceGroup `
        --plan $AppServicePlan `
        --runtime "DOTNET|8.0" `
        --output none
    Write-Color " ✓" "Green"
    
    # Configure App Settings
    Write-Host -NoNewline "Configuring app settings..."
    az webapp config appsettings set `
        --name $AppServiceName `
        --resource-group $ResourceGroup `
        --settings `
            ASPNETCORE_ENVIRONMENT=Production `
            SqlAnalyzer__EnableCaching=true `
            SqlAnalyzer__DefaultTimeout=300 `
            SqlAnalyzer__MaxConcurrentAnalyses=10 `
            SqlAnalyzer__CircuitBreaker__FailureThreshold=5 `
            SqlAnalyzer__CircuitBreaker__OpenDurationSeconds=30 `
        --output none
    Write-Color " ✓" "Green"
    
    # Configure CORS
    Write-Host -NoNewline "Configuring CORS..."
    az webapp cors add `
        --name $AppServiceName `
        --resource-group $ResourceGroup `
        --allowed-origins `
            "https://sqlanalyzer.vercel.app" `
            "https://sqlanalyzer-web.vercel.app" `
            "http://localhost:5173" `
            "http://localhost:3000" `
        --output none
    Write-Color " ✓" "Green"
    
    # Enable HTTPS Only
    Write-Host -NoNewline "Enabling HTTPS only..."
    az webapp update `
        --name $AppServiceName `
        --resource-group $ResourceGroup `
        --https-only true `
        --output none
    Write-Color " ✓" "Green"
}

# Deploy API
if (-not $SkipApi) {
    Write-Color "`nDeploying API to Azure..." "Yellow"
    
    # Build and publish
    Write-Host -NoNewline "Building API..."
    Push-Location src/SqlAnalyzer.Api
    dotnet publish -c Release -o ./publish --quiet
    Write-Color " ✓" "Green"
    
    # Create ZIP
    Write-Host -NoNewline "Creating deployment package..."
    Compress-Archive -Path ./publish/* -DestinationPath deploy.zip -Force
    Write-Color " ✓" "Green"
    
    # Deploy
    Write-Host -NoNewline "Deploying to Azure..."
    az webapp deployment source config-zip `
        --name $AppServiceName `
        --resource-group $ResourceGroup `
        --src deploy.zip `
        --output none
    Write-Color " ✓" "Green"
    
    # Cleanup
    Remove-Item deploy.zip -Force
    Remove-Item -Recurse -Force ./publish
    Pop-Location
    
    # Get API URL
    $apiUrl = "https://$AppServiceName.azurewebsites.net"
    Write-Color "`nAPI deployed to: $apiUrl" "Cyan"
    
    # Test deployment
    Write-Host -NoNewline "Testing API health endpoint..."
    Start-Sleep -Seconds 30
    try {
        $response = Invoke-WebRequest -Uri "$apiUrl/health" -UseBasicParsing
        if ($response.StatusCode -eq 200) {
            Write-Color " ✓" "Green"
        }
    } catch {
        Write-Color " ✗" "Red"
        Write-Color "Health check failed. Please check the deployment." "Yellow"
    }
}

# Deploy Web UI to Vercel
if (-not $SkipWeb) {
    Write-Color "`nDeploying Web UI to Vercel..." "Yellow"
    
    # Check Vercel CLI
    Write-Host -NoNewline "Checking Vercel CLI..."
    if (Get-Command vercel -ErrorAction SilentlyContinue) {
        Write-Color " ✓" "Green"
    } else {
        Write-Color " ✗" "Red"
        Write-Color "Installing Vercel CLI..." "Yellow"
        npm install -g vercel
    }
    
    # Build Web UI
    Push-Location src/SqlAnalyzer.Web
    
    Write-Host -NoNewline "Installing dependencies..."
    npm install --silent
    Write-Color " ✓" "Green"
    
    Write-Host -NoNewline "Building for production..."
    $env:VITE_API_URL = "https://$AppServiceName.azurewebsites.net"
    $env:VITE_ENABLE_MOCK_DATA = "false"
    npm run build --silent
    Write-Color " ✓" "Green"
    
    # Deploy to Vercel
    Write-Host -NoNewline "Deploying to Vercel..."
    $env:VERCEL_TOKEN = $VercelToken
    
    if ($Production) {
        vercel --prod --token $VercelToken --yes > $null 2>&1
    } else {
        vercel --token $VercelToken --yes > $null 2>&1
    }
    Write-Color " ✓" "Green"
    
    Pop-Location
}

# Summary
Write-Color "`n================================" "Cyan"
Write-Color "Deployment Complete!" "Green"
Write-Color "================================" "Cyan"
Write-Color "`nResources:" "Yellow"
Write-Color "- API URL: https://$AppServiceName.azurewebsites.net" "White"
Write-Color "- Health Check: https://$AppServiceName.azurewebsites.net/health" "White"
Write-Color "- Swagger UI: https://$AppServiceName.azurewebsites.net/swagger" "White"
Write-Color "- Web UI: https://sqlanalyzer.vercel.app (or check Vercel dashboard)" "White"
Write-Color "`nNext Steps:" "Yellow"
Write-Color "1. Add database connection string in Azure Portal" "White"
Write-Color "2. Configure custom domains if needed" "White"
Write-Color "3. Set up monitoring and alerts" "White"
Write-Color "4. Test the full integration" "White"

# Open URLs
$openUrls = Read-Host "`nOpen deployed URLs in browser? (Y/N)"
if ($openUrls -eq 'Y' -or $openUrls -eq 'y') {
    Start-Process "https://$AppServiceName.azurewebsites.net/swagger"
    Start-Process "https://sqlanalyzer.vercel.app"
}