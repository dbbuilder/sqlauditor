# Configure Static Web App with GitHub Repository
Write-Host "=== Configuring Static Web App Deployment ===" -ForegroundColor Cyan

# Get GitHub username
$githubUsername = Read-Host "Enter your GitHub username"
if ([string]::IsNullOrWhiteSpace($githubUsername)) {
    Write-Host "❌ GitHub username is required" -ForegroundColor Red
    exit 1
}

# Configure Static Web App
Write-Host "`nConfiguring Static Web App with GitHub repository..." -ForegroundColor Yellow

try {
    # Update the static web app configuration
    az staticwebapp update `
        --name black-desert-02d93d30f `
        --resource-group rg-sqlanalyzer `
        --source "https://github.com/$githubUsername/sqlanalyzer-web" `
        --branch main `
        --app-location "/" `
        --output-location "dist" `
        --login-with-github

    Write-Host "✅ Static Web App configured successfully!" -ForegroundColor Green
    
    # Trigger deployment
    Write-Host "`nTriggering deployment..." -ForegroundColor Yellow
    
    # The deployment should start automatically after configuration
    Write-Host "Deployment will start automatically via GitHub Actions" -ForegroundColor Green
    
    Write-Host "`nMonitor deployment status:" -ForegroundColor Yellow
    Write-Host "1. GitHub Actions: https://github.com/$githubUsername/sqlanalyzer-web/actions" -ForegroundColor Gray
    Write-Host "2. Azure Portal: https://portal.azure.com (navigate to black-desert-02d93d30f)" -ForegroundColor Gray
    
    # Check deployment status
    Write-Host "`nChecking current deployment status..." -ForegroundColor Yellow
    az staticwebapp show `
        --name black-desert-02d93d30f `
        --resource-group rg-sqlanalyzer `
        --query "{defaultHostname:defaultHostname, repositoryUrl:repositoryUrl, branch:branch}" `
        -o table
        
} catch {
    Write-Host "❌ Failed to configure Static Web App" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    
    Write-Host "`nManual configuration required:" -ForegroundColor Yellow
    Write-Host "1. Go to Azure Portal: https://portal.azure.com" -ForegroundColor Gray
    Write-Host "2. Navigate to: black-desert-02d93d30f" -ForegroundColor Gray
    Write-Host "3. Click on 'Deployment Center'" -ForegroundColor Gray
    Write-Host "4. Configure GitHub source with repository: $githubUsername/sqlanalyzer-web" -ForegroundColor Gray
}

Write-Host "`n=== Deployment URLs ===" -ForegroundColor Cyan
Write-Host "Static Web App: https://black-desert-02d93d30f.2.azurestaticapps.net" -ForegroundColor Green
Write-Host "API Backend: https://sqlanalyzer-api.azurewebsites.net" -ForegroundColor Green
Write-Host ""
Write-Host "Default Login: admin / AnalyzeThis!!" -ForegroundColor Yellow