# Deploy using publish profile
$ErrorActionPreference = "Stop"

Write-Host "Deploying API to Azure..." -ForegroundColor Green

# Use the existing win-publish folder
$publishPath = "api/win-publish"

if (Test-Path $publishPath) {
    # Create zip from published files
    Write-Host "Creating deployment package..." -ForegroundColor Yellow
    Compress-Archive -Path "$publishPath/*" -DestinationPath "deploy.zip" -Force
    
    # Deploy using Azure CLI
    Write-Host "Deploying to Azure App Service..." -ForegroundColor Yellow
    az webapp deployment source config-zip `
        --resource-group rg-sqlanalyzer `
        --name sqlanalyzer-api-win `
        --src deploy.zip
    
    # Cleanup
    Remove-Item "deploy.zip" -Force
    
    Write-Host "Deployment initiated!" -ForegroundColor Green
    Write-Host "Check status at: https://sqlanalyzer-api-win.azurewebsites.net" -ForegroundColor Cyan
} else {
    Write-Host "Published files not found at $publishPath" -ForegroundColor Red
}