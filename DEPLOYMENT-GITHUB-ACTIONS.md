# GitHub Actions Deployment Guide

This guide walks through setting up automated deployment to Azure App Service (API) and Vercel (Web UI) using GitHub Actions.

## Prerequisites

1. Azure subscription with ID: `7b2beff3-b38a-4516-a75f-3216725cc4e9`
2. Vercel account with token: `3wRsfj8ZPCOe9CbvQf2qh6XA`
3. GitHub repository with the SQL Analyzer code

## Step 1: Create Azure Service Principal

Run these commands in Azure Cloud Shell or Azure CLI:

```bash
# Set your subscription
az account set --subscription "7b2beff3-b38a-4516-a75f-3216725cc4e9"

# Create service principal
az ad sp create-for-rbac \
  --name "sp-sqlanalyzer-github-actions" \
  --role contributor \
  --scopes /subscriptions/7b2beff3-b38a-4516-a75f-3216725cc4e9 \
  --sdk-auth
```

Save the JSON output - you'll need it for GitHub secrets.

## Step 2: Get Vercel Project IDs

```bash
cd src/SqlAnalyzer.Web
npx vercel link
npx vercel env pull .env.vercel-ids
cat .env.vercel-ids
```

Note the `VERCEL_ORG_ID` and `VERCEL_PROJECT_ID` values.

## Step 3: Add GitHub Secrets

Go to your GitHub repository → Settings → Secrets and variables → Actions

Add these repository secrets:

| Secret Name | Value |
|-------------|-------|
| AZURE_CREDENTIALS | The entire JSON output from Step 1 |
| VERCEL_TOKEN | 3wRsfj8ZPCOe9CbvQf2qh6XA |
| VERCEL_ORG_ID | Your Vercel organization ID from Step 2 |
| VERCEL_PROJECT_ID | Your Vercel project ID from Step 2 |

## Step 4: GitHub Actions Workflow

The workflow file `.github/workflows/deploy-azure.yml` is already created and will:

1. **Build and Test**: Run tests on every push
2. **Deploy Infrastructure**: Create Azure resources (only on main branch)
3. **Deploy API**: Deploy to Azure App Service
4. **Deploy Web UI**: Deploy to Vercel
5. **Summary**: Provide deployment URLs

## Step 5: Trigger Deployment

### Option A: Push to main branch
```bash
git add .
git commit -m "Setup GitHub Actions deployment"
git push origin main
```

### Option B: Manual trigger
1. Go to Actions tab in GitHub
2. Select "Deploy to Azure" workflow
3. Click "Run workflow"
4. Select branch and click "Run workflow"

## Step 6: Monitor Deployment

1. Go to GitHub Actions tab
2. Click on the running workflow
3. Monitor each job's progress
4. Check the summary for deployment URLs

## Deployment URLs

After successful deployment:
- **API**: https://sqlanalyzer-api.azurewebsites.net
- **API Health**: https://sqlanalyzer-api.azurewebsites.net/health
- **API Swagger**: https://sqlanalyzer-api.azurewebsites.net/swagger
- **Web UI**: https://sqlanalyzer.vercel.app

## Troubleshooting

### Azure Login Failed
```
Error: Login failed due to invalid credentials
```
**Solution**: Recreate service principal and update AZURE_CREDENTIALS secret

### Vercel Deployment Failed
```
Error: Invalid token
```
**Solution**: Verify VERCEL_TOKEN is correct and hasn't expired

### Resource Already Exists
```
Error: The resource 'sqlanalyzer-api' already exists
```
**Solution**: Either delete existing resources or use different names

### Health Check Failed
```
Error: API health endpoint returned non-200 status
```
**Solution**: Check Azure Portal logs for startup errors

## Customization

### Change Resource Names
Edit these in `.github/workflows/deploy-azure.yml`:
```yaml
env:
  AZURE_WEBAPP_NAME: your-api-name
  AZURE_RESOURCE_GROUP: your-resource-group
  AZURE_LOCATION: your-location
```

### Add Database Connection
After deployment, add connection string in Azure Portal:
1. Go to App Service → Configuration
2. Add new connection string
3. Name: `DefaultConnection`
4. Value: Your SQL Server connection string
5. Type: `SQLAzure` or `SQLServer`

### Environment-Specific Deployment
Create separate workflows for staging/production:
- `.github/workflows/deploy-staging.yml`
- `.github/workflows/deploy-production.yml`

## Cost Optimization

### Azure
- **App Service Plan**: B1 (~$13/month)
- **Alternative**: F1 Free tier (limited features)
- **Scale down**: Use Azure Functions for serverless

### Vercel
- **Free Tier**: Sufficient for most use cases
- **Pro**: $20/month for team features

## Security Best Practices

1. **Secrets**: Never commit secrets to repository
2. **Permissions**: Use least-privilege service principal
3. **HTTPS**: Always enabled by default
4. **CORS**: Configured for specific origins only
5. **Authentication**: Add before production use

## Rollback Procedure

### API Rollback (Azure)
```bash
# List deployments
az webapp deployment list \
  --name sqlanalyzer-api \
  --resource-group rg-sqlanalyzer

# Rollback to previous
az webapp deployment rollback \
  --name sqlanalyzer-api \
  --resource-group rg-sqlanalyzer
```

### Web UI Rollback (Vercel)
```bash
# In Vercel dashboard or CLI
vercel rollback
```

## Manual Deployment Alternative

If you prefer manual deployment:

### PowerShell (Windows)
```powershell
.\deploy-azure.ps1
```

### Bash (Linux/Mac)
```bash
./deploy-azure.sh
```

Both scripts support flags:
- `--production`: Deploy to production
- `--skip-infrastructure`: Skip Azure resource creation
- `--skip-api`: Skip API deployment
- `--skip-web`: Skip Web UI deployment