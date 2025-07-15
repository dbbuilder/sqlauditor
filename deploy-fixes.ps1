# Deploy fixes for API and Frontend

Write-Host "Deploying API fixes..." -ForegroundColor Yellow

# Deploy API using GitHub Actions
gh workflow run deploy-api.yml

Write-Host "API deployment triggered" -ForegroundColor Green

# Wait a moment for workflows to start
Start-Sleep -Seconds 5

# Show workflow status
Write-Host "`nChecking deployment status..." -ForegroundColor Yellow
gh run list --workflow=deploy-api.yml --limit=3

Write-Host "`nFrontend will deploy automatically via GitHub Actions" -ForegroundColor Cyan
gh run list --workflow=deploy-frontend.yml --limit=3

Write-Host "`nDeployment process initiated. Check GitHub Actions for progress." -ForegroundColor Green
Write-Host "Frontend URL: https://black-desert-02d93d30f.2.azurestaticapps.net" -ForegroundColor Cyan
Write-Host "API URL: https://sqlanalyzer-api.azurewebsites.net" -ForegroundColor Cyan