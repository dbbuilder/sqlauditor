# Simple API deployment script for SQL Analyzer
param(
    [string]$ResourceGroup = "rg-sqlanalyzer",
    [string]$AppServiceName = "sqlanalyzer-api"
)

Write-Host "Building SQL Analyzer API..." -ForegroundColor Cyan
dotnet publish src/SqlAnalyzer.Api/SqlAnalyzer.Api.csproj -c Release -o ./publish/api

Write-Host "Creating deployment package..." -ForegroundColor Cyan
Compress-Archive -Path ./publish/api/* -DestinationPath api.zip -Force

Write-Host "Deploying to Azure App Service..." -ForegroundColor Cyan
az webapp deploy --resource-group $ResourceGroup --name $AppServiceName --src-path api.zip --type zip

Write-Host "Deployment complete!" -ForegroundColor Green
Write-Host "API URL: https://$AppServiceName.azurewebsites.net" -ForegroundColor Yellow