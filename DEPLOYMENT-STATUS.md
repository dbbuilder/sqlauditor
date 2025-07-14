# SQL Analyzer Deployment Status

## Current Status

### Infrastructure ✅
- **Resource Group**: `rg-sqlanalyzer` (East US 2)
- **App Service**: `sqlanalyzer-api` (Linux, .NET 8.0)
- **App Service Plan**: `asp-sqlanalyzer-linux` (Basic B1)
- **URL**: https://sqlanalyzer-api.azurewebsites.net

### API Deployment 🚧
- API builds successfully locally
- Infrastructure is ready and running
- Manual deployment via Azure CLI encountered issues
- GitHub Actions workflow created for CI/CD

## Next Steps

1. **Get Publish Profile**:
   ```bash
   az webapp deployment list-publishing-profiles \
     --name sqlanalyzer-api \
     --resource-group rg-sqlanalyzer \
     --xml
   ```
   Save the output as a GitHub secret named `AZURE_WEBAPP_PUBLISH_PROFILE`

2. **Configure App Settings**:
   ```bash
   az webapp config appsettings set \
     --name sqlanalyzer-api \
     --resource-group rg-sqlanalyzer \
     --settings \
       ASPNETCORE_ENVIRONMENT=Production \
       SqlAnalyzer__AllowedOrigins__0=https://sqlanalyzer-web.vercel.app
   ```

3. **Deploy via GitHub Actions**:
   - Push code to main/master branch
   - GitHub Actions will automatically build and deploy

## Manual Deployment (Alternative)

If you need to deploy manually:

```powershell
# Build
dotnet publish src/SqlAnalyzer.Api/SqlAnalyzer.Api.csproj -c Release -o ./publish/api

# Create ZIP
Compress-Archive -Path ./publish/api/* -DestinationPath api.zip -Force

# Deploy via Azure CLI
az webapp deploy --resource-group rg-sqlanalyzer --name sqlanalyzer-api --src-path api.zip --type zip
```

## Troubleshooting

### If deployment fails:
1. Check deployment logs:
   ```bash
   az webapp log tail --name sqlanalyzer-api --resource-group rg-sqlanalyzer
   ```

2. Check application logs:
   ```bash
   az webapp log download --name sqlanalyzer-api --resource-group rg-sqlanalyzer
   ```

3. Restart the app service:
   ```bash
   az webapp restart --name sqlanalyzer-api --resource-group rg-sqlanalyzer
   ```

## Security Notes

- Azure credentials are stored in `.env.azure.local` (excluded from Git)
- Use GitHub secrets for CI/CD credentials
- Never commit credentials to the repository