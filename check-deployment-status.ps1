# Check SQL Analyzer Deployment Status
Write-Host "SQL Analyzer Deployment Status Check" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan

# Check API endpoints
Write-Host "`nChecking API endpoints..." -ForegroundColor Yellow

# Test basic API access
try {
    $apiVersion = Invoke-RestMethod -Uri "https://sqlanalyzer-api.azurewebsites.net/api/version" -ErrorAction Stop
    Write-Host "✅ API Version endpoint: SUCCESS" -ForegroundColor Green
    Write-Host "   Version: $($apiVersion.version)" -ForegroundColor Gray
    Write-Host "   Environment: $($apiVersion.environment)" -ForegroundColor Gray
} catch {
    Write-Host "❌ API Version endpoint: FAILED" -ForegroundColor Red
    Write-Host "   Error: $_" -ForegroundColor Red
}

# Test health endpoint
try {
    $health = Invoke-WebRequest -Uri "https://sqlanalyzer-api.azurewebsites.net/health" -ErrorAction Stop
    Write-Host "✅ Health endpoint: SUCCESS (Status: $($health.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "❌ Health endpoint: FAILED" -ForegroundColor Red
    Write-Host "   Error: $_" -ForegroundColor Red
}

# Check Static Web App
Write-Host "`nChecking Static Web App..." -ForegroundColor Yellow
try {
    $swa = Invoke-WebRequest -Uri "https://black-desert-02d93d30f.2.azurestaticapps.net" -ErrorAction Stop
    Write-Host "✅ Static Web App: ACCESSIBLE (Status: $($swa.StatusCode))" -ForegroundColor Green
    
    # Check for common errors in content
    if ($swa.Content -match "Error:|error:") {
        Write-Host "⚠️  Possible errors detected in page content" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Static Web App: FAILED" -ForegroundColor Red
    Write-Host "   Error: $_" -ForegroundColor Red
}

Write-Host "`nCORS Test Instructions:" -ForegroundColor Yellow
Write-Host "1. Open browser: https://black-desert-02d93d30f.2.azurestaticapps.net" -ForegroundColor White
Write-Host "2. Open Developer Tools (F12)" -ForegroundColor White
Write-Host "3. Check Console tab for CORS errors" -ForegroundColor White
Write-Host "4. Check Network tab for failed API calls" -ForegroundColor White

Write-Host "`nIf CORS errors persist:" -ForegroundColor Yellow
Write-Host "1. Go to Azure Portal > sqlanalyzer-api > CORS" -ForegroundColor White
Write-Host "2. Remove ALL entries and save" -ForegroundColor White
Write-Host "3. Restart the app service" -ForegroundColor White

Write-Host "`nDirect Links:" -ForegroundColor Yellow
Write-Host "- UI: https://black-desert-02d93d30f.2.azurestaticapps.net" -ForegroundColor Cyan
Write-Host "- API Health: https://sqlanalyzer-api.azurewebsites.net/health" -ForegroundColor Cyan
Write-Host "- API Version: https://sqlanalyzer-api.azurewebsites.net/api/version" -ForegroundColor Cyan
Write-Host "- Azure Portal: https://portal.azure.com" -ForegroundColor Cyan