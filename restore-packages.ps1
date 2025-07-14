Write-Host "SQL Analyzer - Package Restore Script" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green

# Get current directory
$currentDir = Get-Location
Write-Host "Current directory: $currentDir" -ForegroundColor Yellow

# Clean all obj and bin folders
Write-Host "`nCleaning obj and bin folders..." -ForegroundColor Yellow
Get-ChildItem -Path . -Include bin,obj -Recurse -Directory | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "Clean complete!" -ForegroundColor Green

# Clear NuGet cache for the solution
Write-Host "`nClearing NuGet cache..." -ForegroundColor Yellow
dotnet nuget locals all --clear
Write-Host "NuGet cache cleared!" -ForegroundColor Green

# Restore packages for the entire solution
Write-Host "`nRestoring packages for solution..." -ForegroundColor Yellow
$restoreResult = dotnet restore SqlAnalyzer.sln --force --no-cache -v normal 2>&1 | Out-String
Write-Host $restoreResult

# Check if restore was successful
if ($LASTEXITCODE -eq 0) {
    Write-Host "`nPackage restore successful!" -ForegroundColor Green
    
    # List restored packages
    Write-Host "`nRestored packages:" -ForegroundColor Yellow
    Get-ChildItem -Path . -Filter "*.csproj" -Recurse | ForEach-Object {
        Write-Host "`nProject: $($_.FullName)" -ForegroundColor Cyan
        dotnet list $_.FullName package
    }
} else {
    Write-Host "`nPackage restore failed!" -ForegroundColor Red
    Write-Host "Exit code: $LASTEXITCODE" -ForegroundColor Red
}

# Try to build the solution
Write-Host "`n`nAttempting to build solution..." -ForegroundColor Yellow
$buildResult = dotnet build SqlAnalyzer.sln --no-restore -v minimal 2>&1 | Out-String
Write-Host $buildResult

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nBuild successful!" -ForegroundColor Green
} else {
    Write-Host "`nBuild failed!" -ForegroundColor Red
    Write-Host "Exit code: $LASTEXITCODE" -ForegroundColor Red
}

# Try to run tests
Write-Host "`n`nAttempting to run tests..." -ForegroundColor Yellow
$testResult = dotnet test tests/SqlAnalyzer.Tests/SqlAnalyzer.Tests.csproj --no-build --no-restore --logger "console;verbosity=normal" 2>&1 | Out-String
Write-Host $testResult

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nTests passed!" -ForegroundColor Green
} else {
    Write-Host "`nTests failed or could not run!" -ForegroundColor Red
    Write-Host "Exit code: $LASTEXITCODE" -ForegroundColor Red
}

Write-Host "`n`nScript complete!" -ForegroundColor Green