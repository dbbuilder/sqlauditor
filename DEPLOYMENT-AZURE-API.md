# SQL Analyzer API - Azure App Service Deployment Guide

This guide covers deploying the SQL Analyzer API to Azure App Service on Linux.

## Required Azure Credentials

### 1. Azure Account Credentials
- **Azure Subscription ID**: Found in Azure Portal → Subscriptions
- **Azure Tenant ID**: Azure Active Directory → Properties → Tenant ID
- **Service Principal** (for automation):
  - Client ID (Application ID)
  - Client Secret
  - Tenant ID

### 2. Resource Credentials
- **Resource Group Name**: Container for all resources
- **App Service Name**: Unique name for your API (e.g., `sqlanalyzer-api`)
- **App Service Plan**: Linux-based plan name

## Step 1: Create Azure Resources

### Option A: Using Azure Portal

1. **Create Resource Group**:
   - Name: `rg-sqlanalyzer`
   - Region: `East US` (or preferred region)

2. **Create App Service Plan**:
   - Name: `asp-sqlanalyzer-linux`
   - OS: Linux
   - Pricing Tier: B1 (Basic) or F1 (Free)

3. **Create App Service**:
   - Name: `sqlanalyzer-api` (must be globally unique)
   - Publish: Code
   - Runtime Stack: .NET 8
   - OS: Linux
   - Region: Same as resource group

### Option B: Using Azure CLI

```bash
# Login to Azure
az login

# Set subscription (if you have multiple)
az account set --subscription "Your-Subscription-Name"

# Create resource group
az group create \
  --name rg-sqlanalyzer \
  --location eastus

# Create App Service Plan (Linux)
az appservice plan create \
  --name asp-sqlanalyzer-linux \
  --resource-group rg-sqlanalyzer \
  --sku B1 \
  --is-linux

# Create Web App
az webapp create \
  --name sqlanalyzer-api \
  --resource-group rg-sqlanalyzer \
  --plan asp-sqlanalyzer-linux \
  --runtime "DOTNET|8.0"
```

## Step 2: Configure App Service

### 2.1 Application Settings

```bash
# Set application settings
az webapp config appsettings set \
  --name sqlanalyzer-api \
  --resource-group rg-sqlanalyzer \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    SqlAnalyzer__EnableCaching=true \
    SqlAnalyzer__DefaultTimeout=300 \
    SqlAnalyzer__MaxConcurrentAnalyses=10

# Set connection strings (use your actual connection string)
az webapp config connection-string set \
  --name sqlanalyzer-api \
  --resource-group rg-sqlanalyzer \
  --connection-string-type SQLAzure \
  --settings DefaultConnection="Server=tcp:your-server.database.windows.net,1433;Database=SqlAnalyzerDB;User ID=your-user;Password=your-password;Encrypt=True;TrustServerCertificate=False;"
```

### 2.2 CORS Configuration

```bash
# Configure CORS for Vercel frontend
az webapp cors add \
  --name sqlanalyzer-api \
  --resource-group rg-sqlanalyzer \
  --allowed-origins \
    "https://sqlanalyzer.vercel.app" \
    "https://your-custom-domain.com" \
    "http://localhost:5173"
```

## Step 3: Create Service Principal (for CI/CD)

```bash
# Create service principal and assign contributor role
az ad sp create-for-rbac \
  --name "sp-sqlanalyzer-deployment" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/rg-sqlanalyzer \
  --sdk-auth
```

This outputs JSON with credentials:
```json
{
  "clientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "clientSecret": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "subscriptionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
}
```

## Step 4: Deployment Methods

### Method 1: GitHub Actions (Recommended)

Create `.github/workflows/deploy-api-azure.yml`:

```yaml
name: Deploy API to Azure

on:
  push:
    branches: [main]
    paths:
      - 'src/SqlAnalyzer.Api/**'
      - 'src/SqlAnalyzer.Core/**'
      - 'src/SqlAnalyzer.Shared/**'

env:
  AZURE_WEBAPP_NAME: sqlanalyzer-api
  DOTNET_VERSION: '8.0.x'

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Build
      run: |
        cd src/SqlAnalyzer.Api
        dotnet restore
        dotnet build --configuration Release
        dotnet publish -c Release -o ./publish
    
    - name: Deploy to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: ${{ env.AZURE_WEBAPP_NAME }}
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./src/SqlAnalyzer.Api/publish
```

Add publish profile to GitHub Secrets:
1. Azure Portal → Your App Service → Deployment Center
2. Download publish profile
3. GitHub → Settings → Secrets → New repository secret
4. Name: `AZURE_WEBAPP_PUBLISH_PROFILE`
5. Value: Contents of downloaded file

### Method 2: Azure CLI Deployment

```bash
# Build the application
cd src/SqlAnalyzer.Api
dotnet publish -c Release -o ./publish

# Create ZIP file
cd publish
zip -r ../deploy.zip .
cd ..

# Deploy to Azure
az webapp deployment source config-zip \
  --name sqlanalyzer-api \
  --resource-group rg-sqlanalyzer \
  --src deploy.zip
```

### Method 3: Visual Studio / VS Code

**Visual Studio:**
1. Right-click project → Publish
2. Target: Azure → Azure App Service (Linux)
3. Select existing or create new
4. Publish

**VS Code:**
1. Install Azure App Service extension
2. Right-click project → Deploy to Web App
3. Select subscription and app service

## Step 5: Post-Deployment Configuration

### 5.1 Enable Application Insights

```bash
# Create Application Insights
az monitor app-insights component create \
  --app sqlanalyzer-insights \
  --location eastus \
  --resource-group rg-sqlanalyzer

# Get instrumentation key
INSTRUMENTATION_KEY=$(az monitor app-insights component show \
  --app sqlanalyzer-insights \
  --resource-group rg-sqlanalyzer \
  --query instrumentationKey -o tsv)

# Configure App Service
az webapp config appsettings set \
  --name sqlanalyzer-api \
  --resource-group rg-sqlanalyzer \
  --settings \
    APPINSIGHTS_INSTRUMENTATIONKEY=$INSTRUMENTATION_KEY \
    ApplicationInsights__InstrumentationKey=$INSTRUMENTATION_KEY
```

### 5.2 Configure Logging

```bash
# Enable application logging
az webapp log config \
  --name sqlanalyzer-api \
  --resource-group rg-sqlanalyzer \
  --application-logging filesystem \
  --level information \
  --detailed-error-messages true \
  --failed-request-tracing true
```

### 5.3 Set Up Health Check

```bash
# Configure health check
az webapp config set \
  --name sqlanalyzer-api \
  --resource-group rg-sqlanalyzer \
  --health-check-path /health
```

## Step 6: Security Configuration

### 6.1 Enable HTTPS Only

```bash
az webapp update \
  --name sqlanalyzer-api \
  --resource-group rg-sqlanalyzer \
  --https-only true
```

### 6.2 Configure Managed Identity (for Azure SQL)

```bash
# Enable system-assigned managed identity
az webapp identity assign \
  --name sqlanalyzer-api \
  --resource-group rg-sqlanalyzer

# Grant SQL access (if using Azure SQL)
# Run in SQL Database:
# CREATE USER [sqlanalyzer-api] FROM EXTERNAL PROVIDER;
# ALTER ROLE db_datareader ADD MEMBER [sqlanalyzer-api];
```

### 6.3 Key Vault Integration (for secrets)

```bash
# Create Key Vault
az keyvault create \
  --name kv-sqlanalyzer \
  --resource-group rg-sqlanalyzer \
  --location eastus

# Add secrets
az keyvault secret set \
  --vault-name kv-sqlanalyzer \
  --name "ConnectionStrings--DefaultConnection" \
  --value "your-connection-string"

# Grant App Service access
IDENTITY_PRINCIPAL_ID=$(az webapp identity show \
  --name sqlanalyzer-api \
  --resource-group rg-sqlanalyzer \
  --query principalId -o tsv)

az keyvault set-policy \
  --name kv-sqlanalyzer \
  --object-id $IDENTITY_PRINCIPAL_ID \
  --secret-permissions get list
```

## Step 7: Custom Domain (Optional)

```bash
# Add custom domain
az webapp config hostname add \
  --webapp-name sqlanalyzer-api \
  --resource-group rg-sqlanalyzer \
  --hostname api.yourdomain.com

# Upload SSL certificate
az webapp config ssl upload \
  --name sqlanalyzer-api \
  --resource-group rg-sqlanalyzer \
  --certificate-file ./your-certificate.pfx \
  --certificate-password "your-password"

# Bind SSL
az webapp config ssl bind \
  --name sqlanalyzer-api \
  --resource-group rg-sqlanalyzer \
  --certificate-thumbprint "YOUR-CERT-THUMBPRINT" \
  --ssl-type SNI
```

## Environment Variables Summary

```bash
# Required environment variables
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:8080

# Application settings
SqlAnalyzer__EnableCaching=true
SqlAnalyzer__DefaultTimeout=300
SqlAnalyzer__MaxConcurrentAnalyses=10
SqlAnalyzer__CircuitBreaker__FailureThreshold=5
SqlAnalyzer__CircuitBreaker__OpenDurationSeconds=30

# CORS (if not using Azure CORS)
Cors__AllowedOrigins__0=https://sqlanalyzer.vercel.app
Cors__AllowedOrigins__1=https://your-domain.com

# Redis (optional)
Redis__Enabled=true
Redis__ConnectionString=your-redis.redis.cache.windows.net:6380,password=xxx,ssl=True,abortConnect=False

# Application Insights
ApplicationInsights__InstrumentationKey=your-key
```

## Deployment Script

Create `deploy-azure.ps1`:

```powershell
# Azure API Deployment Script
param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroup = "rg-sqlanalyzer",
    
    [Parameter(Mandatory=$true)]
    [string]$AppServiceName = "sqlanalyzer-api",
    
    [string]$Location = "eastus"
)

# Login to Azure
az login

# Build and publish
Write-Host "Building application..." -ForegroundColor Yellow
dotnet publish src/SqlAnalyzer.Api/SqlAnalyzer.Api.csproj -c Release -o ./publish

# Create ZIP
Write-Host "Creating deployment package..." -ForegroundColor Yellow
Compress-Archive -Path ./publish/* -DestinationPath deploy.zip -Force

# Deploy
Write-Host "Deploying to Azure..." -ForegroundColor Yellow
az webapp deployment source config-zip `
    --name $AppServiceName `
    --resource-group $ResourceGroup `
    --src deploy.zip

# Cleanup
Remove-Item deploy.zip
Remove-Item -Recurse -Force ./publish

Write-Host "Deployment complete!" -ForegroundColor Green
Write-Host "API URL: https://$AppServiceName.azurewebsites.net" -ForegroundColor Cyan
```

## Monitoring and Troubleshooting

### View Logs

```bash
# Stream logs
az webapp log tail \
  --name sqlanalyzer-api \
  --resource-group rg-sqlanalyzer

# Download logs
az webapp log download \
  --name sqlanalyzer-api \
  --resource-group rg-sqlanalyzer \
  --log-file logs.zip
```

### SSH Access

```bash
# Open SSH session
az webapp ssh \
  --name sqlanalyzer-api \
  --resource-group rg-sqlanalyzer
```

### Restart App

```bash
az webapp restart \
  --name sqlanalyzer-api \
  --resource-group rg-sqlanalyzer
```

## Cost Optimization

1. **Use B1 tier** for production (₹3,500/month)
2. **Use F1 tier** for development (free)
3. **Enable auto-scale** for traffic spikes
4. **Use Azure SQL Basic** tier for small databases
5. **Enable Application Insights sampling** to reduce costs

## Security Checklist

- [ ] HTTPS only enabled
- [ ] Managed Identity configured
- [ ] Secrets in Key Vault
- [ ] CORS properly configured
- [ ] Authentication enabled (if needed)
- [ ] Network restrictions configured
- [ ] Diagnostic logs enabled
- [ ] Backup strategy in place