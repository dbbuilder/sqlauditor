# Clean deployment script for API

Write-Host "Stopping the API app service..." -ForegroundColor Yellow
az webapp stop --name sqlanalyzer-api --resource-group rg-sqlanalyzer

Write-Host "Cleaning deployment folder..." -ForegroundColor Yellow
az webapp deploy --resource-group rg-sqlanalyzer --name sqlanalyzer-api --src-path empty.zip --type zip --clean true 2>$null

Write-Host "Starting the API app service..." -ForegroundColor Yellow
az webapp start --name sqlanalyzer-api --resource-group rg-sqlanalyzer

Write-Host "Triggering GitHub deployment..." -ForegroundColor Yellow
gh workflow run deploy-api.yml

Write-Host "Deployment initiated!" -ForegroundColor Green