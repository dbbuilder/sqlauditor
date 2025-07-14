# Comprehensive test of SQL Analyzer deployment with authentication
Write-Host "=== SQL Analyzer Deployment Test ===" -ForegroundColor Cyan

$apiUrl = "https://sqlanalyzer-api.azurewebsites.net"
$uiUrl = "https://black-desert-02d93d30f.2.azurestaticapps.net"
$username = "admin"
$password = "AnalyzeThis!!"

# Test 1: API Health
Write-Host "`n1. Testing API health..." -ForegroundColor Yellow
try {
    $health = Invoke-WebRequest -Uri "$apiUrl/health" -UseBasicParsing
    Write-Host "✅ API is healthy (Status: $($health.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "❌ API health check failed: $_" -ForegroundColor Red
}

# Test 2: Login
Write-Host "`n2. Testing authentication login..." -ForegroundColor Yellow
$loginBody = @{
    username = $username
    password = $password
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod `
        -Uri "$apiUrl/api/v1/auth/login" `
        -Method POST `
        -Body $loginBody `
        -ContentType "application/json"
    
    Write-Host "✅ Login successful!" -ForegroundColor Green
    Write-Host "   Username: $($loginResponse.username)" -ForegroundColor Gray
    Write-Host "   Token: $($loginResponse.token.Substring(0, 50))..." -ForegroundColor Gray
    Write-Host "   Expires: $($loginResponse.expiresAt)" -ForegroundColor Gray
    $token = $loginResponse.token
} catch {
    Write-Host "❌ Login failed: $_" -ForegroundColor Red
    exit 1
}

# Test 3: Verify token
Write-Host "`n3. Testing token verification..." -ForegroundColor Yellow
try {
    $headers = @{ "Authorization" = "Bearer $token" }
    $verify = Invoke-RestMethod `
        -Uri "$apiUrl/api/v1/auth/verify" `
        -Headers $headers
    
    Write-Host "✅ Token verified" -ForegroundColor Green
    Write-Host "   Authenticated: $($verify.authenticated)" -ForegroundColor Gray
    Write-Host "   Username: $($verify.username)" -ForegroundColor Gray
    Write-Host "   Role: $($verify.role)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Token verification failed: $_" -ForegroundColor Red
}

# Test 4: Protected endpoint without auth
Write-Host "`n4. Testing protected endpoint without auth..." -ForegroundColor Yellow
try {
    $noAuth = Invoke-WebRequest `
        -Uri "$apiUrl/api/v1/analysis/types" `
        -UseBasicParsing `
        -ErrorAction Stop
    Write-Host "⚠️  Endpoint not protected! (Status: $($noAuth.StatusCode))" -ForegroundColor Yellow
} catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 401) {
        Write-Host "✅ Endpoint properly protected (401 Unauthorized)" -ForegroundColor Green
    } else {
        Write-Host "❌ Unexpected error: $_" -ForegroundColor Red
    }
}

# Test 5: Protected endpoint with auth
Write-Host "`n5. Testing protected endpoint with auth..." -ForegroundColor Yellow
try {
    $types = Invoke-RestMethod `
        -Uri "$apiUrl/api/v1/analysis/types" `
        -Headers $headers
    
    Write-Host "✅ Protected endpoint accessible with token" -ForegroundColor Green
    Write-Host "   Response type: $($types.GetType().Name)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Failed to access protected endpoint: $_" -ForegroundColor Red
}

# Test 6: UI Availability
Write-Host "`n6. Testing UI availability..." -ForegroundColor Yellow
try {
    $ui = Invoke-WebRequest -Uri $uiUrl -UseBasicParsing
    if ($ui.StatusCode -eq 200) {
        Write-Host "✅ UI is accessible" -ForegroundColor Green
        if ($ui.Content -match "login") {
            Write-Host "   Login page detected" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "❌ UI not accessible: $_" -ForegroundColor Red
}

# Test 7: CORS
Write-Host "`n7. Testing CORS configuration..." -ForegroundColor Yellow
try {
    $corsHeaders = @{
        "Origin" = $uiUrl
        "Access-Control-Request-Method" = "GET"
        "Access-Control-Request-Headers" = "authorization"
    }
    
    $cors = Invoke-WebRequest `
        -Uri "$apiUrl/api/v1/analysis/types" `
        -Method OPTIONS `
        -Headers $corsHeaders `
        -UseBasicParsing
    
    if ($cors.Headers["Access-Control-Allow-Origin"]) {
        Write-Host "✅ CORS properly configured" -ForegroundColor Green
        Write-Host "   Allow-Origin: $($cors.Headers["Access-Control-Allow-Origin"])" -ForegroundColor Gray
        Write-Host "   Allow-Credentials: $($cors.Headers["Access-Control-Allow-Credentials"])" -ForegroundColor Gray
    } else {
        Write-Host "⚠️  No CORS headers found" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️  CORS test inconclusive: $_" -ForegroundColor Yellow
}

# Summary
Write-Host "`n=== Test Summary ===" -ForegroundColor Cyan
Write-Host "API URL: $apiUrl" -ForegroundColor White
Write-Host "UI URL: $uiUrl" -ForegroundColor White
Write-Host "Authentication: Working with password '$password'" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Open $uiUrl in your browser" -ForegroundColor White
Write-Host "2. Login with admin / $password" -ForegroundColor White
Write-Host "3. Test the application functionality" -ForegroundColor White