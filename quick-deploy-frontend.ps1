# Quick Frontend Deployment Script
Write-Host "=== Quick Frontend Deployment to Azure Static Web Apps ===" -ForegroundColor Cyan

# Check if we're in the frontend repo
if (Test-Path "D:\Dev2\sqlanalyzer-web\.git") {
    Set-Location "D:\Dev2\sqlanalyzer-web"
    
    # Check if remote is configured
    $remote = git remote get-url origin 2>$null
    
    if (!$remote) {
        Write-Host "❌ No GitHub remote configured" -ForegroundColor Red
        Write-Host "Please run: .\push-frontend-repo.ps1" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "Repository configured: $remote" -ForegroundColor Green
    
    # Get deployment token
    Write-Host "`nGetting deployment token..." -ForegroundColor Yellow
    $token = az staticwebapp secrets list `
        --name sqlanalyzer-web `
        --resource-group rg-sqlanalyzer `
        --query "properties.apiKey" -o tsv
    
    if ($token) {
        Write-Host "✅ Retrieved deployment token" -ForegroundColor Green
        
        # Extract GitHub username from remote URL
        if ($remote -match "github.com[:/]([^/]+)/") {
            $githubUsername = $matches[1]
            
            Write-Host "`n=== Next Steps ===" -ForegroundColor Cyan
            Write-Host ""
            Write-Host "1. Add deployment token to GitHub:" -ForegroundColor White
            Write-Host "   URL: https://github.com/$githubUsername/sqlanalyzer-web/settings/secrets/actions/new" -ForegroundColor Gray
            Write-Host "   Secret Name: AZURE_STATIC_WEB_APPS_API_TOKEN" -ForegroundColor Yellow
            Write-Host "   Secret Value: (see below)" -ForegroundColor Gray
            Write-Host ""
            Write-Host "2. Push your code (if not already done):" -ForegroundColor White
            Write-Host "   git push -u origin main" -ForegroundColor Gray
            Write-Host ""
            Write-Host "3. GitHub Actions will automatically deploy" -ForegroundColor White
            Write-Host ""
            Write-Host "=== DEPLOYMENT TOKEN ===" -ForegroundColor Red
            Write-Host $token -ForegroundColor Yellow
            Write-Host "========================" -ForegroundColor Red
            
            # Save token to clipboard if possible
            $token | Set-Clipboard 2>$null
            if ($?) {
                Write-Host "`n✅ Token copied to clipboard!" -ForegroundColor Green
            }
            
            # Open browser to add secret
            Write-Host "`nOpening GitHub secrets page..." -ForegroundColor Yellow
            Start-Process "https://github.com/$githubUsername/sqlanalyzer-web/settings/secrets/actions/new"
            
            # Also open Actions page
            Start-Process "https://github.com/$githubUsername/sqlanalyzer-web/actions"
        }
    } else {
        Write-Host "❌ Could not retrieve deployment token" -ForegroundColor Red
    }
    
    Set-Location -Path "D:\Dev2\sqlauditor"
} else {
    Write-Host "❌ Frontend repository not found at D:\Dev2\sqlanalyzer-web" -ForegroundColor Red
    Write-Host "Please run: .\create-frontend-repo.ps1" -ForegroundColor Yellow
}

# Check current deployment
Write-Host "`n=== Current Deployment Status ===" -ForegroundColor Cyan
az staticwebapp show `
    --name sqlanalyzer-web `
    --resource-group rg-sqlanalyzer `
    --query "{URL:defaultHostname, Repository:repositoryUrl, Branch:branch}" `
    -o table

Write-Host "`nSite URL: https://black-desert-02d93d30f.2.azurestaticapps.net" -ForegroundColor Green
Write-Host "Login: admin / AnalyzeThis!!" -ForegroundColor Yellow