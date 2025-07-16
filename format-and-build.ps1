# Quick Format and Build Check

Write-Host "Running format and build check..." -ForegroundColor Cyan

Set-Location -Path "api"

# Format check
Write-Host "`nChecking code formatting..." -ForegroundColor Yellow
dotnet format --verify-no-changes

if ($LASTEXITCODE -ne 0) {
    Write-Host "`nFormatting issues found. Fixing..." -ForegroundColor Yellow
    dotnet format
    Write-Host "Formatting complete!" -ForegroundColor Green
    Write-Host "`nPlease commit these changes:" -ForegroundColor Yellow
    Write-Host "  git add -A" -ForegroundColor White
    Write-Host "  git commit -m 'style: Apply dotnet format for Hangfire integration'" -ForegroundColor White
} else {
    Write-Host "Code formatting is good!" -ForegroundColor Green
}

# Build check
Write-Host "`nBuilding solution..." -ForegroundColor Yellow
dotnet build --configuration Release

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nBuild successful!" -ForegroundColor Green
    Write-Host "`nReady to deploy with:" -ForegroundColor Cyan
    Write-Host "  git push" -ForegroundColor White
} else {
    Write-Host "`nBuild failed! Please fix errors before deploying." -ForegroundColor Red
}

Set-Location -Path ".."