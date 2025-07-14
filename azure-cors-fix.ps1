# Azure CORS Fix Script
Write-Host "=== SQL Analyzer CORS Fix ===" -ForegroundColor Cyan
Write-Host ""

# Load Azure credentials
if (Test-Path ".env.azure.local") {
    Get-Content ".env.azure.local" | ForEach-Object {
        if ($_ -match '^(.+?)=(.+)$') {
            [System.Environment]::SetEnvironmentVariable($matches[1], $matches[2])
        }
    }
}

Write-Host "Attempting to fix CORS via Azure CLI..." -ForegroundColor Yellow

# Try to remove CORS settings via CLI
Write-Host "`nRemoving Azure Portal CORS settings..." -ForegroundColor Cyan
try {
    # First, get current CORS settings
    $currentCors = az webapp cors show --name sqlanalyzer-api --resource-group rg-sqlanalyzer --query allowedOrigins -o json | ConvertFrom-Json
    
    if ($currentCors.Count -gt 0) {
        Write-Host "Found $($currentCors.Count) CORS entries. Removing all..." -ForegroundColor Yellow
        
        # Remove all CORS entries
        az webapp cors remove --name sqlanalyzer-api --resource-group rg-sqlanalyzer --allowed-origins * 2>$null
        
        Write-Host "✅ CORS entries removed" -ForegroundColor Green
    } else {
        Write-Host "No CORS entries found in Azure Portal" -ForegroundColor Gray
    }
} catch {
    Write-Host "⚠️  Could not remove CORS via CLI. Manual fix required." -ForegroundColor Yellow
}

Write-Host "`nRestarting API..." -ForegroundColor Cyan
az webapp restart --name sqlanalyzer-api --resource-group rg-sqlanalyzer

Write-Host "`n✅ API restarted" -ForegroundColor Green

Write-Host "`n=== MANUAL FIX REQUIRED ===" -ForegroundColor Red
Write-Host "The Azure CLI cannot fully remove CORS settings. Please follow these steps:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Open Azure Portal: https://portal.azure.com" -ForegroundColor White
Write-Host "2. Navigate to:" -ForegroundColor White
Write-Host "   Resource Groups → rg-sqlanalyzer → sqlanalyzer-api" -ForegroundColor Cyan
Write-Host "3. Click 'CORS' in the left menu under 'API'" -ForegroundColor White
Write-Host "4. DELETE ALL entries in the 'Allowed Origins' list" -ForegroundColor Yellow
Write-Host "5. Click 'Save' at the top" -ForegroundColor White
Write-Host "6. Go back to 'Overview' and click 'Restart'" -ForegroundColor White
Write-Host ""
Write-Host "Direct link to CORS settings:" -ForegroundColor Green
Write-Host "https://portal.azure.com/#blade/WebsitesExtension/CorsBladeV3/resourceId/%2Fsubscriptions%2F7b2beff3-b38a-4516-a75f-3216725cc4e9%2FresourceGroups%2Frg-sqlanalyzer%2Fproviders%2FMicrosoft.Web%2Fsites%2Fsqlanalyzer-api" -ForegroundColor Cyan
Write-Host ""
Write-Host "=== Why This Happens ===" -ForegroundColor Yellow
Write-Host "- Azure Portal CORS overrides application CORS" -ForegroundColor Gray
Write-Host "- The app already has correct CORS in Program.cs" -ForegroundColor Gray
Write-Host "- Removing Portal CORS lets the app handle it" -ForegroundColor Gray
Write-Host ""
Write-Host "After fixing, test at:" -ForegroundColor Green
Write-Host "https://black-desert-02d93d30f.2.azurestaticapps.net" -ForegroundColor Cyan