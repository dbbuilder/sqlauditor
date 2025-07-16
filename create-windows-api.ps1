# Create Windows-based API App Service
# Start time: 9:46 PM Pacific
# Expected completion: 9:48 PM Pacific

$resourceGroup = "rg-sqlanalyzer"
$apiName = "sqlanalyzer-api-win"
$location = "eastus2"

Write-Host "Creating Windows App Service Plan..." -ForegroundColor Yellow
az appservice plan create `
    --name asp-sqlanalyzer-win `
    --resource-group $resourceGroup `
    --location $location `
    --sku B1 `
    --is-windows

Write-Host "Creating Windows Web App..." -ForegroundColor Yellow
az webapp create `
    --resource-group $resourceGroup `
    --plan asp-sqlanalyzer-win `
    --name $apiName `
    --runtime "DOTNET:8"

Write-Host "Configuring app settings..." -ForegroundColor Yellow
$JwtSecret = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))

az webapp config appsettings set `
    --name $apiName `
    --resource-group $resourceGroup `
    --settings `
    "Authentication__JwtSecret=$JwtSecret" `
    "Authentication__DefaultUsername=admin" `
    "Authentication__DefaultPassword=AnalyzeThis!!" `
    "Authentication__JwtExpirationHours=24" `
    "ASPNETCORE_ENVIRONMENT=Production"

Write-Host "Configuring CORS..." -ForegroundColor Yellow
az webapp cors add `
    --resource-group $resourceGroup `
    --name $apiName `
    --allowed-origins "https://black-desert-02d93d30f.2.azurestaticapps.net" "http://localhost:5173"

Write-Host "Getting publish profile..." -ForegroundColor Yellow
az webapp deployment list-publishing-profiles `
    --name $apiName `
    --resource-group $resourceGroup `
    --xml | Out-File -FilePath "publish-profile-win.xml" -Encoding UTF8

Write-Host "`nCompleted at $(Get-Date -Format 'h:mm tt') Pacific" -ForegroundColor Green
Write-Host "New Windows API URL: https://$apiName.azurewebsites.net" -ForegroundColor Cyan