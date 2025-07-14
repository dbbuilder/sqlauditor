# SQL Analyzer Azure Deployment Script with Local Credentials
# This script uses local credentials from .env.azure.local file

param(
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroup = "rg-sqlanalyzer",
    
    [Parameter(Mandatory=$false)]
    [string]$AppServiceName = "sqlanalyzer-api",
    
    [Parameter(Mandatory=$false)]
    [string]$AppServicePlan = "asp-sqlanalyzer-linux",
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "eastus",
    
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

Write-Color "SQL Analyzer - Azure Deployment (Local Credentials)" "Cyan"
Write-Color "===================================================" "Cyan"
Write-Color ""

# Load Azure credentials from local file
$envFile = Join-Path $PSScriptRoot ".env.azure.local"
if (-not (Test-Path $envFile)) {
    Write-Color "ERROR: .env.azure.local file not found!" "Red"
    Write-Color "Please ensure the file exists with your Azure credentials." "Yellow"
    exit 1
}

Write-Color "Loading Azure credentials from local file..." "Yellow"

# Read credentials from .env.azure.local
$envContent = Get-Content $envFile
$credentials = @{}
foreach ($line in $envContent) {
    if ($line -match '^([^#=]+)=(.*)$') {
        $key = $matches[1].Trim()
        $value = $matches[2].Trim()
        $credentials[$key] = $value
    }
}

# Extract individual values
$clientId = $credentials["AZURE_CLIENT_ID"]
$clientSecret = $credentials["AZURE_CLIENT_SECRET"]
$subscriptionId = $credentials["AZURE_SUBSCRIPTION_ID"]
$tenantId = $credentials["AZURE_TENANT_ID"]

if (-not $clientId -or -not $clientSecret -or -not $subscriptionId -or -not $tenantId) {
    Write-Color "ERROR: Missing required Azure credentials in .env.azure.local" "Red"
    exit 1
}

# Login to Azure using service principal
Write-Color "Logging in to Azure with service principal..." "Yellow"
$securePassword = ConvertTo-SecureString $clientSecret -AsPlainText -Force
$credential = New-Object System.Management.Automation.PSCredential($clientId, $securePassword)

try {
    Connect-AzAccount -ServicePrincipal -Credential $credential -Tenant $tenantId -ErrorAction Stop | Out-Null
    Set-AzContext -Subscription $subscriptionId | Out-Null
    Write-Color "Successfully logged in to Azure" "Green"
} catch {
    Write-Color "ERROR: Failed to login to Azure" "Red"
    Write-Color $_.Exception.Message "Red"
    exit 1
}

# Create Infrastructure
if (-not $SkipInfrastructure) {
    Write-Color "`nCreating Azure infrastructure..." "Yellow"
    
    # Create Resource Group
    Write-Host -NoNewline "Creating resource group..."
    try {
        $rg = Get-AzResourceGroup -Name $ResourceGroup -ErrorAction SilentlyContinue
        if (-not $rg) {
            New-AzResourceGroup -Name $ResourceGroup -Location $Location | Out-Null
        }
        Write-Color " ✓" "Green"
    } catch {
        Write-Color " ✗" "Red"
        Write-Color $_.Exception.Message "Red"
        exit 1
    }
    
    # Create App Service Plan
    Write-Host -NoNewline "Creating App Service Plan (Basic B1)..."
    try {
        $plan = Get-AzAppServicePlan -ResourceGroupName $ResourceGroup -Name $AppServicePlan -ErrorAction SilentlyContinue
        if (-not $plan) {
            New-AzAppServicePlan `
                -ResourceGroupName $ResourceGroup `
                -Name $AppServicePlan `
                -Location $Location `
                -Tier "Basic" `
                -NumberofWorkers 1 `
                -WorkerSize "Small" `
                -Linux | Out-Null
        }
        Write-Color " ✓" "Green"
    } catch {
        Write-Color " ✗" "Red"
        Write-Color $_.Exception.Message "Red"
        exit 1
    }
    
    # Create Web App
    Write-Host -NoNewline "Creating Web App..."
    try {
        $app = Get-AzWebApp -ResourceGroupName $ResourceGroup -Name $AppServiceName -ErrorAction SilentlyContinue
        if (-not $app) {
            New-AzWebApp `
                -ResourceGroupName $ResourceGroup `
                -Name $AppServiceName `
                -AppServicePlan $AppServicePlan `
                -RuntimeStack "DOTNETCORE:8.0" | Out-Null
        }
        Write-Color " ✓" "Green"
    } catch {
        Write-Color " ✗" "Red"
        Write-Color $_.Exception.Message "Red"
        exit 1
    }
    
    # Configure App Settings
    Write-Host -NoNewline "Configuring app settings..."
    $appSettings = @{
        "ASPNETCORE_ENVIRONMENT" = "Production"
        "SqlAnalyzer__EnableCaching" = "true"
        "SqlAnalyzer__DefaultTimeout" = "300"
        "SqlAnalyzer__MaxConcurrentAnalyses" = "10"
        "SqlAnalyzer__CircuitBreaker__FailureThreshold" = "5"
        "SqlAnalyzer__CircuitBreaker__OpenDurationSeconds" = "30"
    }
    Set-AzWebApp `
        -ResourceGroupName $ResourceGroup `
        -Name $AppServiceName `
        -AppSettings $appSettings | Out-Null
    Write-Color " ✓" "Green"
    
    # Configure CORS
    Write-Host -NoNewline "Configuring CORS..."
    $corsRules = @(
        "https://sqlanalyzer.vercel.app",
        "https://sqlanalyzer-web.vercel.app",
        "http://localhost:5173",
        "http://localhost:3000"
    )
    $webApp = Get-AzWebApp -ResourceGroupName $ResourceGroup -Name $AppServiceName
    $webApp.SiteConfig.Cors = @{
        AllowedOrigins = $corsRules
        SupportCredentials = $true
    }
    Set-AzWebApp -WebApp $webApp | Out-Null
    Write-Color " ✓" "Green"
    
    # Enable HTTPS Only
    Write-Host -NoNewline "Enabling HTTPS only..."
    Set-AzWebApp `
        -ResourceGroupName $ResourceGroup `
        -Name $AppServiceName `
        -HttpsOnly $true | Out-Null
    Write-Color " ✓" "Green"
}

# Deploy API
if (-not $SkipApi) {
    Write-Color "`nDeploying API to Azure..." "Yellow"
    
    # Build and publish
    Write-Host -NoNewline "Building API..."
    Push-Location (Join-Path $PSScriptRoot "src\SqlAnalyzer.Api")
    dotnet publish -c Release -o ./publish --quiet
    Write-Color " ✓" "Green"
    
    # Create ZIP
    Write-Host -NoNewline "Creating deployment package..."
    Compress-Archive -Path ./publish/* -DestinationPath deploy.zip -Force
    Write-Color " ✓" "Green"
    
    # Deploy using Azure CLI (fallback method)
    Write-Host -NoNewline "Deploying to Azure..."
    az webapp deployment source config-zip `
        --name $AppServiceName `
        --resource-group $ResourceGroup `
        --src deploy.zip `
        --subscription $subscriptionId | Out-Null
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
    Push-Location (Join-Path $PSScriptRoot "src\SqlAnalyzer.Web")
    
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
Write-Color "`n===================================================" "Cyan"
Write-Color "Deployment Complete!" "Green"
Write-Color "===================================================" "Cyan"
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
Write-Color "`nSecurity Note:" "Red"
Write-Color "Keep your .env.azure.local file secure and never commit it to Git!" "Red"

# Disconnect from Azure
Disconnect-AzAccount -ErrorAction SilentlyContinue | Out-Null