#!/bin/bash
# Create Service Principal for Azure DevOps Service Connection

echo "Creating Service Principal for SQL Analyzer DevOps..."

# Create service principal with contributor access
SP_NAME="sp-sqlanalyzer-devops-$(date +%s)"
echo "Creating service principal: $SP_NAME"

# Create SP and capture output
SP_OUTPUT=$(az ad sp create-for-rbac \
    --name "$SP_NAME" \
    --role Contributor \
    --scopes "/subscriptions/$(az account show --query id -o tsv)" \
    --query "{clientId:appId, clientSecret:password, tenantId:tenant}" -o json)

if [ $? -eq 0 ]; then
    # Parse the output
    CLIENT_ID=$(echo $SP_OUTPUT | jq -r '.clientId')
    CLIENT_SECRET=$(echo $SP_OUTPUT | jq -r '.clientSecret')
    TENANT_ID=$(echo $SP_OUTPUT | jq -r '.tenantId')
    SUBSCRIPTION_ID=$(az account show --query id -o tsv)
    SUBSCRIPTION_NAME=$(az account show --query name -o tsv)
    
    echo ""
    echo "=========================================="
    echo "Service Principal Created Successfully!"
    echo "=========================================="
    echo ""
    echo "Use these values in Azure DevOps:"
    echo ""
    echo "1. Go to: https://dev.azure.com/dbbuilder-dev/SQLAnalyzer/_settings/adminservices"
    echo "2. Click 'New service connection' → 'Azure Resource Manager'"
    echo "3. Choose 'Service principal (manual)' at the bottom"
    echo "4. Enter these values:"
    echo ""
    echo "   Subscription ID:        $SUBSCRIPTION_ID"
    echo "   Subscription Name:      $SUBSCRIPTION_NAME"
    echo "   Service Principal ID:   $CLIENT_ID"
    echo "   Service Principal Key:  $CLIENT_SECRET"
    echo "   Tenant ID:             $TENANT_ID"
    echo "   Service Connection Name: SqlAnalyzer-ServiceConnection"
    echo ""
    echo "5. Click 'Verify' then 'Save'"
    echo ""
    
    # Save to file for reference
    cat > service-principal-details.txt << EOF
Azure DevOps Service Connection Details
Created: $(date)

Service Connection Name: SqlAnalyzer-ServiceConnection
Subscription ID: $SUBSCRIPTION_ID
Subscription Name: $SUBSCRIPTION_NAME
Service Principal ID: $CLIENT_ID
Service Principal Key: $CLIENT_SECRET
Tenant ID: $TENANT_ID

Portal URL: https://dev.azure.com/dbbuilder-dev/SQLAnalyzer/_settings/adminservices
EOF
    
    echo "Details also saved to: service-principal-details.txt"
    echo ""
    echo "IMPORTANT: Keep the service principal key secure!"
    
else
    echo "Failed to create service principal"
    echo "Make sure you're logged in to Azure: az login"
fi