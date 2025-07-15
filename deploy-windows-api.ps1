# Direct deployment to Windows API
# Start time: 10:00 PM Pacific
# Expected completion: 10:02 PM Pacific

Write-Host "Building and deploying API to Windows App Service..." -ForegroundColor Yellow
Write-Host "Start time: $(Get-Date -Format 'h:mm tt') Pacific" -ForegroundColor Cyan

cd api

# Clean previous builds
if (Test-Path "win-publish") {
    Remove-Item -Path "win-publish" -Recurse -Force
}

# Create deployment package using Docker
Write-Host "Building with Docker..." -ForegroundColor Yellow
docker run --rm -v "${PWD}:/src" -w /src mcr.microsoft.com/dotnet/sdk:8.0 bash -c "
    dotnet restore SqlAnalyzer.Api/SqlAnalyzer.Api.csproj
    dotnet publish SqlAnalyzer.Api/SqlAnalyzer.Api.csproj -c Release -o win-publish
"

# Create zip file
Write-Host "Creating deployment package..." -ForegroundColor Yellow
cd win-publish
zip -r ../deploy-win.zip *
cd ..

# Deploy using Azure CLI
Write-Host "Deploying to Azure..." -ForegroundColor Yellow
az webapp deployment source config-zip `
    --resource-group rg-sqlanalyzer `
    --name sqlanalyzer-api-win `
    --src deploy-win.zip

# Clean up
Remove-Item -Path deploy-win.zip -Force
Remove-Item -Path win-publish -Recurse -Force

Write-Host "`nDeployment completed at $(Get-Date -Format 'h:mm tt') Pacific" -ForegroundColor Green
Write-Host "API URL: https://sqlanalyzer-api-win.azurewebsites.net" -ForegroundColor Cyan

cd ..