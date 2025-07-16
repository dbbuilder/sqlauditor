# Check Deployment Status
Write-Host "=== Deployment Status Check ===" -ForegroundColor Cyan

# Check GitHub repository
Write-Host "`nGitHub Repository:" -ForegroundColor Yellow
gh repo view dbbuilder/sqlauditor --json name,url,pushedAt,defaultBranch | ConvertFrom-Json | Format-List

# Check GitHub Actions
Write-Host "`nGitHub Actions Status:" -ForegroundColor Yellow
$workflows = gh workflow list --repo dbbuilder/sqlauditor 2>$null
if ($workflows) {
    Write-Host $workflows
} else {
    Write-Host "No workflows found yet (push may be in progress)" -ForegroundColor Gray
}

# Check GitHub Secrets
Write-Host "`nGitHub Secrets:" -ForegroundColor Yellow
$secrets = gh secret list --repo dbbuilder/sqlauditor
Write-Host $secrets

# Check Azure Static Web App
Write-Host "`nAzure Static Web App Status:" -ForegroundColor Yellow
az staticwebapp show `
    --name sqlanalyzer-web `
    --resource-group rg-sqlanalyzer `
    --query "{URL:defaultHostname, Repository:repositoryUrl, Branch:branch}" `
    -o table

# Test the site
Write-Host "`nTesting Site:" -ForegroundColor Yellow
$response = Invoke-WebRequest -Uri "https://black-desert-02d93d30f.2.azurestaticapps.net" -UseBasicParsing -TimeoutSec 5 2>$null
if ($response.StatusCode -eq 200) {
    Write-Host "✅ Site is accessible" -ForegroundColor Green
} else {
    Write-Host "❌ Site returned status: $($response.StatusCode)" -ForegroundColor Red
}

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "Repository: https://github.com/dbbuilder/sqlauditor" -ForegroundColor White
Write-Host "Frontend URL: https://black-desert-02d93d30f.2.azurestaticapps.net" -ForegroundColor White
Write-Host "API URL: https://sqlanalyzer-api.azurewebsites.net" -ForegroundColor White
Write-Host "Login: admin / AnalyzeThis!!" -ForegroundColor Yellow

Write-Host "`nNote: If push is still in progress, wait a few minutes and run this script again." -ForegroundColor Gray