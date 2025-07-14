#!/bin/bash

# SQL Analyzer Azure Deployment Script with Local Credentials
# This script uses local credentials from .env.azure.local file

# Default values
RESOURCE_GROUP="rg-sqlanalyzer"
APP_SERVICE_NAME="sqlanalyzer-api"
APP_SERVICE_PLAN="asp-sqlanalyzer-linux"
LOCATION="eastus"
VERCEL_TOKEN="3wRsfj8ZPCOe9CbvQf2qh6XA"
SKIP_INFRASTRUCTURE=false
SKIP_API=false
SKIP_WEB=false
PRODUCTION=false

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --resource-group)
            RESOURCE_GROUP="$2"
            shift 2
            ;;
        --app-name)
            APP_SERVICE_NAME="$2"
            shift 2
            ;;
        --location)
            LOCATION="$2"
            shift 2
            ;;
        --vercel-token)
            VERCEL_TOKEN="$2"
            shift 2
            ;;
        --skip-infrastructure)
            SKIP_INFRASTRUCTURE=true
            shift
            ;;
        --skip-api)
            SKIP_API=true
            shift
            ;;
        --skip-web)
            SKIP_WEB=true
            shift
            ;;
        --production)
            PRODUCTION=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

echo -e "${CYAN}SQL Analyzer - Azure Deployment (Local Credentials)${NC}"
echo -e "${CYAN}===================================================${NC}"
echo ""

# Load Azure credentials from local file
ENV_FILE="$(dirname "$0")/.env.azure.local"
if [ ! -f "$ENV_FILE" ]; then
    echo -e "${RED}ERROR: .env.azure.local file not found!${NC}"
    echo -e "${YELLOW}Please ensure the file exists with your Azure credentials.${NC}"
    exit 1
fi

echo -e "${YELLOW}Loading Azure credentials from local file...${NC}"

# Source the environment file
set -a
source "$ENV_FILE"
set +a

# Verify credentials are loaded
if [ -z "$AZURE_CLIENT_ID" ] || [ -z "$AZURE_CLIENT_SECRET" ] || [ -z "$AZURE_SUBSCRIPTION_ID" ] || [ -z "$AZURE_TENANT_ID" ]; then
    echo -e "${RED}ERROR: Missing required Azure credentials in .env.azure.local${NC}"
    exit 1
fi

# Login to Azure using service principal
echo -e "${YELLOW}Logging in to Azure with service principal...${NC}"
az login --service-principal \
    --username "$AZURE_CLIENT_ID" \
    --password "$AZURE_CLIENT_SECRET" \
    --tenant "$AZURE_TENANT_ID" \
    --output none

if [ $? -eq 0 ]; then
    echo -e "${GREEN}Successfully logged in to Azure${NC}"
else
    echo -e "${RED}ERROR: Failed to login to Azure${NC}"
    exit 1
fi

# Set subscription
az account set --subscription "$AZURE_SUBSCRIPTION_ID"

# Create Infrastructure
if [ "$SKIP_INFRASTRUCTURE" = false ]; then
    echo -e "\n${YELLOW}Creating Azure infrastructure...${NC}"
    
    # Create Resource Group
    echo -n "Creating resource group..."
    if az group show --name "$RESOURCE_GROUP" &> /dev/null; then
        echo -e " ${GREEN}✓ (exists)${NC}"
    else
        az group create \
            --name "$RESOURCE_GROUP" \
            --location "$LOCATION" \
            --output none
        echo -e " ${GREEN}✓${NC}"
    fi
    
    # Create App Service Plan
    echo -n "Creating App Service Plan (Basic B1)..."
    if az appservice plan show --name "$APP_SERVICE_PLAN" --resource-group "$RESOURCE_GROUP" &> /dev/null; then
        echo -e " ${GREEN}✓ (exists)${NC}"
    else
        az appservice plan create \
            --name "$APP_SERVICE_PLAN" \
            --resource-group "$RESOURCE_GROUP" \
            --location "$LOCATION" \
            --sku B1 \
            --is-linux \
            --output none
        echo -e " ${GREEN}✓${NC}"
    fi
    
    # Create Web App
    echo -n "Creating Web App..."
    if az webapp show --name "$APP_SERVICE_NAME" --resource-group "$RESOURCE_GROUP" &> /dev/null; then
        echo -e " ${GREEN}✓ (exists)${NC}"
    else
        az webapp create \
            --name "$APP_SERVICE_NAME" \
            --resource-group "$RESOURCE_GROUP" \
            --plan "$APP_SERVICE_PLAN" \
            --runtime "DOTNET|8.0" \
            --output none
        echo -e " ${GREEN}✓${NC}"
    fi
    
    # Configure App Settings
    echo -n "Configuring app settings..."
    az webapp config appsettings set \
        --name "$APP_SERVICE_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --settings \
            ASPNETCORE_ENVIRONMENT=Production \
            SqlAnalyzer__EnableCaching=true \
            SqlAnalyzer__DefaultTimeout=300 \
            SqlAnalyzer__MaxConcurrentAnalyses=10 \
            SqlAnalyzer__CircuitBreaker__FailureThreshold=5 \
            SqlAnalyzer__CircuitBreaker__OpenDurationSeconds=30 \
        --output none
    echo -e " ${GREEN}✓${NC}"
    
    # Configure CORS
    echo -n "Configuring CORS..."
    az webapp cors add \
        --name "$APP_SERVICE_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --allowed-origins \
            "https://sqlanalyzer.vercel.app" \
            "https://sqlanalyzer-web.vercel.app" \
            "http://localhost:5173" \
            "http://localhost:3000" \
        --output none
    echo -e " ${GREEN}✓${NC}"
    
    # Enable HTTPS Only
    echo -n "Enabling HTTPS only..."
    az webapp update \
        --name "$APP_SERVICE_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --https-only true \
        --output none
    echo -e " ${GREEN}✓${NC}"
fi

# Deploy API
if [ "$SKIP_API" = false ]; then
    echo -e "\n${YELLOW}Deploying API to Azure...${NC}"
    
    # Build and publish
    echo -n "Building API..."
    cd "$(dirname "$0")/src/SqlAnalyzer.Api"
    dotnet publish -c Release -o ./publish > /dev/null 2>&1
    echo -e " ${GREEN}✓${NC}"
    
    # Create ZIP
    echo -n "Creating deployment package..."
    cd publish
    zip -r ../deploy.zip . > /dev/null 2>&1
    cd ..
    echo -e " ${GREEN}✓${NC}"
    
    # Deploy
    echo -n "Deploying to Azure..."
    az webapp deployment source config-zip \
        --name "$APP_SERVICE_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --src deploy.zip \
        --output none
    echo -e " ${GREEN}✓${NC}"
    
    # Cleanup
    rm -f deploy.zip
    rm -rf ./publish
    cd ../..
    
    # Get API URL
    API_URL="https://$APP_SERVICE_NAME.azurewebsites.net"
    echo -e "\n${CYAN}API deployed to: $API_URL${NC}"
    
    # Test deployment
    echo -n "Testing API health endpoint..."
    sleep 30
    if curl -f "$API_URL/health" > /dev/null 2>&1; then
        echo -e " ${GREEN}✓${NC}"
    else
        echo -e " ${RED}✗${NC}"
        echo -e "${YELLOW}Health check failed. Please check the deployment.${NC}"
    fi
fi

# Deploy Web UI to Vercel
if [ "$SKIP_WEB" = false ]; then
    echo -e "\n${YELLOW}Deploying Web UI to Vercel...${NC}"
    
    # Check Vercel CLI
    echo -n "Checking Vercel CLI..."
    if command -v vercel &> /dev/null; then
        echo -e " ${GREEN}✓${NC}"
    else
        echo -e " ${RED}✗${NC}"
        echo -e "${YELLOW}Installing Vercel CLI...${NC}"
        npm install -g vercel
    fi
    
    # Build Web UI
    cd "$(dirname "$0")/src/SqlAnalyzer.Web"
    
    echo -n "Installing dependencies..."
    npm install --silent
    echo -e " ${GREEN}✓${NC}"
    
    echo -n "Building for production..."
    export VITE_API_URL="https://$APP_SERVICE_NAME.azurewebsites.net"
    export VITE_ENABLE_MOCK_DATA="false"
    npm run build > /dev/null 2>&1
    echo -e " ${GREEN}✓${NC}"
    
    # Deploy to Vercel
    echo -n "Deploying to Vercel..."
    export VERCEL_TOKEN="$VERCEL_TOKEN"
    
    if [ "$PRODUCTION" = true ]; then
        vercel --prod --token "$VERCEL_TOKEN" --yes > /dev/null 2>&1
    else
        vercel --token "$VERCEL_TOKEN" --yes > /dev/null 2>&1
    fi
    echo -e " ${GREEN}✓${NC}"
    
    cd ../..
fi

# Logout from Azure
az logout --output none

# Summary
echo -e "\n${CYAN}===================================================${NC}"
echo -e "${GREEN}Deployment Complete!${NC}"
echo -e "${CYAN}===================================================${NC}"
echo -e "\n${YELLOW}Resources:${NC}"
echo -e "- API URL: https://$APP_SERVICE_NAME.azurewebsites.net"
echo -e "- Health Check: https://$APP_SERVICE_NAME.azurewebsites.net/health"
echo -e "- Swagger UI: https://$APP_SERVICE_NAME.azurewebsites.net/swagger"
echo -e "- Web UI: https://sqlanalyzer.vercel.app (or check Vercel dashboard)"
echo -e "\n${YELLOW}Next Steps:${NC}"
echo -e "1. Add database connection string in Azure Portal"
echo -e "2. Configure custom domains if needed"
echo -e "3. Set up monitoring and alerts"
echo -e "4. Test the full integration"
echo -e "\n${RED}Security Note:${NC}"
echo -e "${RED}Keep your .env.azure.local file secure and never commit it to Git!${NC}"