# Test Hangfire Build

Write-Host "Building API with Hangfire integration..." -ForegroundColor Cyan

Set-Location -Path "api"

# Restore packages
Write-Host "`nRestoring NuGet packages..." -ForegroundColor Yellow
dotnet restore

# Build the solution
Write-Host "`nBuilding solution..." -ForegroundColor Yellow
dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nBuild successful!" -ForegroundColor Green
    Write-Host "`nHangfire features added:" -ForegroundColor Cyan
    Write-Host "- Persistent job storage (in-memory for dev, SQL Server for production)" -ForegroundColor White
    Write-Host "- Job dashboard at /hangfire (requires authentication)" -ForegroundColor White
    Write-Host "- Automatic retries for failed jobs" -ForegroundColor White
    Write-Host "- Better scalability across multiple servers" -ForegroundColor White
    Write-Host "- Jobs survive application restarts" -ForegroundColor White
    
    Write-Host "`nTo access Hangfire dashboard in production:" -ForegroundColor Yellow
    Write-Host "1. Navigate to https://sqlanalyzer-api-win.azurewebsites.net/hangfire" -ForegroundColor White
    Write-Host "2. Login with your admin credentials" -ForegroundColor White
    Write-Host "3. Monitor running jobs, view history, and manage retries" -ForegroundColor White
} else {
    Write-Host "`nBuild failed!" -ForegroundColor Red
}

Set-Location -Path ".."