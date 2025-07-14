# Push Frontend Repository to GitHub and Configure Deployment
Write-Host "=== Pushing Frontend Repository to GitHub ===" -ForegroundColor Cyan

# Navigate to frontend repository
Set-Location -Path "D:\Dev2\sqlanalyzer-web"

# Check if git remote exists
$remoteExists = git remote -v
if ($remoteExists) {
    Write-Host "❌ Remote already exists. Removing..." -ForegroundColor Yellow
    git remote remove origin
}

# Add GitHub remote (you'll need to update the username)
Write-Host "`nAdding GitHub remote..." -ForegroundColor Yellow
Write-Host "Please replace YOUR_USERNAME with your actual GitHub username:" -ForegroundColor Red

$githubUsername = Read-Host "Enter your GitHub username"
if ([string]::IsNullOrWhiteSpace($githubUsername)) {
    Write-Host "❌ GitHub username is required" -ForegroundColor Red
    exit 1
}

git remote add origin "https://github.com/$githubUsername/sqlanalyzer-web.git"

Write-Host "`nPushing to GitHub..." -ForegroundColor Yellow
Write-Host "Note: You may be prompted for GitHub credentials" -ForegroundColor Gray

# Push to GitHub
git push -u origin main 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Push failed. Trying master branch..." -ForegroundColor Yellow
    git branch -m main master
    git push -u origin master
}

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Repository pushed successfully!" -ForegroundColor Green
    
    # Get Static Web App deployment token
    Write-Host "`n=== Configuring Static Web App Deployment ===`n" -ForegroundColor Cyan
    
    Write-Host "Getting deployment token from Azure..." -ForegroundColor Yellow
    $deploymentToken = az staticwebapp secrets list `
        --name black-desert-02d93d30f `
        --resource-group rg-sqlanalyzer `
        --query "properties.apiKey" -o tsv
    
    if ($deploymentToken) {
        Write-Host "✅ Retrieved deployment token" -ForegroundColor Green
        
        Write-Host "`nNext steps to complete deployment:" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "1. Add deployment token to GitHub repository:" -ForegroundColor White
        Write-Host "   - Go to: https://github.com/$githubUsername/sqlanalyzer-web/settings/secrets/actions" -ForegroundColor Gray
        Write-Host "   - Click 'New repository secret'" -ForegroundColor Gray
        Write-Host "   - Name: AZURE_STATIC_WEB_APPS_API_TOKEN" -ForegroundColor Gray
        Write-Host "   - Value: (see below)" -ForegroundColor Gray
        Write-Host ""
        Write-Host "2. Configure Static Web App source in Azure Portal:" -ForegroundColor White
        Write-Host "   - Go to: https://portal.azure.com" -ForegroundColor Gray
        Write-Host "   - Navigate to: black-desert-02d93d30f" -ForegroundColor Gray
        Write-Host "   - Go to Deployment Center" -ForegroundColor Gray
        Write-Host "   - Select GitHub as source" -ForegroundColor Gray
        Write-Host "   - Repository: $githubUsername/sqlanalyzer-web" -ForegroundColor Gray
        Write-Host "   - Branch: main (or master)" -ForegroundColor Gray
        Write-Host ""
        Write-Host "3. The deployment will start automatically via GitHub Actions" -ForegroundColor White
        Write-Host ""
        Write-Host "=== DEPLOYMENT TOKEN (Keep this secure!) ===" -ForegroundColor Red
        Write-Host $deploymentToken -ForegroundColor Yellow
        Write-Host "============================================" -ForegroundColor Red
        
        # Create a file with the token for easy copying
        $deploymentToken | Out-File -FilePath "deployment-token.txt" -NoNewline
        Write-Host "`nToken also saved to: deployment-token.txt (delete after use!)" -ForegroundColor Yellow
        
        # Try to open GitHub secrets page
        Write-Host "`nOpening GitHub secrets page in browser..." -ForegroundColor Yellow
        Start-Process "https://github.com/$githubUsername/sqlanalyzer-web/settings/secrets/actions/new"
        
    } else {
        Write-Host "❌ Could not retrieve deployment token" -ForegroundColor Red
        Write-Host "Please get it manually from Azure Portal > black-desert-02d93d30f > Manage deployment token" -ForegroundColor Yellow
    }
} else {
    Write-Host "`n❌ Failed to push repository" -ForegroundColor Red
    Write-Host "Please ensure:" -ForegroundColor Yellow
    Write-Host "1. You have created the repository on GitHub: https://github.com/new" -ForegroundColor Gray
    Write-Host "2. The repository name is: sqlanalyzer-web" -ForegroundColor Gray
    Write-Host "3. You have the correct permissions" -ForegroundColor Gray
}

Set-Location -Path "D:\Dev2\sqlauditor"