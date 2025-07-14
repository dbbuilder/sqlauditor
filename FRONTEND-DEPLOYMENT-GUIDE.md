# Frontend Deployment Guide - SQL Analyzer Web

## Current Status

✅ **Frontend repository created** at `D:\Dev2\sqlanalyzer-web`
✅ **All frontend files copied** and committed
✅ **GitHub Actions workflow** configured
⏳ **Awaiting push to GitHub** and Azure configuration

## Step-by-Step Deployment Instructions

### 1. Create GitHub Repository

Go to: https://github.com/new

- **Repository name**: `sqlanalyzer-web`
- **Description**: SQL Analyzer Web Frontend - Vue.js application
- **Public/Private**: Your choice
- **DO NOT** initialize with README (we already have one)

### 2. Push Repository to GitHub

Run the push script:
```powershell
.\push-frontend-repo.ps1
```

Or manually:
```bash
cd D:\Dev2\sqlanalyzer-web
git remote add origin https://github.com/YOUR_USERNAME/sqlanalyzer-web.git
git push -u origin main
```

### 3. Get Deployment Token

The deployment token is needed for GitHub Actions to deploy to Azure.

```powershell
# Get token via Azure CLI
az staticwebapp secrets list `
    --name black-desert-02d93d30f `
    --resource-group rg-sqlanalyzer `
    --query "properties.apiKey" -o tsv
```

### 4. Add Token to GitHub Secrets

1. Go to: https://github.com/YOUR_USERNAME/sqlanalyzer-web/settings/secrets/actions
2. Click "New repository secret"
3. Name: `AZURE_STATIC_WEB_APPS_API_TOKEN`
4. Value: [paste deployment token]
5. Click "Add secret"

### 5. Configure Static Web App Source

Run the configuration script:
```powershell
.\configure-swa-deployment.ps1
```

Or manually in Azure Portal:
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to: `black-desert-02d93d30f`
3. Click "Deployment Center"
4. Configure:
   - Source: GitHub
   - Organization: Your GitHub username
   - Repository: sqlanalyzer-web
   - Branch: main
   - Build Presets: Vue.js
   - App location: `/`
   - Output location: `dist`

### 6. Deployment Will Start Automatically

Once configured, GitHub Actions will:
1. Build the Vue.js application
2. Deploy to Azure Static Web Apps
3. Make it available at: https://black-desert-02d93d30f.2.azurestaticapps.net

### 7. Monitor Deployment

Check deployment status:
```powershell
.\check-frontend-deployment.ps1
```

Or monitor at:
- GitHub Actions: https://github.com/YOUR_USERNAME/sqlanalyzer-web/actions
- Azure Portal: Static Web App overview page

## What's Included

### Frontend Features
- ✅ JWT Authentication with login/logout
- ✅ Connection string builder UI
- ✅ Toggle between builder and manual entry
- ✅ Support for SQL Server, PostgreSQL, MySQL
- ✅ Real-time analysis progress (SignalR ready)
- ✅ Fixed all console errors

### Repository Structure
```
sqlanalyzer-web/
├── src/                    # Vue.js source files
├── public/                 # Static assets
├── .github/workflows/      # GitHub Actions for deployment
├── package.json           # Dependencies
├── vite.config.js         # Build configuration
├── staticwebapp.config.json # Azure Static Web Apps config
└── README.md              # Documentation
```

### Environment Configuration
The build process sets:
- `VITE_API_URL`: https://sqlanalyzer-api.azurewebsites.net
- `VITE_ENABLE_MOCK_DATA`: false

## Testing After Deployment

1. Visit: https://black-desert-02d93d30f.2.azurestaticapps.net
2. You should see the login page immediately
3. Login with: `admin` / `AnalyzeThis!!`
4. Test the connection string builder
5. Verify no console errors

## Troubleshooting

### "Still the old version"
- Check GitHub Actions completed successfully
- Clear browser cache (Ctrl+F5)
- Wait 5-10 minutes for CDN propagation
- Verify in Azure Portal that deployment succeeded

### GitHub Push Fails
- Ensure repository exists on GitHub
- Check your GitHub credentials
- Verify repository name matches exactly

### Deployment Token Issues
- Token must be added as GitHub secret
- Name must be exactly: `AZURE_STATIC_WEB_APPS_API_TOKEN`
- No spaces or quotes around the token

### Build Failures
- Check GitHub Actions logs
- Ensure Node.js 18 compatibility
- Verify all dependencies in package.json

## Quick Commands

```powershell
# Push to GitHub
.\push-frontend-repo.ps1

# Configure Azure
.\configure-swa-deployment.ps1

# Check status
.\check-frontend-deployment.ps1
```

## Success Indicators

✅ GitHub repository shows your code
✅ GitHub Actions workflow runs without errors
✅ Azure Portal shows successful deployment
✅ Site loads with new connection builder UI
✅ No console errors in browser
✅ Login page appears immediately

The frontend will be fully deployed once all steps are completed!