# Secure Local Deployment Guide

This guide explains how to deploy SQL Analyzer to Azure using local credentials while keeping them secure from GitHub.

## Security Setup

### 1. Credentials File Created

Your Azure credentials are stored in `.env.azure.local` which is:
- ✅ Ignored by Git (added to .gitignore)
- ✅ Contains your service principal credentials
- ✅ Should NEVER be committed to GitHub

### 2. Protected Files

The following files contain sensitive information and are git-ignored:
- `.env.azure.local` - Azure service principal credentials
- `*.local.env` - Any local environment files
- `*-credentials.json` - Any credentials JSON files
- `azure-credentials.json` - Azure credentials in JSON format

## Local Deployment Scripts

Two scripts have been created for local deployment:

### PowerShell (Windows)
```powershell
.\deploy-azure-local.ps1
```

### Bash (Linux/Mac/WSL)
```bash
./deploy-azure-local.sh
```

Both scripts:
- Read credentials from `.env.azure.local`
- Login to Azure using service principal
- Create Azure infrastructure (Resource Group, App Service Plan, Web App)
- Deploy API to Azure App Service
- Deploy Web UI to Vercel
- Logout from Azure after completion

## Deployment Options

### Full Deployment
```bash
# Deploy everything (infrastructure + API + Web UI)
./deploy-azure-local.sh

# PowerShell
.\deploy-azure-local.ps1
```

### Partial Deployment
```bash
# Skip infrastructure creation (if already exists)
./deploy-azure-local.sh --skip-infrastructure

# Skip API deployment
./deploy-azure-local.sh --skip-api

# Skip Web UI deployment
./deploy-azure-local.sh --skip-web

# Production deployment to Vercel
./deploy-azure-local.sh --production
```

### Custom Resource Names
```bash
# Use custom resource names
./deploy-azure-local.sh \
  --resource-group "my-rg" \
  --app-name "my-api" \
  --location "westus"
```

## GitHub Actions Setup

For automated deployment via GitHub Actions:

1. **Add Secrets to GitHub** (Settings → Secrets → Actions):
   - `AZURE_CREDENTIALS`: The full JSON from `.env.azure.local`
   - `VERCEL_TOKEN`: 3wRsfj8ZPCOe9CbvQf2qh6XA
   - `VERCEL_ORG_ID`: Get from Vercel
   - `VERCEL_PROJECT_ID`: Get from Vercel

2. **Push to GitHub**:
   ```bash
   git add .
   git commit -m "Setup deployment (credentials excluded)"
   git push origin main
   ```

## Security Best Practices

### DO:
- ✅ Keep `.env.azure.local` in a secure location
- ✅ Use different credentials for dev/staging/production
- ✅ Rotate service principal secrets regularly
- ✅ Use Azure Key Vault for production secrets
- ✅ Enable Azure RBAC with least privilege
- ✅ Use managed identities when possible

### DON'T:
- ❌ Commit `.env.azure.local` to Git
- ❌ Share credentials in plain text
- ❌ Use the same credentials across environments
- ❌ Grant more permissions than needed
- ❌ Store credentials in code files

## Verifying Security

### Check Git Status
```bash
# Ensure credentials file is not tracked
git status --ignored

# Should show:
# .env.azure.local
```

### Check What's Committed
```bash
# List all tracked files
git ls-files | grep -E "(env|credential|secret)"

# Should return nothing
```

## Credential Rotation

To rotate your Azure credentials:

1. **Create New Service Principal**:
   ```bash
   az ad sp create-for-rbac \
     --name "sp-sqlanalyzer-new" \
     --role contributor \
     --scopes /subscriptions/7b2beff3-b38a-4516-a75f-3216725cc4e9
   ```

2. **Update `.env.azure.local`** with new credentials

3. **Update GitHub Secrets** if using GitHub Actions

4. **Delete Old Service Principal**:
   ```bash
   az ad sp delete --id [old-client-id]
   ```

## Troubleshooting

### Permission Denied
```bash
# Make script executable
chmod +x deploy-azure-local.sh
```

### Login Failed
```bash
# Test credentials manually
az login --service-principal \
  --username $AZURE_CLIENT_ID \
  --password $AZURE_CLIENT_SECRET \
  --tenant $AZURE_TENANT_ID
```

### Resource Already Exists
Use `--skip-infrastructure` flag or delete existing resources:
```bash
az group delete --name rg-sqlanalyzer --yes
```

## Next Steps

1. **Run Local Deployment**:
   ```bash
   ./deploy-azure-local.sh
   ```

2. **Verify Deployment**:
   - API: https://sqlanalyzer-api.azurewebsites.net
   - Web UI: https://sqlanalyzer.vercel.app

3. **Configure Database**:
   - Add connection string in Azure Portal
   - Test with your SQL Server database

4. **Monitor Resources**:
   - Azure Portal for API metrics
   - Vercel dashboard for Web UI analytics