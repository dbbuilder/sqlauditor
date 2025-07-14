# Setting up Azure Credentials for GitHub Actions

## Create Service Principal

Run these commands in Azure CLI or Cloud Shell:

```bash
# Set your subscription
az account set --subscription "7b2beff3-b38a-4516-a75f-3216725cc4e9"

# Create service principal with contributor role
az ad sp create-for-rbac \
  --name "sp-sqlanalyzer-github-actions" \
  --role contributor \
  --scopes /subscriptions/7b2beff3-b38a-4516-a75f-3216725cc4e9 \
  --sdk-auth
```

This will output JSON credentials like:
```json
{
  "clientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "clientSecret": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "subscriptionId": "7b2beff3-b38a-4516-a75f-3216725cc4e9",
  "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

## Add to GitHub Secrets

1. Go to your GitHub repository
2. Navigate to Settings → Secrets and variables → Actions
3. Click "New repository secret"
4. Add the following secrets:

### Required Secrets:
- **AZURE_CREDENTIALS**: The entire JSON output from the service principal creation
- **VERCEL_TOKEN**: 3wRsfj8ZPCOe9CbvQf2qh6XA
- **VERCEL_ORG_ID**: (Get from Vercel dashboard or CLI)
- **VERCEL_PROJECT_ID**: (Get from Vercel dashboard or CLI)

## Get Vercel IDs

Run in your local project:
```bash
cd src/SqlAnalyzer.Web
vercel link
vercel env pull .env.vercel-ids
cat .env.vercel-ids
```

Copy the VERCEL_ORG_ID and VERCEL_PROJECT_ID values to GitHub Secrets.