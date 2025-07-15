# Final Azure DevOps Pipeline Setup

You've successfully completed:
✅ Created service connection
✅ Created variable group with Static Web App token
✅ Configured PAT authentication

## Last Step: Create Pipeline

Since the CLI requires an interactive GitHub connection, please use the web interface:

### Direct Link to Create Pipeline:
https://dev.azure.com/dbbuilder-dev/SQLAnalyzer/_build/create

### Steps:
1. Click the link above
2. Select **"GitHub"**
3. You'll be prompted to authorize Azure Pipelines to access GitHub - click **"Authorize"**
4. Select repository: **dbbuilder/sqlauditor**
5. Click **"Continue"**
6. Select **"Existing Azure Pipelines YAML file"**
7. Branch: **master**
8. Path: **/azure-pipelines.yml**
9. Click **"Continue"**
10. Click **"Variables"** → **"Variable groups"** → **"Link variable group"**
11. Select **"SqlAnalyzer-Variables"** and click **"Link"**
12. Click **"Run"** to start the deployment!

## Alternative: Use GitHub App

If the above doesn't work, you can install the Azure Pipelines GitHub App:
1. Go to: https://github.com/marketplace/azure-pipelines
2. Click "Set up a plan" → Choose "Free"
3. Select "dbbuilder" organization
4. Grant access to "sqlauditor" repository
5. Return to Azure DevOps and retry creating the pipeline

## Manual Pipeline Run Command (after creation):
```bash
# List pipelines
az pipelines list --org "https://dev.azure.com/dbbuilder-dev" --project "SQLAnalyzer"

# Run specific pipeline
az pipelines run --id <pipeline-id> --org "https://dev.azure.com/dbbuilder-dev" --project "SQLAnalyzer"
```

## Monitor Deployment:
- API deployment: https://sqlanalyzer-api-win.azurewebsites.net
- Frontend: https://sqlanalyzer-web.azureedge.net