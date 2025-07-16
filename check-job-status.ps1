# Check Analysis Job Status

param(
    [Parameter(Mandatory=$false)]
    [string]$JobId = "b02d35b7-9e48-4ee0-b634-d439fea8fb39",
    
    [Parameter(Mandatory=$false)]
    [string]$ApiUrl = "https://sqlanalyzer-api-win.azurewebsites.net"
)

Write-Host "Checking Analysis Job Status" -ForegroundColor Cyan
Write-Host "Job ID: $JobId" -ForegroundColor White
Write-Host "API URL: $ApiUrl" -ForegroundColor White
Write-Host "================================" -ForegroundColor Cyan

# Login first
Write-Host "`nLogging in..." -ForegroundColor Yellow
$loginBody = @{
    username = "admin"
    password = "AnalyzeThis!!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$ApiUrl/api/v1/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "✓ Login successful" -ForegroundColor Green
} catch {
    Write-Host "✗ Login failed: $_" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
}

# Check job status
Write-Host "`nChecking job status..." -ForegroundColor Yellow
try {
    $status = Invoke-RestMethod -Uri "$ApiUrl/api/v1/analysis/status/$JobId" -Method GET -Headers $headers
    
    Write-Host "`nJob Status:" -ForegroundColor Green
    Write-Host "  Status: $($status.status)" -ForegroundColor White
    Write-Host "  Progress: $($status.progressPercentage)%" -ForegroundColor White
    Write-Host "  Current Step: $($status.currentStep)" -ForegroundColor White
    Write-Host "  Started At: $($status.startedAt)" -ForegroundColor White
    
    if ($status.completedAt) {
        Write-Host "  Completed At: $($status.completedAt)" -ForegroundColor White
    }
    
    if ($status.errorMessage) {
        Write-Host "  Error: $($status.errorMessage)" -ForegroundColor Red
    }
    
} catch {
    Write-Host "✗ Job not found or error getting status" -ForegroundColor Red
    Write-Host "Error details: $_" -ForegroundColor Yellow
    
    # Try to get results in case job is completed
    Write-Host "`nTrying to get results..." -ForegroundColor Yellow
    try {
        $results = Invoke-RestMethod -Uri "$ApiUrl/api/v1/analysis/results/$JobId" -Method GET -Headers $headers
        Write-Host "✓ Results found! Job must have completed." -ForegroundColor Green
        Write-Host "  Database: $($results.database.name)" -ForegroundColor White
        Write-Host "  Findings: $($results.findings.Count)" -ForegroundColor White
    } catch {
        Write-Host "✗ No results found either" -ForegroundColor Red
    }
}

# Check Hangfire stats to see if it's working
Write-Host "`nChecking Hangfire system status..." -ForegroundColor Yellow
try {
    $stats = Invoke-RestMethod -Uri "$ApiUrl/api/v1/hangfire/stats" -Method GET -Headers $headers
    Write-Host "Hangfire Statistics:" -ForegroundColor Green
    Write-Host "  Servers: $($stats.servers)" -ForegroundColor White
    Write-Host "  Enqueued: $($stats.enqueued)" -ForegroundColor White
    Write-Host "  Processing: $($stats.processing)" -ForegroundColor White
    Write-Host "  Succeeded: $($stats.succeeded)" -ForegroundColor White
    Write-Host "  Failed: $($stats.failed)" -ForegroundColor White
} catch {
    Write-Host "⚠ Hangfire stats not available" -ForegroundColor Yellow
}

# Check recent analysis history
Write-Host "`nChecking recent analysis history..." -ForegroundColor Yellow
try {
    $history = Invoke-RestMethod -Uri "$ApiUrl/api/v1/analysis/history?page=1&pageSize=5" -Method GET -Headers $headers
    
    if ($history.Count -gt 0) {
        Write-Host "Recent analyses:" -ForegroundColor Green
        foreach ($item in $history) {
            $status = if ($item.status -eq "Completed") { "✓" } elseif ($item.status -eq "Failed") { "✗" } else { "⚠" }
            Write-Host "  $status $($item.jobId.Substring(0,8))... - $($item.status) - $($item.startedAt)" -ForegroundColor White
        }
    } else {
        Write-Host "No recent analyses found" -ForegroundColor Yellow
    }
} catch {
    Write-Host "Could not get analysis history" -ForegroundColor Yellow
}

Write-Host "`n================================" -ForegroundColor Cyan
Write-Host "Troubleshooting:" -ForegroundColor Cyan
Write-Host "1. If job not found, it may have been lost during deployment" -ForegroundColor White
Write-Host "2. Hangfire is using in-memory storage by default" -ForegroundColor White
Write-Host "3. Jobs are lost when the app restarts" -ForegroundColor White
Write-Host "4. To persist jobs, configure SQL Server storage for Hangfire" -ForegroundColor White