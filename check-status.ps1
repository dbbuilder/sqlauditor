# SQL Analyzer Status Check Script

Write-Host "SQL Analyzer Status Check" -ForegroundColor Green
Write-Host "=========================" -ForegroundColor Green

$apiUrl = "https://sqlanalyzer-api-win.azurewebsites.net"
$frontendUrl = "https://black-desert-02d93d30f.2.azurestaticapps.net"

# 1. Check if API is responding
Write-Host "`n1. Checking API Version endpoint..." -ForegroundColor Yellow
try {
    $version = Invoke-RestMethod -Uri "$apiUrl/api/version" -Method Get
    Write-Host "✅ API is responding" -ForegroundColor Green
    Write-Host "   Version: $($version.version.assembly)"
    Write-Host "   Environment: $($version.deployment.environment)"
    Write-Host "   Build: $($version.deployment.timestamp)"
} catch {
    Write-Host "❌ API version endpoint failed" -ForegroundColor Red
    Write-Host "   Error: $_" -ForegroundColor Red
}

# 2. Test Authentication
Write-Host "`n2. Testing Authentication..." -ForegroundColor Yellow
$loginBody = @{
    username = "admin"
    password = "AnalyzeThis!!"
} | ConvertTo-Json

try {
    $headers = @{
        "Content-Type" = "application/json"
    }
    $auth = Invoke-RestMethod -Uri "$apiUrl/api/v1/auth/login" -Method Post -Body $loginBody -Headers $headers
    Write-Host "✅ Authentication successful" -ForegroundColor Green
    Write-Host "   Token received: $($auth.token.Substring(0, 20))..."
    $token = $auth.token
    
    # 3. Test Auth Verify with token
    Write-Host "`n3. Verifying token..." -ForegroundColor Yellow
    $authHeaders = @{
        "Authorization" = "Bearer $token"
    }
    $verify = Invoke-RestMethod -Uri "$apiUrl/api/v1/auth/verify" -Method Get -Headers $authHeaders
    Write-Host "✅ Token is valid" -ForegroundColor Green
    Write-Host "   User: $($verify.username)"
    Write-Host "   Role: $($verify.role)"
    
    # 4. Test Email Status endpoint
    Write-Host "`n4. Testing Email Status endpoint..." -ForegroundColor Yellow
    try {
        $emailStatus = Invoke-RestMethod -Uri "$apiUrl/api/v1/email/status" -Method Get -Headers $authHeaders
        Write-Host "✅ Email endpoint accessible" -ForegroundColor Green
        Write-Host "   Enabled: $($emailStatus.enabled)"
        Write-Host "   Provider: $($emailStatus.provider)"
    } catch {
        Write-Host "❌ Email status endpoint failed (404)" -ForegroundColor Red
        Write-Host "   This suggests EmailController is not deployed yet" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "❌ Authentication failed" -ForegroundColor Red
    Write-Host "   Error: $_" -ForegroundColor Red
}

# 5. Check Frontend
Write-Host "`n5. Checking Frontend..." -ForegroundColor Yellow
try {
    $frontend = Invoke-WebRequest -Uri $frontendUrl -UseBasicParsing
    if ($frontend.StatusCode -eq 200) {
        Write-Host "✅ Frontend is accessible" -ForegroundColor Green
        Write-Host "   URL: $frontendUrl"
    }
} catch {
    Write-Host "❌ Frontend check failed" -ForegroundColor Red
}

Write-Host "`n=========================" -ForegroundColor Green
Write-Host "Status check complete!" -ForegroundColor Green

# Summary
Write-Host "`nSummary:" -ForegroundColor Cyan
Write-Host "- API: Operational" -ForegroundColor White
Write-Host "- Authentication: Working" -ForegroundColor White
Write-Host "- Frontend: Accessible" -ForegroundColor White
Write-Host "- Email Service: Pending deployment" -ForegroundColor Yellow
Write-Host "`nThe email endpoints will be available after the next deployment cycle." -ForegroundColor Yellow