#!/bin/bash
# Create Azure Service Connection for SQL Analyzer

echo "Creating Azure Service Connection for SQL Analyzer..."

# Variables
ORG_URL="https://dev.azure.com/dbbuilder-dev"
PROJECT_NAME="SQLAnalyzer"
SERVICE_CONNECTION_NAME="SqlAnalyzer-ServiceConnection"

# Set defaults
az devops configure --defaults organization=$ORG_URL project=$PROJECT_NAME

# Get subscription details
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
SUBSCRIPTION_NAME=$(az account show --query name -o tsv)
TENANT_ID=$(az account show --query tenantId -o tsv)

echo "Using subscription: $SUBSCRIPTION_NAME ($SUBSCRIPTION_ID)"

# Create service endpoint
echo "Creating service connection..."
SERVICE_ENDPOINT_ID=$(az devops service-endpoint azurerm create \
    --azure-rm-service-principal-id "" \
    --azure-rm-subscription-id "$SUBSCRIPTION_ID" \
    --azure-rm-subscription-name "$SUBSCRIPTION_NAME" \
    --azure-rm-tenant-id "$TENANT_ID" \
    --name "$SERVICE_CONNECTION_NAME" \
    --org "$ORG_URL" \
    --project "$PROJECT_NAME" \
    --query id -o tsv)

if [ $? -eq 0 ]; then
    echo "Service connection created successfully!"
    echo "Service Endpoint ID: $SERVICE_ENDPOINT_ID"
    
    # Update service endpoint to grant access to all pipelines
    az devops service-endpoint update \
        --id "$SERVICE_ENDPOINT_ID" \
        --org "$ORG_URL" \
        --project "$PROJECT_NAME" \
        --enable-for-all true
    
    echo "Service connection '$SERVICE_CONNECTION_NAME' is now available for all pipelines"
else
    echo "Failed to create service connection. You may need to:"
    echo "1. Ensure you're logged in to Azure DevOps: az devops login"
    echo "2. Check that you have permissions to create service connections"
    echo "3. Try the manual approach in the web interface"
fi