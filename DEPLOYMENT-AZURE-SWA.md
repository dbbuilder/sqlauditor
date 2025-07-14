# Azure Static Web Apps Deployment Guide

## Overview

SQL Analyzer uses Azure Static Web Apps for hosting the Vue.js frontend UI. This provides:
- Global CDN distribution
- Automatic HTTPS
- Custom domain support
- GitHub Actions integration
- Free tier available

## Architecture

```
┌─────────────────────┐         ┌─────────────────────┐         ┌─────────────────┐
│ Azure Static Web    │         │ Azure App Service   │         │   Database      │
│      Apps (UI)      │ ──API──▶│    (.NET API)       │ ──SQL──▶│ (SQL Server)    │
│ sqlanalyzer-web.    │         │ sqlanalyzer-api.    │         │                 │
│ azurestaticapps.net │         │ azurewebsites.net   │         │                 │
└─────────────────────┘         └─────────────────────┘         └─────────────────┘
```

## Current Status

- **UI URL**: `https://sqlanalyzer-web.azurestaticapps.net` (to be created)
- **API URL**: `https://sqlanalyzer-api.azurewebsites.net` (already deployed)
- **Configuration**: Production uses real API, mock mode disabled

## Deployment Steps

### 1. Create Azure Static Web App

Run the deployment script:
```powershell
./deploy-azure-swa.ps1
```

Or manually:
```bash
# Create Static Web App
az staticwebapp create \
  --name sqlanalyzer-web \
  --resource-group rg-sqlanalyzer \
  --location eastus2 \
  --sku Free

# Get deployment token
az staticwebapp secrets list \
  --name sqlanalyzer-web \
  --resource-group rg-sqlanalyzer \
  --query "properties.apiKey" -o tsv
```

### 2. Configure GitHub Secrets

Add the deployment token to GitHub:
1. Go to Settings → Secrets → Actions
2. Add new secret: `AZURE_STATIC_WEB_APPS_API_TOKEN`
3. Paste the deployment token from step 1

### 3. Deploy via GitHub Actions

The deployment happens automatically on push to main/master:
```bash
git add .
git commit -m "Deploy UI to Azure Static Web Apps"
git push origin main
```

Or trigger manually:
```bash
gh workflow run deploy-swa.yml
```

### 4. Manual Deployment (Alternative)

```bash
# Install SWA CLI
npm install -g @azure/static-web-apps-cli

# Build the app
cd src/SqlAnalyzer.Web
npm install
npm run build

# Deploy
swa deploy ./dist \
  --deployment-token <your-token> \
  --env production
```

## Configuration

### Static Web App Configuration

The `staticwebapp.config.json` file configures:
- Routing rules (SPA fallback)
- Security headers
- API proxy (if needed)
- MIME types

### Environment Variables

Set in Azure portal or via CLI:
```bash
az staticwebapp appsettings set \
  --name sqlanalyzer-web \
  --resource-group rg-sqlanalyzer \
  --setting-names \
    VITE_API_URL=https://sqlanalyzer-api.azurewebsites.net \
    VITE_ENABLE_MOCK_DATA=false
```

### API CORS Configuration

The API is configured to accept requests from:
- `https://sqlanalyzer-web.azurestaticapps.net`
- `https://*.azurestaticapps.net`
- Local development URLs

## Custom Domain Setup

1. In Azure Portal → Static Web App → Custom domains
2. Add domain: `sqlanalyzer.yourdomain.com`
3. Configure DNS:
   ```
   CNAME  sqlanalyzer  sqlanalyzer-web.azurestaticapps.net
   ```

## Monitoring and Troubleshooting

### Check Deployment Status
```bash
# View recent deployments
az staticwebapp environment list \
  --name sqlanalyzer-web \
  --resource-group rg-sqlanalyzer

# View logs
az staticwebapp logs show \
  --name sqlanalyzer-web \
  --resource-group rg-sqlanalyzer
```

### Common Issues

1. **CORS Errors**
   - Check API CORS configuration includes SWA URL
   - Verify API is running and accessible

2. **404 Errors**
   - Check `staticwebapp.config.json` routing rules
   - Ensure SPA fallback is configured

3. **API Connection Failed**
   - Verify `VITE_API_URL` is set correctly
   - Check network tab in browser DevTools

## Cost Optimization

Azure Static Web Apps Free tier includes:
- 100 GB bandwidth per month
- 2 custom domains
- SSL certificates
- Global CDN

For production, consider:
- Standard tier for more bandwidth
- Azure Front Door for advanced routing
- Application Insights for monitoring

## Security Best Practices

1. **Content Security Policy**: Configure in `staticwebapp.config.json`
2. **Authentication**: Can add Azure AD, GitHub, etc.
3. **API Keys**: Never expose in frontend code
4. **HTTPS Only**: Enforced by default

## Rollback Procedure

1. **Via GitHub Actions**: Rerun previous workflow
2. **Via Azure Portal**: Swap deployment slots
3. **Via CLI**: Deploy previous build

## Next Steps

1. Run `./deploy-azure-swa.ps1` to create Static Web App
2. Add GitHub secret for deployment token
3. Push code to trigger deployment
4. Verify at `https://sqlanalyzer-web.azurestaticapps.net`
5. Configure custom domain if needed