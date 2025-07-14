# PowerShell script to fix CORS using Azure Portal method

Write-Host "Fixing CORS for SQL Analyzer API" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# The best approach is to remove Azure Portal CORS and let the app handle it
Write-Host "`nRECOMMENDED FIX:" -ForegroundColor Yellow
Write-Host "1. Go to Azure Portal: https://portal.azure.com" -ForegroundColor White
Write-Host "2. Navigate to: Resource Groups > rg-sqlanalyzer > sqlanalyzer-api" -ForegroundColor White
Write-Host "3. Go to: Settings > CORS" -ForegroundColor White
Write-Host "4. Remove ALL entries (click X on each)" -ForegroundColor White
Write-Host "5. Click Save" -ForegroundColor White
Write-Host "6. Restart the app service" -ForegroundColor White

Write-Host "`nWhy this works:" -ForegroundColor Green
Write-Host "- Azure Portal CORS overrides application CORS" -ForegroundColor Gray
Write-Host "- Our API already has proper CORS configuration in code" -ForegroundColor Gray
Write-Host "- The code includes the SWA URLs" -ForegroundColor Gray

Write-Host "`nAlternative if removing doesn't work:" -ForegroundColor Yellow
Write-Host "Add these origins in Azure Portal CORS:" -ForegroundColor White
Write-Host "- https://black-desert-02d93d30f.2.azurestaticapps.net" -ForegroundColor Cyan
Write-Host "- https://sqlanalyzer-web.azurestaticapps.net" -ForegroundColor Cyan
Write-Host "- http://localhost:5173" -ForegroundColor Cyan
Write-Host "- http://localhost:3000" -ForegroundColor Cyan

Write-Host "`nTesting after fix:" -ForegroundColor Yellow
Write-Host "1. Clear browser cache (Ctrl+Shift+R)" -ForegroundColor White
Write-Host "2. Open: https://black-desert-02d93d30f.2.azurestaticapps.net" -ForegroundColor White
Write-Host "3. Check console - CORS errors should be gone" -ForegroundColor White