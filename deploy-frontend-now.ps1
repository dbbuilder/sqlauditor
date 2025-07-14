# Quick Frontend Deployment to Azure Static Web Apps
Write-Host "=== Deploying Frontend to Azure Static Web Apps ===" -ForegroundColor Cyan

# Check if we're in the right directory
if (!(Test-Path "src/SqlAnalyzer.Web/dist")) {
    Write-Host "❌ Frontend build not found. Building now..." -ForegroundColor Yellow
    Set-Location -Path "src/SqlAnalyzer.Web"
    npm run build
    Set-Location -Path "../.."
}

# Deploy using Azure CLI
Write-Host "`nDeploying to Static Web Apps..." -ForegroundColor Yellow

# Try using SWA CLI first
if (Get-Command swa -ErrorAction SilentlyContinue) {
    Write-Host "Using SWA CLI..." -ForegroundColor Green
    Set-Location -Path "src/SqlAnalyzer.Web"
    swa deploy ./dist --deployment-token $env:SWA_DEPLOYMENT_TOKEN
} else {
    Write-Host "SWA CLI not found. Using Azure CLI..." -ForegroundColor Yellow
    
    # Get deployment token
    $token = az staticwebapp secrets list `
        --name black-desert-02d93d30f `
        --resource-group rg-sqlanalyzer `
        --query "properties.apiKey" -o tsv
    
    if ($token) {
        Write-Host "Deploying with deployment token..." -ForegroundColor Yellow
        
        # Create deployment package
        Set-Location -Path "src/SqlAnalyzer.Web/dist"
        
        # Use GitHub API to deploy
        $headers = @{
            "Authorization" = "Bearer $token"
            "Content-Type" = "application/zip"
        }
        
        # Zip the dist folder
        Compress-Archive -Path * -DestinationPath ../deploy-frontend.zip -Force
        
        Write-Host "✅ Deployment package created" -ForegroundColor Green
        
        # Manual deployment instructions
        Write-Host "`nManual deployment required:" -ForegroundColor Yellow
        Write-Host "1. Go to Azure Portal" -ForegroundColor White
        Write-Host "2. Navigate to: black-desert-02d93d30f" -ForegroundColor White
        Write-Host "3. Use 'Deployment Center' to deploy" -ForegroundColor White
        Write-Host "4. Or use GitHub Actions by pushing to repository" -ForegroundColor White
    } else {
        Write-Host "❌ Could not get deployment token" -ForegroundColor Red
    }
}

Set-Location -Path "../../.."

Write-Host "`n=== Alternative: Direct File Upload ===" -ForegroundColor Cyan
Write-Host "Since we don't have a Git remote set up, you can:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Install Static Web Apps CLI:" -ForegroundColor White
Write-Host "   npm install -g @azure/static-web-apps-cli" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Deploy directly:" -ForegroundColor White
Write-Host "   cd src/SqlAnalyzer.Web" -ForegroundColor Gray
Write-Host "   swa deploy ./dist --app-name black-desert-02d93d30f" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Or manually upload files:" -ForegroundColor White
Write-Host "   - Go to Azure Portal > Static Web Apps" -ForegroundColor Gray
Write-Host "   - Navigate to your app" -ForegroundColor Gray
Write-Host "   - Use the deployment options" -ForegroundColor Gray

Write-Host "`n✅ Frontend build is ready in: src/SqlAnalyzer.Web/dist" -ForegroundColor Green