# Quick Local Setup Script for SQL Analyzer
# This script sets up the application for local development with authentication

Write-Host "=== SQL Analyzer Local Setup ===" -ForegroundColor Cyan

# Step 1: Install backend dependencies
Write-Host "`nSetting up backend..." -ForegroundColor Yellow
Set-Location -Path "src/SqlAnalyzer.Api"
dotnet restore
Write-Host "✅ Backend dependencies installed" -ForegroundColor Green

# Step 2: Install frontend dependencies
Write-Host "`nSetting up frontend..." -ForegroundColor Yellow
Set-Location -Path "../SqlAnalyzer.Web"
npm install
Write-Host "✅ Frontend dependencies installed" -ForegroundColor Green

# Step 3: Create local environment files
Write-Host "`nCreating environment files..." -ForegroundColor Yellow
Set-Location -Path "../.."

# Create API local settings
$apiSettings = @'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Authentication": {
    "JwtSecret": "LOCAL_DEV_SECRET_CHANGE_IN_PRODUCTION_ABCDEF1234567890",
    "JwtExpirationHours": 24,
    "DefaultUsername": "admin",
    "DefaultPassword": "SqlAnalyzer2024!"
  },
  "SqlAnalyzer": {
    "AllowedOrigins": [
      "http://localhost:5173",
      "http://localhost:3000",
      "http://localhost:8080"
    ]
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=sqltest.schoolvision.net,14333;Database=SVDB_CaptureT;User Id=sv;Password=Gv51076!;TrustServerCertificate=true;"
  }
}
'@

$apiSettings | Set-Content "src/SqlAnalyzer.Api/appsettings.Development.json"
Write-Host "✅ API settings created" -ForegroundColor Green

# Create frontend .env
$frontendEnv = @'
VITE_API_URL=http://localhost:5274
VITE_ENABLE_MOCK_DATA=false
'@

$frontendEnv | Set-Content "src/SqlAnalyzer.Web/.env.development"
Write-Host "✅ Frontend environment created" -ForegroundColor Green

# Step 4: Create run scripts
Write-Host "`nCreating run scripts..." -ForegroundColor Yellow

# PowerShell run script
$runScript = @'
# Run SQL Analyzer Locally
Write-Host "Starting SQL Analyzer..." -ForegroundColor Cyan

# Start API in background
Write-Host "Starting API..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd src/SqlAnalyzer.Api; dotnet run"

# Wait for API to start
Start-Sleep -Seconds 5

# Start Frontend
Write-Host "Starting Frontend..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd src/SqlAnalyzer.Web; npm run dev"

Write-Host "`nSQL Analyzer is starting..." -ForegroundColor Green
Write-Host "API: http://localhost:5274" -ForegroundColor White
Write-Host "UI: http://localhost:5173" -ForegroundColor White
Write-Host "`nDefault login:" -ForegroundColor Yellow
Write-Host "Username: admin" -ForegroundColor White
Write-Host "Password: SqlAnalyzer2024!" -ForegroundColor White
'@

$runScript | Set-Content "run-local.ps1"

# Bash run script
$bashRunScript = @'
#!/bin/bash
# Run SQL Analyzer Locally

echo -e "\033[36mStarting SQL Analyzer...\033[0m"

# Start API in background
echo -e "\033[33mStarting API...\033[0m"
(cd src/SqlAnalyzer.Api && dotnet run) &
API_PID=$!

# Wait for API to start
sleep 5

# Start Frontend
echo -e "\033[33mStarting Frontend...\033[0m"
(cd src/SqlAnalyzer.Web && npm run dev) &
FRONTEND_PID=$!

echo -e "\n\033[32mSQL Analyzer is starting...\033[0m"
echo "API: http://localhost:5274"
echo "UI: http://localhost:5173"
echo -e "\n\033[33mDefault login:\033[0m"
echo "Username: admin"
echo "Password: SqlAnalyzer2024!"
echo -e "\n\033[90mPress Ctrl+C to stop all services\033[0m"

# Wait for Ctrl+C
trap "kill $API_PID $FRONTEND_PID; exit" INT
wait
'@

$bashRunScript | Set-Content "run-local.sh"

# Make bash script executable
if (Get-Command chmod -ErrorAction SilentlyContinue) {
    chmod +x run-local.sh
}

Write-Host "✅ Run scripts created" -ForegroundColor Green

# Step 5: Build everything
Write-Host "`nBuilding application..." -ForegroundColor Yellow
Set-Location -Path "src/SqlAnalyzer.Api"
dotnet build
Set-Location -Path "../SqlAnalyzer.Web"
npm run build
Set-Location -Path "../.."

Write-Host "`n✅ Local setup complete!" -ForegroundColor Green
Write-Host "`nTo run the application:" -ForegroundColor Yellow
Write-Host "  PowerShell: .\run-local.ps1" -ForegroundColor White
Write-Host "  Bash: ./run-local.sh" -ForegroundColor White
Write-Host "`nThe application will open with authentication enabled." -ForegroundColor Gray