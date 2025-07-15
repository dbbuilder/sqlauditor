#!/bin/bash
# Setup Azure DevOps for SQL Analyzer

echo -e "\033[33mSetting up Azure DevOps for SQL Analyzer...\033[0m"

# Variables
ORG_URL="https://dev.azure.com/sqlanalyzer"
PROJECT_NAME="SqlAnalyzer"
RESOURCE_GROUP="rg-sqlanalyzer"

# Create organization if needed
echo -e "\n\033[36mChecking Azure DevOps organization...\033[0m"
if ! az devops project list --org $ORG_URL &>/dev/null; then
    echo "Creating new Azure DevOps organization..."
    # Open browser to create org
    echo "Please create the organization 'sqlanalyzer' at: https://dev.azure.com"
    read -p "Press Enter when done..."
fi

# Create project
echo -e "\n\033[36mCreating Azure DevOps project...\033[0m"
az devops project create \
    --name $PROJECT_NAME \
    --description "SQL Analyzer - Database Analysis Tool" \
    --org $ORG_URL || echo "Project might already exist"

# Set defaults
az devops configure --defaults organization=$ORG_URL project=$PROJECT_NAME

# Get Static Web App token
echo -e "\n\033[36mRetrieving Static Web App deployment token...\033[0m"
STATIC_TOKEN=$(az staticwebapp secrets list \
    --name sqlanalyzer-frontend \
    --resource-group $RESOURCE_GROUP \
    --query "properties.apiKey" -o tsv)

echo "Static Web App Token: ${STATIC_TOKEN:0:10}..."

# Create service connection JSON
echo -e "\n\033[36mCreating service connection configuration...\033[0m"
cat > azure-service-connection.json << EOF
{
  "name": "SqlAnalyzer-ServiceConnection",
  "type": "azurerm",
  "url": "https://management.azure.com/",
  "data": {
    "subscriptionId": "$(az account show --query id -o tsv)",
    "subscriptionName": "$(az account show --query name -o tsv)",
    "environment": "AzureCloud",
    "scopeLevel": "Subscription",
    "creationMode": "Automatic"
  },
  "authorization": {
    "scheme": "ServicePrincipal",
    "parameters": {
      "authenticationType": "spnKey"
    }
  }
}
EOF

# Instructions for manual steps
echo -e "\n\033[33m=== MANUAL STEPS REQUIRED ===\033[0m"
echo -e "\033[32m1. Create Azure service connection:\033[0m"
echo "   - Go to: $ORG_URL/$PROJECT_NAME/_settings/adminservices"
echo "   - Click 'New service connection' > 'Azure Resource Manager'"
echo "   - Choose 'Service principal (automatic)'"
echo "   - Select subscription and name it: SqlAnalyzer-ServiceConnection"

echo -e "\n\033[32m2. Add pipeline variable:\033[0m"
echo "   - Go to: $ORG_URL/$PROJECT_NAME/_library"
echo "   - Create variable group: SqlAnalyzer-Variables"
echo "   - Add variable: AZURE_STATIC_WEB_APPS_API_TOKEN = $STATIC_TOKEN"
echo "   - Mark as secret"

echo -e "\n\033[32m3. Create pipeline:\033[0m"
echo "   - Go to: $ORG_URL/$PROJECT_NAME/_build"
echo "   - Click 'New pipeline'"
echo "   - Select GitHub > dbbuilder/sqlauditor"
echo "   - Choose 'Existing Azure Pipelines YAML file'"
echo "   - Select '/azure-pipelines.yml'"

echo -e "\n\033[32m4. Authorize GitHub connection when prompted\033[0m"

# Create a direct pipeline import URL
IMPORT_URL="$ORG_URL/$PROJECT_NAME/_build?definitionPath=%2F&path=%2F&repository=dbbuilder%2Fsqlauditor&type=github"
echo -e "\n\033[36mDirect pipeline import URL:\033[0m"
echo $IMPORT_URL

# Clean up
rm -f azure-service-connection.json