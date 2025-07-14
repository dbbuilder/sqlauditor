# Test Azure Static Web App Deployment

$swaUrl = "https://black-desert-02d93d30f.2.azurestaticapps.net"

Write-Host "Testing SQL Analyzer Static Web App Deployment" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Main page
Write-Host "1. Testing main page..." -ForegroundColor Yellow
$mainPage = Invoke-WebRequest -Uri $swaUrl -UseBasicParsing
if ($mainPage.StatusCode -eq 200) {
    Write-Host "   ✓ Main page loads (Status: 200)" -ForegroundColor Green
    
    # Check for Vue app div
    if ($mainPage.Content -match '<div id="app">') {
        Write-Host "   ✓ Vue app container found" -ForegroundColor Green
    } else {
        Write-Host "   ✗ Vue app container NOT found" -ForegroundColor Red
    }
    
    # Check title
    if ($mainPage.Content -match '<title>SQL Analyzer</title>') {
        Write-Host "   ✓ Correct title found" -ForegroundColor Green
    }
} else {
    Write-Host "   ✗ Main page failed to load" -ForegroundColor Red
}

# Test 2: JavaScript asset
Write-Host "`n2. Testing JavaScript asset..." -ForegroundColor Yellow
$jsUrl = "$swaUrl/assets/index-Cqh46ZPP.js"
try {
    $jsResponse = Invoke-WebRequest -Uri $jsUrl -UseBasicParsing
    if ($jsResponse.StatusCode -eq 200) {
        Write-Host "   ✓ JavaScript loads successfully" -ForegroundColor Green
        $jsSize = [math]::Round($jsResponse.RawContentLength / 1024, 2)
        Write-Host "   Size: ${jsSize}KB" -ForegroundColor Gray
    }
} catch {
    Write-Host "   ✗ JavaScript failed to load: $_" -ForegroundColor Red
}

# Test 3: CSS asset
Write-Host "`n3. Testing CSS asset..." -ForegroundColor Yellow
$cssUrl = "$swaUrl/assets/index-CgDbBy7C.css"
try {
    $cssResponse = Invoke-WebRequest -Uri $cssUrl -UseBasicParsing
    if ($cssResponse.StatusCode -eq 200) {
        Write-Host "   ✓ CSS loads successfully" -ForegroundColor Green
        $cssSize = [math]::Round($cssResponse.RawContentLength / 1024, 2)
        Write-Host "   Size: ${cssSize}KB" -ForegroundColor Gray
    }
} catch {
    Write-Host "   ✗ CSS failed to load: $_" -ForegroundColor Red
}

# Test 4: Check if SPA routing works
Write-Host "`n4. Testing SPA routing..." -ForegroundColor Yellow
$dashboardUrl = "$swaUrl/dashboard"
try {
    $dashboardResponse = Invoke-WebRequest -Uri $dashboardUrl -UseBasicParsing
    if ($dashboardResponse.StatusCode -eq 200 -and $dashboardResponse.Content -match '<div id="app">') {
        Write-Host "   ✓ SPA routing works (returns index.html)" -ForegroundColor Green
    }
} catch {
    Write-Host "   ✗ SPA routing failed: $_" -ForegroundColor Red
}

# Test 5: Check for common issues
Write-Host "`n5. Common issue checks..." -ForegroundColor Yellow

# Check if it's the Azure default page
if ($mainPage.Content -match "Your Azure Static Web App is live") {
    Write-Host "   ⚠ WARNING: Still showing Azure default page!" -ForegroundColor Yellow
    Write-Host "   The deployment may not have completed properly." -ForegroundColor Yellow
} else {
    Write-Host "   ✓ Not showing Azure default page" -ForegroundColor Green
}

# Summary
Write-Host "`n" + ("="*50) -ForegroundColor Cyan
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "URL: $swaUrl" -ForegroundColor White
Write-Host "`nNext Steps:" -ForegroundColor Yellow
Write-Host "1. Open the URL in a browser" -ForegroundColor White
Write-Host "2. Check browser console for errors (F12)" -ForegroundColor White
Write-Host "3. Verify API calls are going to: https://sqlanalyzer-api.azurewebsites.net" -ForegroundColor White