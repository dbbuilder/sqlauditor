#!/bin/bash

# Complete SQL Analyzer Deployment Script with Authentication
# This script builds, tests, and deploys the entire application

# Default values
JWT_SECRET=""
ADMIN_PASSWORD="SqlAnalyzer2024!"
SKIP_TESTS=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --jwt-secret)
            JWT_SECRET="$2"
            shift 2
            ;;
        --admin-password)
            ADMIN_PASSWORD="$2"
            shift 2
            ;;
        --skip-tests)
            SKIP_TESTS=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

echo -e "\033[36m=== SQL Analyzer Complete Deployment ===\033[0m"
echo -e "\033[90mStarting at: $(date)\033[0m"

# Check prerequisites
echo -e "\n\033[33mChecking prerequisites...\033[0m"
HAS_ERRORS=false

# Check .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo -e "\033[31m❌ .NET SDK not found. Please install from https://dotnet.microsoft.com/download\033[0m"
    HAS_ERRORS=true
fi

# Check Node.js
if ! command -v node &> /dev/null; then
    echo -e "\033[31m❌ Node.js not found. Please install from https://nodejs.org/\033[0m"
    HAS_ERRORS=true
fi

# Check Azure CLI
if ! command -v az &> /dev/null; then
    echo -e "\033[31m❌ Azure CLI not found. Please install from https://aka.ms/InstallAzureCLI\033[0m"
    HAS_ERRORS=true
fi

if [ "$HAS_ERRORS" = true ]; then
    echo -e "\n\033[31mPlease install missing prerequisites and run again.\033[0m"
    exit 1
fi

echo -e "\033[32m✅ All prerequisites found\033[0m"

# Load environment variables
if [ -f ".env.azure.local" ]; then
    echo -e "\n\033[33mLoading Azure credentials...\033[0m"
    export $(cat .env.azure.local | xargs)
    echo -e "\033[32m✅ Azure credentials loaded\033[0m"
fi

# Generate JWT secret if not provided
if [ -z "$JWT_SECRET" ]; then
    echo -e "\n\033[33mGenerating secure JWT secret...\033[0m"
    JWT_SECRET=$(openssl rand -base64 64 | tr -d '\n')
    echo -e "\033[32m✅ JWT secret generated\033[0m"
fi

# Step 1: Build and test backend
echo -e "\n\033[36m=== Building Backend ===\033[0m"
cd src/SqlAnalyzer.Api

echo -e "\033[33mRestoring packages...\033[0m"
dotnet restore

echo -e "\033[33mBuilding API...\033[0m"
dotnet build -c Release

if [ "$SKIP_TESTS" = false ]; then
    echo -e "\033[33mRunning tests...\033[0m"
    cd ../../tests/SqlAnalyzer.Tests
    dotnet test --no-restore
    cd ../../src/SqlAnalyzer.Api
fi

echo -e "\033[33mPublishing API...\033[0m"
dotnet publish -c Release -o ./publish

echo -e "\033[32m✅ Backend build complete\033[0m"

# Step 2: Build frontend
echo -e "\n\033[36m=== Building Frontend ===\033[0m"
cd ../SqlAnalyzer.Web

echo -e "\033[33mInstalling dependencies...\033[0m"
npm install

echo -e "\033[33mBuilding Vue app...\033[0m"
npm run build

echo -e "\033[32m✅ Frontend build complete\033[0m"

# Step 3: Deploy to Azure
echo -e "\n\033[36m=== Deploying to Azure ===\033[0m"
cd ../..

# Login to Azure if needed
echo -e "\n\033[33mChecking Azure login...\033[0m"
if ! az account show &> /dev/null; then
    echo -e "\033[33mPlease login to Azure...\033[0m"
    az login
fi

# Deploy API
echo -e "\n\033[33mDeploying API to Azure App Service...\033[0m"
cd src/SqlAnalyzer.Api/publish

# Create ZIP for deployment
echo -e "\033[33mCreating deployment package...\033[0m"
zip -r ../api-deploy.zip .

# Deploy using ZIP deploy
echo -e "\033[33mDeploying API...\033[0m"
az webapp deployment source config-zip \
    --resource-group rg-sqlanalyzer \
    --name sqlanalyzer-api \
    --src ../api-deploy.zip

# Configure app settings
echo -e "\n\033[33mConfiguring API settings...\033[0m"
az webapp config appsettings set \
    --name sqlanalyzer-api \
    --resource-group rg-sqlanalyzer \
    --settings \
    "Authentication__JwtSecret=$JWT_SECRET" \
    "Authentication__DefaultPassword=$ADMIN_PASSWORD" \
    "Authentication__JwtExpirationHours=24" \
    "SqlAnalyzer__AllowedOrigins__0=https://black-desert-02d93d30f.2.azurestaticapps.net" \
    "SqlAnalyzer__AllowedOrigins__1=https://sqlanalyzer-web.azurestaticapps.net"

# Restart API
echo -e "\033[33mRestarting API...\033[0m"
az webapp restart --name sqlanalyzer-api --resource-group rg-sqlanalyzer

echo -e "\033[32m✅ API deployed\033[0m"

# Deploy Frontend
echo -e "\n\033[33mDeploying frontend to Static Web App...\033[0m"
cd ../../..

# The SWA should auto-deploy via GitHub Actions
echo -e "\033[33mFrontend will deploy automatically via GitHub Actions\033[0m"
echo -e "\033[90mCheck status at: https://github.com/[your-repo]/actions\033[0m"

# Step 4: Verify deployment
echo -e "\n\033[36m=== Verifying Deployment ===\033[0m"

# Test API health
echo -e "\n\033[33mTesting API health...\033[0m"
sleep 10  # Wait for restart

if curl -s https://sqlanalyzer-api.azurewebsites.net/health > /dev/null; then
    echo -e "\033[32m✅ API is healthy\033[0m"
else
    echo -e "\033[33m⚠️  API health check failed. It may still be starting up.\033[0m"
fi

# Test authentication endpoint
echo -e "\n\033[33mTesting authentication...\033[0m"
AUTH_RESPONSE=$(curl -s -X POST https://sqlanalyzer-api.azurewebsites.net/api/v1/auth/login \
    -H "Content-Type: application/json" \
    -d "{\"username\":\"admin\",\"password\":\"$ADMIN_PASSWORD\"}")

if echo "$AUTH_RESPONSE" | grep -q "token"; then
    echo -e "\033[32m✅ Authentication is working\033[0m"
else
    echo -e "\033[33m⚠️  Authentication test failed\033[0m"
fi

# Output summary
echo -e "\n\033[36m=== Deployment Summary ===\033[0m"
echo -e "API URL: \033[0mhttps://sqlanalyzer-api.azurewebsites.net"
echo -e "UI URL: \033[0mhttps://black-desert-02d93d30f.2.azurestaticapps.net"
echo ""
echo -e "\033[33mAuthentication:\033[0m"
echo -e "  Username: admin"
echo -e "  Password: $ADMIN_PASSWORD"
echo ""
echo -e "\033[90mJWT Secret has been set in Azure (not shown for security)\033[0m"
echo ""
echo -e "\033[32m✅ Deployment complete at: $(date)\033[0m"

# Save deployment info
cat > deployment-info.json <<EOF
{
  "deploymentTime": "$(date -Iseconds)",
  "apiUrl": "https://sqlanalyzer-api.azurewebsites.net",
  "uiUrl": "https://black-desert-02d93d30f.2.azurestaticapps.net",
  "jwtSecretConfigured": true,
  "adminPasswordSet": $([ "$ADMIN_PASSWORD" != "SqlAnalyzer2024!" ] && echo "true" || echo "false")
}
EOF

echo -e "\n\033[90mDeployment info saved to deployment-info.json\033[0m"