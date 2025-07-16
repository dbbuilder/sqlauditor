# Fix Code Formatting Issues

Write-Host "Fixing Code Formatting Issues..." -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Navigate to API directory
Set-Location -Path "api"

# Run dotnet format
Write-Host "`nRunning dotnet format..." -ForegroundColor Yellow
dotnet format

Write-Host "`nFormatting complete!" -ForegroundColor Green
Write-Host "Now commit and push the formatting fixes:" -ForegroundColor Yellow
Write-Host "  git add -A" -ForegroundColor White
Write-Host "  git commit -m 'fix: Apply dotnet format to fix code quality issues'" -ForegroundColor White
Write-Host "  git push" -ForegroundColor White

# Return to root directory
Set-Location -Path ".."