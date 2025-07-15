# Create a new API app service to replace the broken one
# Expected completion time: 9:31 PM Pacific (3 minutes from 9:28 PM)

$resourceGroup = "rg-sqlanalyzer"
$newApiName = "sqlanalyzer-api-v2"
$location = "eastus2"
$appServicePlan = "asp-sqlanalyzer"

Write-Host "Starting at $(Get-Date -Format 'h:mm tt') Pacific" -ForegroundColor Cyan
Write-Host "Expected completion: 9:31 PM Pacific" -ForegroundColor Cyan

# Step 1: Create new App Service (ETA: 9:29 PM Pacific)
Write-Host "`nCreating new App Service: $newApiName..." -ForegroundColor Yellow
az webapp create `
    --resource-group $resourceGroup `
    --plan $appServicePlan `
    --name $newApiName `
    --runtime "DOTNET:8.0" `
    --deployment-local-git

# Step 2: Configure app settings (ETA: 9:30 PM Pacific)
Write-Host "`nConfiguring authentication settings..." -ForegroundColor Yellow
$JwtSecret = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))

az webapp config appsettings set `
    --name $newApiName `
    --resource-group $resourceGroup `
    --settings `
    "Authentication__JwtSecret=$JwtSecret" `
    "Authentication__DefaultUsername=admin" `
    "Authentication__DefaultPassword=AnalyzeThis!!" `
    "Authentication__JwtExpirationHours=24" `
    "SqlAnalyzer__AllowedOrigins__0=https://black-desert-02d93d30f.2.azurestaticapps.net" `
    "SqlAnalyzer__AllowedOrigins__1=https://sqlanalyzer-web.azurestaticapps.net" `
    "SqlAnalyzer__AllowedOrigins__2=http://localhost:5173" `
    "ASPNETCORE_ENVIRONMENT=Production"

# Step 3: Enable CORS (ETA: 9:30 PM Pacific)
Write-Host "`nConfiguring CORS..." -ForegroundColor Yellow
az webapp cors add `
    --resource-group $resourceGroup `
    --name $newApiName `
    --allowed-origins "https://black-desert-02d93d30f.2.azurestaticapps.net" "https://sqlanalyzer-web.azurestaticapps.net" "http://localhost:5173"

# Step 4: Get publish profile (ETA: 9:31 PM Pacific)
Write-Host "`nGetting publish profile..." -ForegroundColor Yellow
$publishProfile = az webapp deployment list-publishing-profiles `
    --name $newApiName `
    --resource-group $resourceGroup `
    --xml

# Save to file for GitHub secret
$publishProfile | Out-File -FilePath "publish-profile-v2.xml" -Encoding UTF8

Write-Host "`n✅ New API created successfully!" -ForegroundColor Green
Write-Host "New API URL: https://$newApiName.azurewebsites.net" -ForegroundColor Cyan
Write-Host "Completed at $(Get-Date -Format 'h:mm tt') Pacific" -ForegroundColor Cyan
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Update GitHub secret AZURE_WEBAPP_PUBLISH_PROFILE_API with contents of publish-profile-v2.xml"
Write-Host "2. Update frontend environment variable VITE_API_URL to https://$newApiName.azurewebsites.net"
Write-Host "3. Run deployment workflow"