# Test Hangfire API Integration

$apiUrl = "https://sqlanalyzer-api-win.azurewebsites.net"
# $apiUrl = "https://localhost:7234"  # Uncomment for local testing

Write-Host "Testing Hangfire Integration..." -ForegroundColor Cyan

# Step 1: Login
Write-Host "`n1. Logging in..." -ForegroundColor Yellow
$loginBody = @{
    username = "admin"
    password = "AnalyzeThis!!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$apiUrl/api/v1/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "[OK] Login successful" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Login failed: $_" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
}

# Step 2: Check Hangfire Stats
Write-Host "`n2. Checking Hangfire stats..." -ForegroundColor Yellow
try {
    $stats = Invoke-RestMethod -Uri "$apiUrl/api/v1/hangfire/stats" -Method GET -Headers $headers
    Write-Host "Hangfire Statistics:" -ForegroundColor Green
    Write-Host "  Servers: $($stats.servers)" -ForegroundColor White
    Write-Host "  Enqueued: $($stats.enqueued)" -ForegroundColor White
    Write-Host "  Processing: $($stats.processing)" -ForegroundColor White
    Write-Host "  Succeeded: $($stats.succeeded)" -ForegroundColor White
    Write-Host "  Failed: $($stats.failed)" -ForegroundColor White
} catch {
    Write-Host "Warning: Could not get Hangfire stats - might not be deployed yet" -ForegroundColor Yellow
}

# Step 3: Start a test analysis
Write-Host "`n3. Starting test analysis..." -ForegroundColor Yellow
$analysisBody = @{
    connectionString = "Server=172.31.208.1,14333;Database=master;User Id=sv;Password=YourPassword;TrustServerCertificate=true"
    databaseType = "SqlServer"
    analysisType = "quick"
    options = @{
        includeIndexAnalysis = $true
        includeQueryPerformance = $false
        includeSecurityAudit = $false
    }
    notificationEmail = "test@example.com"
} | ConvertTo-Json

try {
    $startResponse = Invoke-RestMethod -Uri "$apiUrl/api/v1/analysis/start" -Method POST -Headers $headers -Body $analysisBody -ContentType "application/json"
    $jobId = $startResponse.jobId
    Write-Host "[OK] Analysis started with Job ID: $jobId" -ForegroundColor Green
    
    # Step 4: Check job status
    Write-Host "`n4. Monitoring job progress..." -ForegroundColor Yellow
    for ($i = 0; $i -lt 10; $i++) {
        Start-Sleep -Seconds 2
        
        try {
            $status = Invoke-RestMethod -Uri "$apiUrl/api/v1/analysis/status/$jobId" -Method GET -Headers $headers
            Write-Host "  Status: $($status.status) | Progress: $($status.progressPercentage)% | Step: $($status.currentStep)" -ForegroundColor White
            
            if ($status.status -eq "Completed" -or $status.status -eq "Failed") {
                break
            }
        } catch {
            Write-Host "  Could not get status: $_" -ForegroundColor Yellow
        }
    }
    
} catch {
    Write-Host "[ERROR] Failed to start analysis: $_" -ForegroundColor Red
}

Write-Host "`n================================" -ForegroundColor Cyan
Write-Host "Hangfire is working if:" -ForegroundColor Cyan
Write-Host "1. Job was created successfully" -ForegroundColor White
Write-Host "2. Progress updates were received" -ForegroundColor White
Write-Host "3. Job completed or is processing" -ForegroundColor White
Write-Host "`nNote: The Hangfire dashboard at /hangfire requires special auth setup" -ForegroundColor Yellow
Write-Host "Use the API endpoints instead for monitoring in production" -ForegroundColor Yellow