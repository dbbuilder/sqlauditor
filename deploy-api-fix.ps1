# Fix API deployment by building locally and deploying

Write-Host "Building API locally..." -ForegroundColor Yellow
cd api

# Clean previous builds
if (Test-Path "publish") {
    Remove-Item -Path "publish" -Recurse -Force
}

# Build the API
dotnet restore
dotnet publish SqlAnalyzer.Api/SqlAnalyzer.Api.csproj -c Release -o publish

# Create deployment package
Write-Host "Creating deployment package..." -ForegroundColor Yellow
cd publish
Compress-Archive -Path * -DestinationPath ../deploy.zip -Force
cd ..

# Deploy to Azure
Write-Host "Deploying to Azure..." -ForegroundColor Yellow
az webapp deployment source config-zip `
    --resource-group rg-sqlanalyzer `
    --name sqlanalyzer-api `
    --src deploy.zip

# Clean up
Remove-Item -Path deploy.zip -Force

Write-Host "API deployment completed!" -ForegroundColor Green
cd ..