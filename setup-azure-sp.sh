#\!/bin/bash
# Create a service principal for GitHub Actions deployment

# Variables
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
RESOURCE_GROUP="rg-sqlanalyzer"
APP_NAME="sqlanalyzer-api-win"

# Create service principal with contributor role on the resource group
SP_NAME="github-actions-sqlanalyzer"

echo "Creating service principal..."
SP_JSON=$(az ad sp create-for-rbac \
  --name $SP_NAME \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth)

echo "Service Principal created successfully\!"
echo ""
echo "Add this JSON as a GitHub secret named 'AZURE_CREDENTIALS':"
echo ""
echo "$SP_JSON"
echo ""
echo "To add this secret:"
echo "1. Go to your GitHub repository"
echo "2. Settings > Secrets and variables > Actions"
echo "3. New repository secret"
echo "4. Name: AZURE_CREDENTIALS"
echo "5. Value: (paste the JSON above)"
