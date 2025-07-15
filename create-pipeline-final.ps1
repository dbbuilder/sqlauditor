# Final Pipeline Creation Script
Write-Host "Azure DevOps Pipeline Setup" -ForegroundColor Yellow
Write-Host "==========================" -ForegroundColor Yellow

$org = "dbbuilder-dev"
$project = "SQLAnalyzer"

Write-Host "`nYou have completed:" -ForegroundColor Green
Write-Host "✓ Service Connection: SqlAnalyzer-ServiceConnection" -ForegroundColor Green
Write-Host "✓ Variable Group: SqlAnalyzer-Variables (with Static Web App token)" -ForegroundColor Green
Write-Host "✓ GitHub Connection: GitHub-SqlAnalyzer" -ForegroundColor Green
Write-Host "✓ GitHub PAT: Available" -ForegroundColor Green

Write-Host "`nFinal Step - Create Pipeline:" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan

Write-Host "`nOption 1: Direct Link (Recommended)" -ForegroundColor Yellow
$createUrl = "https://dev.azure.com/$org/$project/_build/create?preferredRepositoryType=github&preferredRepository=dbbuilder%2Fsqlauditor"
Write-Host "Click here: $createUrl" -ForegroundColor White
Write-Host "`nThen:" -ForegroundColor Yellow
Write-Host "1. The repository should auto-select to 'dbbuilder/sqlauditor'" -ForegroundColor White
Write-Host "2. Click Continue" -ForegroundColor White
Write-Host "3. Select 'Existing Azure Pipelines YAML file'" -ForegroundColor White
Write-Host "4. Branch: master, Path: /azure-pipelines.yml" -ForegroundColor White
Write-Host "5. Click Continue" -ForegroundColor White
Write-Host "6. IMPORTANT: Click Variables → Variable groups → Link 'SqlAnalyzer-Variables'" -ForegroundColor Red
Write-Host "7. Click Run" -ForegroundColor White

Write-Host "`nOption 2: Manual Steps" -ForegroundColor Yellow
Write-Host "1. Go to: https://dev.azure.com/$org/$project/_build" -ForegroundColor White
Write-Host "2. Click 'New pipeline'" -ForegroundColor White
Write-Host "3. Select 'GitHub' (should show as already authorized)" -ForegroundColor White
Write-Host "4. Select 'dbbuilder/sqlauditor'" -ForegroundColor White
Write-Host "5. Select 'Existing Azure Pipelines YAML file'" -ForegroundColor White
Write-Host "6. Branch: master, Path: /azure-pipelines.yml" -ForegroundColor White
Write-Host "7. Continue and link variable group before running" -ForegroundColor White

Write-Host "`nAfter Pipeline Creation:" -ForegroundColor Cyan
Write-Host "The pipeline will automatically:" -ForegroundColor Yellow
Write-Host "- Build the .NET 8 API" -ForegroundColor White
Write-Host "- Build the Vue.js frontend" -ForegroundColor White
Write-Host "- Deploy API to: https://sqlanalyzer-api-win.azurewebsites.net" -ForegroundColor White
Write-Host "- Deploy Frontend to: https://sqlanalyzer-web.azureedge.net" -ForegroundColor White

# Start the browser
Write-Host "`nOpening browser..." -ForegroundColor Green
Start-Process $createUrl