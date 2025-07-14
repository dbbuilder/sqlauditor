# Check Frontend Deployment Status
Write-Host "=== Checking Frontend Deployment Status ===" -ForegroundColor Cyan

# Check Static Web App status
Write-Host "`nStatic Web App Details:" -ForegroundColor Yellow
az staticwebapp show `
    --name black-desert-02d93d30f `
    --resource-group rg-sqlanalyzer `
    --query "{Name:name, DefaultHostname:defaultHostname, RepositoryUrl:repositoryUrl, Branch:branch, Status:provisioningState}" `
    -o table

# Check recent deployments
Write-Host "`nRecent Builds:" -ForegroundColor Yellow
az staticwebapp builds list `
    --name black-desert-02d93d30f `
    --resource-group rg-sqlanalyzer `
    --query "[?status=='Succeeded' || status=='InProgress' || status=='Failed'].{BuildId:buildId, Status:status, CreatedAt:createdTimeUtc, Branch:sourceBranch} | [0:5]" `
    -o table 2>$null

if ($LASTEXITCODE -ne 0) {
    Write-Host "No recent builds found (this is normal if not yet deployed via GitHub)" -ForegroundColor Gray
}

# Test the deployed site
Write-Host "`nTesting deployed site..." -ForegroundColor Yellow
$siteUrl = "https://black-desert-02d93d30f.2.azurestaticapps.net"

try {
    $response = Invoke-WebRequest -Uri $siteUrl -UseBasicParsing -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Site is accessible!" -ForegroundColor Green
        
        # Check if it's the new version by looking for specific content
        if ($response.Content -match "connection-builder" -or $response.Content -match "connectionMode") {
            Write-Host "✅ New version with connection builder is deployed!" -ForegroundColor Green
        } else {
            Write-Host "⚠️  Site is accessible but might be showing old version" -ForegroundColor Yellow
            Write-Host "   Wait a few minutes for deployment to complete" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "❌ Site is not accessible yet" -ForegroundColor Red
    Write-Host "   This is normal if deployment is still in progress" -ForegroundColor Gray
}

Write-Host "`n=== Quick Links ===" -ForegroundColor Cyan
Write-Host "Site URL: $siteUrl" -ForegroundColor White
Write-Host "Login: admin / AnalyzeThis!!" -ForegroundColor White
Write-Host ""
Write-Host "Azure Portal: https://portal.azure.com" -ForegroundColor Gray
Write-Host "Resource: black-desert-02d93d30f (in rg-sqlanalyzer)" -ForegroundColor Gray

# Check if frontend repository exists locally
if (Test-Path "D:\Dev2\sqlanalyzer-web\.git") {
    Write-Host "`n=== Local Repository Status ===" -ForegroundColor Cyan
    Set-Location "D:\Dev2\sqlanalyzer-web"
    
    # Check remote
    $remote = git remote get-url origin 2>$null
    if ($remote) {
        Write-Host "Repository URL: $remote" -ForegroundColor Green
        
        # Check if pushed
        $unpushed = git log origin/main..HEAD 2>$null
        if ($unpushed) {
            Write-Host "⚠️  Local changes not pushed to GitHub" -ForegroundColor Yellow
        } else {
            Write-Host "✅ All changes pushed to GitHub" -ForegroundColor Green
        }
    } else {
        Write-Host "❌ No remote configured" -ForegroundColor Red
    }
    
    Set-Location -Path "D:\Dev2\sqlauditor"
}