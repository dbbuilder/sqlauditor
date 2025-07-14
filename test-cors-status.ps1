# Test CORS Status for SQL Analyzer
Write-Host "SQL Analyzer CORS Configuration Test" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

$apiUrl = "https://sqlanalyzer-api.azurewebsites.net"
$swaUrl = "https://black-desert-02d93d30f.2.azurestaticapps.net"

# Test 1: Simple GET request to check if API is responding
Write-Host "1. Testing API availability..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$apiUrl/api/version" -Method GET -UseBasicParsing
    Write-Host "✅ API is accessible (Status: $($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "❌ API is not accessible: $_" -ForegroundColor Red
}

# Test 2: OPTIONS preflight request (what browsers do for CORS)
Write-Host "`n2. Testing CORS preflight (OPTIONS)..." -ForegroundColor Yellow
try {
    $headers = @{
        "Origin" = $swaUrl
        "Access-Control-Request-Method" = "GET"
        "Access-Control-Request-Headers" = "content-type"
    }
    
    $response = Invoke-WebRequest -Uri "$apiUrl/api/v1/analysis/types" -Method OPTIONS -Headers $headers -UseBasicParsing
    
    Write-Host "✅ OPTIONS request succeeded (Status: $($response.StatusCode))" -ForegroundColor Green
    
    # Check for CORS headers
    $corsHeaders = @(
        "Access-Control-Allow-Origin",
        "Access-Control-Allow-Methods",
        "Access-Control-Allow-Headers",
        "Access-Control-Allow-Credentials"
    )
    
    Write-Host "`n   CORS Headers in Response:" -ForegroundColor Cyan
    foreach ($header in $corsHeaders) {
        if ($response.Headers[$header]) {
            Write-Host "   ✅ $header : $($response.Headers[$header])" -ForegroundColor Green
        } else {
            Write-Host "   ❌ $header : Not present" -ForegroundColor Red
        }
    }
} catch {
    Write-Host "❌ OPTIONS request failed: $_" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "   Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    }
}

# Test 3: GET request with Origin header (simulating browser)
Write-Host "`n3. Testing GET with Origin header..." -ForegroundColor Yellow
try {
    $headers = @{
        "Origin" = $swaUrl
    }
    
    $response = Invoke-WebRequest -Uri "$apiUrl/api/v1/analysis/types" -Method GET -Headers $headers -UseBasicParsing
    
    Write-Host "✅ GET request with Origin succeeded (Status: $($response.StatusCode))" -ForegroundColor Green
    
    if ($response.Headers["Access-Control-Allow-Origin"]) {
        Write-Host "   ✅ Access-Control-Allow-Origin: $($response.Headers["Access-Control-Allow-Origin"])" -ForegroundColor Green
    } else {
        Write-Host "   ❌ No Access-Control-Allow-Origin header in response" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ GET request with Origin failed: $_" -ForegroundColor Red
}

# Test 4: Check Azure Portal CORS via Azure CLI
Write-Host "`n4. Checking Azure Portal CORS configuration..." -ForegroundColor Yellow
try {
    $corsConfig = az webapp cors show --name sqlanalyzer-api --resource-group rg-sqlanalyzer 2>$null | ConvertFrom-Json
    
    if ($corsConfig.allowedOrigins -and $corsConfig.allowedOrigins.Count -gt 0) {
        Write-Host "⚠️  Azure Portal CORS is CONFIGURED (This overrides app CORS!)" -ForegroundColor Red
        Write-Host "   Allowed Origins:" -ForegroundColor Yellow
        foreach ($origin in $corsConfig.allowedOrigins) {
            Write-Host "   - $origin" -ForegroundColor Gray
        }
        Write-Host "`n   🚨 This is likely causing the CORS errors!" -ForegroundColor Red
    } else {
        Write-Host "✅ No Azure Portal CORS configured (Good!)" -ForegroundColor Green
    }
} catch {
    Write-Host "   Could not check Azure Portal CORS via CLI" -ForegroundColor Gray
}

# Test 5: SignalR negotiate endpoint
Write-Host "`n5. Testing SignalR negotiate endpoint..." -ForegroundColor Yellow
try {
    $headers = @{
        "Origin" = $swaUrl
    }
    
    $response = Invoke-WebRequest -Uri "$apiUrl/hubs/analysis/negotiate?negotiateVersion=1" -Method POST -Headers $headers -UseBasicParsing
    
    Write-Host "✅ SignalR negotiate succeeded (Status: $($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "❌ SignalR negotiate failed: $_" -ForegroundColor Red
}

# Summary
Write-Host "`n====== SUMMARY ======" -ForegroundColor Cyan
Write-Host "API URL: $apiUrl" -ForegroundColor Gray
Write-Host "SWA URL: $swaUrl" -ForegroundColor Gray
Write-Host ""
Write-Host "If you see CORS errors above, you need to:" -ForegroundColor Yellow
Write-Host "1. Remove ALL entries from Azure Portal CORS settings" -ForegroundColor White
Write-Host "2. Restart the API app service" -ForegroundColor White
Write-Host ""
Write-Host "Direct link to fix:" -ForegroundColor Green
Write-Host "https://portal.azure.com/#blade/WebsitesExtension/CorsBladeV3/resourceId/%2Fsubscriptions%2F7b2beff3-b38a-4516-a75f-3216725cc4e9%2FresourceGroups%2Frg-sqlanalyzer%2Fproviders%2FMicrosoft.Web%2Fsites%2Fsqlanalyzer-api" -ForegroundColor Cyan