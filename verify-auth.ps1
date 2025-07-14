# Verify SQL Analyzer Authentication
$response = Invoke-RestMethod `
    -Uri "https://sqlanalyzer-api.azurewebsites.net/api/v1/auth/login" `
    -Method POST `
    -Body (@{username="admin"; password="AnalyzeThis!!"} | ConvertTo-Json) `
    -ContentType "application/json"

if ($response.token) {
    Write-Host "✅ Login successful!" -ForegroundColor Green
    Write-Host "Token: $($response.token.Substring(0,50))..." -ForegroundColor Gray
} else {
    Write-Host "❌ Login failed" -ForegroundColor Red
}
