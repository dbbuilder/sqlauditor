# Check GitHub Workflow Status

Write-Host "Checking GitHub Workflow Status..." -ForegroundColor Cyan
Write-Host "===================================" -ForegroundColor Cyan

# List recent workflow runs
Write-Host "`nRecent Workflow Runs:" -ForegroundColor Yellow
gh run list --limit 10

Write-Host "`n`nBuild and Test Workflow:" -ForegroundColor Yellow
gh run list --workflow "Build and Test" --limit 3

Write-Host "`n`nDeploy API Workflow:" -ForegroundColor Yellow
gh run list --workflow "Deploy API to Azure App Service" --limit 3

Write-Host "`n`nDeploy Frontend Workflow:" -ForegroundColor Yellow
gh run list --workflow "Deploy Frontend to Azure Static Web Apps" --limit 3

Write-Host "`n`nTo manually trigger deployment workflows:" -ForegroundColor Green
Write-Host "  Deploy API: gh workflow run 'Deploy API to Azure App Service'" -ForegroundColor White
Write-Host "  Deploy Frontend: gh workflow run 'Deploy Frontend to Azure Static Web Apps'" -ForegroundColor White