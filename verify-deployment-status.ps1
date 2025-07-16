# Verify SQL Analyzer Deployment

Write-Host "`nChecking SQL Analyzer Deployment Status..." -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

$apiUrl = "https://sqlanalyzer-api-win.azurewebsites.net"

# 1. Check Version Endpoint
Write-Host "`n1. Checking Version Endpoint..." -ForegroundColor Yellow
try {
    $version = Invoke-RestMethod -Uri "$apiUrl/api/version" -Method Get
    Write-Host "✅ API Version Response:" -ForegroundColor Green
    Write-Host "   Version: $($version.version.assembly)" -ForegroundColor White
    Write-Host "   Build: $($version.version.informational)" -ForegroundColor White
    Write-Host "   Environment: $($version.deployment.environment)" -ForegroundColor White
    Write-Host "   Timestamp: $($version.deployment.timestamp)" -ForegroundColor White
    Write-Host "   Commit: $($version.deployment.commit)" -ForegroundColor White
    Write-Host "   Build Number: $($version.deployment.buildNumber)" -ForegroundColor White
    Write-Host "   Uptime: $($version.health.uptime)" -ForegroundColor White
    
    # Check if this is the new deployment
    if ($version.deployment.timestamp -eq "Local Build") {
        Write-Host "`n⚠️  Old deployment detected - new changes not yet deployed" -ForegroundColor Yellow
    } else {
        Write-Host "`n✅ New deployment detected with build timestamp!" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ Version endpoint failed: $_" -ForegroundColor Red
}

# 2. Test Authentication
Write-Host "`n2. Getting Authentication Token..." -ForegroundColor Yellow
$loginBody = @{
    username = "admin"
    password = "AnalyzeThis!!"
} | ConvertTo-Json

try {
    $auth = Invoke-RestMethod -Uri "$apiUrl/api/v1/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $auth.token
    Write-Host "✅ Authentication successful" -ForegroundColor Green
    
    # 3. Test Email Status Endpoint
    Write-Host "`n3. Testing Email Status Endpoint..." -ForegroundColor Yellow
    $headers = @{
        "Authorization" = "Bearer $token"
    }
    
    try {
        $emailStatus = Invoke-RestMethod -Uri "$apiUrl/api/v1/email/status" -Method Get -Headers $headers
        Write-Host "✅ Email endpoint is available!" -ForegroundColor Green
        Write-Host "   Enabled: $($emailStatus.enabled)" -ForegroundColor White
        Write-Host "   Configured: $($emailStatus.configured)" -ForegroundColor White
        Write-Host "   Provider: $($emailStatus.provider)" -ForegroundColor White
        Write-Host "   From Email: $($emailStatus.fromEmail)" -ForegroundColor White
    } catch {
        if ($_.Exception.Response.StatusCode -eq 404) {
            Write-Host "❌ Email endpoints not deployed yet (404)" -ForegroundColor Red
            Write-Host "   The EmailController is not available in this deployment" -ForegroundColor Yellow
        } else {
            Write-Host "❌ Email status check failed: $_" -ForegroundColor Red
        }
    }
    
    # 4. Test Email Test Endpoint
    Write-Host "`n4. Testing Email Test Endpoint..." -ForegroundColor Yellow
    $testEmailBody = @{
        email = "test@example.com"
    } | ConvertTo-Json
    
    try {
        $emailTest = Invoke-RestMethod -Uri "$apiUrl/api/v1/email/test" -Method Post -Headers $headers -Body $testEmailBody -ContentType "application/json"
        Write-Host "✅ Email test endpoint is working!" -ForegroundColor Green
        Write-Host "   Success: $($emailTest.success)" -ForegroundColor White
        Write-Host "   Message: $($emailTest.message)" -ForegroundColor White
    } catch {
        if ($_.Exception.Response.StatusCode -eq 404) {
            Write-Host "❌ Email test endpoint not available (404)" -ForegroundColor Red
        } else {
            Write-Host "❌ Email test failed: $_" -ForegroundColor Red
        }
    }
    
} catch {
    Write-Host "❌ Authentication failed: $_" -ForegroundColor Red
}

Write-Host "`n=========================================" -ForegroundColor Cyan
Write-Host "Deployment Status Check Complete" -ForegroundColor Cyan

# Summary
Write-Host "`nSUMMARY:" -ForegroundColor Magenta
Write-Host "Check the GitHub Actions page for deployment progress:" -ForegroundColor White
Write-Host "https://github.com/dbbuilder/sqlauditor/actions" -ForegroundColor Cyan
Write-Host "`nThe deployment typically takes 5-7 minutes to complete." -ForegroundColor Yellow
Write-Host "If email endpoints are still returning 404, the deployment may still be in progress." -ForegroundColor Yellow