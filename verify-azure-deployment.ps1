# Verify SQL Analyzer Deployment with Authentication
Write-Host "=== Verifying SQL Analyzer Deployment ===" -ForegroundColor Cyan

$apiUrl = "https://sqlanalyzer-api.azurewebsites.net"
$uiUrl = "https://black-desert-02d93d30f.2.azurestaticapps.net"
$username = "admin"
$password = "SqlAnalyzer2024!"  # Change if you used a different password

# Test API Health
Write-Host "`n1. Testing API health..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$apiUrl/health" -ErrorAction Stop
    Write-Host "✅ API is healthy: $health" -ForegroundColor Green
} catch {
    Write-Host "❌ API health check failed: $_" -ForegroundColor Red
}

# Test Authentication
Write-Host "`n2. Testing authentication..." -ForegroundColor Yellow
try {
    $loginBody = @{
        username = $username
        password = $password
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod `
        -Uri "$apiUrl/api/v1/auth/login" `
        -Method POST `
        -Body $loginBody `
        -ContentType "application/json" `
        -ErrorAction Stop

    if ($loginResponse.token) {
        Write-Host "✅ Authentication working" -ForegroundColor Green
        Write-Host "   Token received (expires: $($loginResponse.expiresAt))" -ForegroundColor Gray
        $token = $loginResponse.token
    }
} catch {
    Write-Host "❌ Authentication failed: $_" -ForegroundColor Red
    $token = $null
}

# Test Protected Endpoint
if ($token) {
    Write-Host "`n3. Testing protected endpoint..." -ForegroundColor Yellow
    try {
        $headers = @{
            "Authorization" = "Bearer $token"
        }
        
        $types = Invoke-RestMethod `
            -Uri "$apiUrl/api/v1/analysis/types" `
            -Headers $headers `
            -ErrorAction Stop
            
        Write-Host "✅ Protected endpoint accessible" -ForegroundColor Green
    } catch {
        Write-Host "❌ Protected endpoint failed: $_" -ForegroundColor Red
    }
}

# Test UI
Write-Host "`n4. Testing UI availability..." -ForegroundColor Yellow
try {
    $uiResponse = Invoke-WebRequest -Uri $uiUrl -UseBasicParsing -ErrorAction Stop
    if ($uiResponse.StatusCode -eq 200) {
        Write-Host "✅ UI is accessible" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ UI not accessible: $_" -ForegroundColor Red
}

# Test CORS
Write-Host "`n5. Testing CORS configuration..." -ForegroundColor Yellow
try {
    $corsHeaders = @{
        "Origin" = $uiUrl
        "Access-Control-Request-Method" = "GET"
    }
    
    $corsResponse = Invoke-WebRequest `
        -Uri "$apiUrl/api/v1/analysis/types" `
        -Method OPTIONS `
        -Headers $corsHeaders `
        -UseBasicParsing `
        -ErrorAction Stop
        
    if ($corsResponse.Headers["Access-Control-Allow-Origin"]) {
        Write-Host "✅ CORS configured correctly" -ForegroundColor Green
        Write-Host "   Allowed origin: $($corsResponse.Headers["Access-Control-Allow-Origin"])" -ForegroundColor Gray
    } else {
        Write-Host "⚠️  CORS headers not found" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️  CORS test inconclusive: $_" -ForegroundColor Yellow
}

# Summary
Write-Host "`n=== Deployment Summary ===" -ForegroundColor Cyan
Write-Host "API URL: $apiUrl" -ForegroundColor White
Write-Host "UI URL: $uiUrl" -ForegroundColor White
Write-Host ""
Write-Host "To use the application:" -ForegroundColor Yellow
Write-Host "1. Open $uiUrl" -ForegroundColor White
Write-Host "2. Login with:" -ForegroundColor White
Write-Host "   Username: $username" -ForegroundColor Gray
Write-Host "   Password: $password" -ForegroundColor Gray
Write-Host ""
Write-Host "✅ Deployment verification complete" -ForegroundColor Green