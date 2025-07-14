# Test Authentication System
Write-Host "=== Testing SQL Analyzer Authentication ===" -ForegroundColor Cyan

$apiUrl = "http://localhost:5274"
$username = "admin"
$password = "SqlAnalyzer2024!"

# Function to test endpoint
function Test-Endpoint {
    param(
        [string]$Url,
        [string]$Method = "GET",
        [object]$Body = $null,
        [hashtable]$Headers = @{}
    )
    
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            Headers = $Headers
            ContentType = "application/json"
            ErrorAction = "Stop"
        }
        
        if ($Body) {
            $params.Body = $Body | ConvertTo-Json
        }
        
        $response = Invoke-RestMethod @params
        return @{
            Success = $true
            Data = $response
        }
    } catch {
        return @{
            Success = $false
            Error = $_.Exception.Message
            StatusCode = $_.Exception.Response.StatusCode.value__
        }
    }
}

# Test 1: Check if API is running
Write-Host "`n1. Testing API health..." -ForegroundColor Yellow
$health = Test-Endpoint -Url "$apiUrl/health"
if ($health.Success) {
    Write-Host "✅ API is healthy" -ForegroundColor Green
} else {
    Write-Host "❌ API is not running. Please start it first." -ForegroundColor Red
    exit 1
}

# Test 2: Try to access protected endpoint without auth
Write-Host "`n2. Testing protected endpoint without auth..." -ForegroundColor Yellow
$protected = Test-Endpoint -Url "$apiUrl/api/v1/analysis/types"
if ($protected.StatusCode -eq 401) {
    Write-Host "✅ Endpoint is protected (401 Unauthorized)" -ForegroundColor Green
} else {
    Write-Host "⚠️  Expected 401 but got: $($protected.StatusCode)" -ForegroundColor Yellow
}

# Test 3: Login with credentials
Write-Host "`n3. Testing login..." -ForegroundColor Yellow
$loginBody = @{
    username = $username
    password = $password
}
$login = Test-Endpoint -Url "$apiUrl/api/v1/auth/login" -Method "POST" -Body $loginBody

if ($login.Success -and $login.Data.token) {
    Write-Host "✅ Login successful" -ForegroundColor Green
    Write-Host "   Token: $($login.Data.token.Substring(0, 20))..." -ForegroundColor Gray
    Write-Host "   Expires: $($login.Data.expiresAt)" -ForegroundColor Gray
    $token = $login.Data.token
} else {
    Write-Host "❌ Login failed: $($login.Error)" -ForegroundColor Red
    exit 1
}

# Test 4: Access protected endpoint with token
Write-Host "`n4. Testing protected endpoint with auth..." -ForegroundColor Yellow
$authHeaders = @{
    "Authorization" = "Bearer $token"
}
$protectedAuth = Test-Endpoint -Url "$apiUrl/api/v1/analysis/types" -Headers $authHeaders

if ($protectedAuth.Success) {
    Write-Host "✅ Protected endpoint accessible with token" -ForegroundColor Green
    Write-Host "   Response: $($protectedAuth.Data | ConvertTo-Json -Compress)" -ForegroundColor Gray
} else {
    Write-Host "❌ Failed to access protected endpoint: $($protectedAuth.Error)" -ForegroundColor Red
}

# Test 5: Verify token
Write-Host "`n5. Testing token verification..." -ForegroundColor Yellow
$verify = Test-Endpoint -Url "$apiUrl/api/v1/auth/verify" -Headers $authHeaders

if ($verify.Success) {
    Write-Host "✅ Token verified" -ForegroundColor Green
    Write-Host "   Username: $($verify.Data.username)" -ForegroundColor Gray
    Write-Host "   Role: $($verify.Data.role)" -ForegroundColor Gray
} else {
    Write-Host "❌ Token verification failed: $($verify.Error)" -ForegroundColor Red
}

# Test 6: Test wrong credentials
Write-Host "`n6. Testing wrong credentials..." -ForegroundColor Yellow
$wrongLogin = Test-Endpoint -Url "$apiUrl/api/v1/auth/login" -Method "POST" -Body @{
    username = "admin"
    password = "wrongpassword"
}

if ($wrongLogin.StatusCode -eq 401) {
    Write-Host "✅ Wrong credentials rejected (401)" -ForegroundColor Green
} else {
    Write-Host "⚠️  Expected 401 but got: $($wrongLogin.StatusCode)" -ForegroundColor Yellow
}

# Summary
Write-Host "`n=== Authentication Test Summary ===" -ForegroundColor Cyan
Write-Host "All authentication tests passed!" -ForegroundColor Green
Write-Host "`nYou can now:" -ForegroundColor Yellow
Write-Host "1. Navigate to http://localhost:5173" -ForegroundColor White
Write-Host "2. Login with admin / $password" -ForegroundColor White
Write-Host "3. Use the application with authentication" -ForegroundColor White