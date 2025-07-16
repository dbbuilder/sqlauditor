# Deploy API to Azure App Service
Write-Host "=== Deploying API to Azure ===" -ForegroundColor Cyan

# Navigate to API directory
Set-Location -Path "api"

# Build the API
Write-Host "Building API..." -ForegroundColor Yellow
dotnet publish SqlAnalyzer.Api/SqlAnalyzer.Api.csproj -c Release -o ./publish

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

# Create deployment package
Write-Host "Creating deployment package..." -ForegroundColor Yellow
Compress-Archive -Path ./publish/* -DestinationPath ../api-deploy.zip -Force

# Deploy to Azure
Write-Host "Deploying to Azure App Service..." -ForegroundColor Yellow
az webapp deployment source config-zip `
    --resource-group rg-sqlanalyzer `
    --name sqlanalyzer-api `
    --src ../api-deploy.zip

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ API deployed successfully!" -ForegroundColor Green
    
    # Test the API
    Write-Host "`nTesting API..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10
    
    $testUrl = "https://sqlanalyzer-api.azurewebsites.net/api/v1/auth/login"
    Write-Host "Testing: $testUrl" -ForegroundColor Gray
    
    try {
        $response = Invoke-RestMethod -Uri $testUrl -Method POST `
            -Headers @{"Content-Type"="application/json"} `
            -Body '{"Username":"admin","Password":"AnalyzeThis!!"}'
        
        Write-Host "✅ API is working!" -ForegroundColor Green
        Write-Host "Token received: $($response.token.Substring(0,20))..." -ForegroundColor Gray
    } catch {
        Write-Host "⚠️  API test failed: $_" -ForegroundColor Yellow
    }
} else {
    Write-Host "❌ Deployment failed!" -ForegroundColor Red
}

Set-Location -Path ".."