# How to Create a Service Principal for Azure DevOps

## Method 1: Azure Portal (Easiest)

1. **Go to Azure Portal**
   - Navigate to: https://portal.azure.com
   - Search for "App registrations" in the top search bar

2. **Create App Registration**
   - Click "New registration"
   - Name: `sp-sqlanalyzer-devops`
   - Account types: "Accounts in this organizational directory only"
   - Click "Register"

3. **Create Client Secret**
   - In your new app registration, go to "Certificates & secrets"
   - Click "New client secret"
   - Description: `devops-secret`
   - Expires: Choose your preference (e.g., 12 months)
   - Click "Add"
   - **COPY THE SECRET VALUE NOW** (you can't see it again!)

4. **Grant Permissions**
   - Go to your subscription: Portal Home > Subscriptions > Your Subscription
   - Click "Access control (IAM)"
   - Click "Add" > "Add role assignment"
   - Role: "Contributor"
   - Members: Search for your app name `sp-sqlanalyzer-devops`
   - Click "Review + assign"

5. **Collect Information**
   From the app registration overview page, note:
   - Application (client) ID
   - Directory (tenant) ID
   - Your subscription ID
   - The secret you copied earlier

## Method 2: Azure CLI (If You Have Permissions)

```bash
# Check if you have permissions
az ad sp create-for-rbac --name test-permission-check --skip-assignment

# If successful, delete the test and create the real one
az ad sp delete --id <app-id-from-above>

# Create the service principal
az ad sp create-for-rbac \
    --name "sp-sqlanalyzer-devops" \
    --role Contributor \
    --scopes "/subscriptions/$(az account show --query id -o tsv)"
```

## Method 3: Ask Azure Admin

If you get "Insufficient privileges" errors, ask your Azure admin to:

1. Create a service principal with Contributor access to your subscription
2. Provide you with:
   - Client ID (Application ID)
   - Client Secret
   - Tenant ID
   - Subscription ID

## Using the Service Principal in Azure DevOps

Once you have the service principal details:

1. Go to: https://dev.azure.com/dbbuilder-dev/SQLAnalyzer/_settings/adminservices
2. Click "New service connection" → "Azure Resource Manager"
3. Choose "Service principal (manual)" (link at bottom)
4. Enter:
   - Subscription ID: `<your-subscription-id>`
   - Subscription Name: `<your-subscription-name>`
   - Service Principal ID: `<client-id>`
   - Service principal key: `<client-secret>`
   - Tenant ID: `<tenant-id>`
   - Service connection name: `SqlAnalyzer-ServiceConnection`
5. Click "Verify" (should show green checkmark)
6. Check "Grant access permission to all pipelines"
7. Click "Save"

## Alternative: Use Azure DevOps Automatic Mode

If manual creation fails, in Azure DevOps:

1. Click "New service connection" → "Azure Resource Manager"
2. Keep "Service principal (automatic)" selected
3. Sign in with your Azure account when prompted
4. Select your subscription
5. Leave Resource Group empty (for full subscription access)
6. Service connection name: `SqlAnalyzer-ServiceConnection`
7. Check "Grant access permission to all pipelines"
8. Save

This method creates the service principal automatically if you have permissions.