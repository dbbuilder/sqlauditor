# Test API Login
Write-Host "Testing API login endpoint..." -ForegroundColor Cyan

$loginData = @{
    Username = "admin"
    Password = "AnalyzeThis!!"
} | ConvertTo-Json

$headers = @{
    "Content-Type" = "application/json"
    "Origin" = "https://black-desert-02d93d30f.2.azurestaticapps.net"
}

try {
    $response = Invoke-RestMethod `
        -Uri "https://sqlanalyzer-api.azurewebsites.net/api/v1/auth/login" `
        -Method POST `
        -Headers $headers `
        -Body $loginData
    
    Write-Host "✅ Login successful!" -ForegroundColor Green
    Write-Host "Token: $($response.token.Substring(0, 20))..." -ForegroundColor Gray
    Write-Host "Username: $($response.username)" -ForegroundColor Gray
    Write-Host "Expires: $($response.expiresAt)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Login failed!" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Yellow
    }
}

Write-Host "`nTesting CORS headers..." -ForegroundColor Cyan
$corsResponse = Invoke-WebRequest `
    -Uri "https://sqlanalyzer-api.azurewebsites.net/api/v1/auth/login" `
    -Method OPTIONS `
    -Headers @{"Origin" = "https://black-desert-02d93d30f.2.azurestaticapps.net"} `
    -UseBasicParsing

Write-Host "CORS Headers:" -ForegroundColor Yellow
$corsResponse.Headers | Where-Object { $_.Key -like "*cors*" -or $_.Key -like "*origin*" } | Format-Table