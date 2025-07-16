# Testing Hangfire Integration Locally

## Quick Start

1. **Run the test script**:
   ```powershell
   .\test-and-format.ps1
   ```

2. **Manual Testing Steps**

### Step 1: Start the API
```bash
cd api
dotnet run --project SqlAnalyzer.Api/SqlAnalyzer.Api.csproj
```

### Step 2: Access Hangfire Dashboard
- Navigate to: https://localhost:7234/hangfire
- In development, it should be accessible without authentication
- You'll see the Hangfire dashboard with:
  - Jobs queue
  - Processing statistics
  - Server information

### Step 3: Test Analysis with Hangfire

```powershell
# 1. Get JWT token
$loginResponse = Invoke-RestMethod -Uri "https://localhost:7234/api/v1/auth/login" -Method POST -Body (@{
    username = "admin"
    password = "AnalyzeThis!!"
} | ConvertTo-Json) -ContentType "application/json" -SkipCertificateCheck

$token = $loginResponse.token

# 2. Start an analysis job
$headers = @{
    "Authorization" = "Bearer $token"
}

$analysisRequest = @{
    connectionString = "Server=172.31.208.1,14333;Database=YourDB;User Id=sv;Password=YourPassword;TrustServerCertificate=true"
    databaseType = "SqlServer"
    analysisType = "quick"
    options = @{
        includeIndexAnalysis = $true
        includeQueryPerformance = $true
        includeSecurityAudit = $false
    }
    notificationEmail = "test@example.com"
} | ConvertTo-Json

$startResponse = Invoke-RestMethod -Uri "https://localhost:7234/api/v1/analysis/start" -Method POST -Headers $headers -Body $analysisRequest -ContentType "application/json" -SkipCertificateCheck

Write-Host "Job ID: $($startResponse.jobId)"

# 3. Check job status
$statusResponse = Invoke-RestMethod -Uri "https://localhost:7234/api/v1/analysis/status/$($startResponse.jobId)" -Method GET -Headers $headers -SkipCertificateCheck

Write-Host "Status: $($statusResponse.status)"
Write-Host "Progress: $($statusResponse.progressPercentage)%"
Write-Host "Current Step: $($statusResponse.currentStep)"
```

### Step 4: Monitor in Hangfire Dashboard
1. Go to https://localhost:7234/hangfire
2. Click on "Jobs" → "Enqueued" or "Processing"
3. You should see your analysis job
4. Click on the job to see details

### Step 5: Test Job Persistence
1. Start an analysis job (note the Job ID)
2. Stop the API (Ctrl+C)
3. Check that the job is still in Hangfire storage
4. Restart the API
5. The job should resume processing

## Testing Checklist

### Before Deployment

- [ ] Code formatting passes (`dotnet format --verify-no-changes`)
- [ ] Build succeeds (`dotnet build`)
- [ ] All tests pass (`dotnet test`)
- [ ] API starts without errors
- [ ] Hangfire dashboard is accessible
- [ ] Jobs can be enqueued successfully
- [ ] Real-time progress updates work (if SignalR enabled)
- [ ] Email notifications are sent (if configured)

### Configuration Testing

1. **In-Memory Storage (Development)**
   ```json
   {
     "Hangfire": {
       "UseInMemoryStorage": true
     }
   }
   ```
   - Jobs process immediately
   - Jobs lost on restart (expected)

2. **SQL Server Storage (Production)**
   ```json
   {
     "ConnectionStrings": {
       "HangfireConnection": "Server=...;Database=HangfireDB;..."
     },
     "Hangfire": {
       "UseInMemoryStorage": false
     }
   }
   ```
   - Jobs persist across restarts
   - Can scale to multiple servers

### Common Issues and Solutions

1. **Hangfire Dashboard 404**
   - Ensure `app.UseHangfireDashboard()` is called in Program.cs
   - Check the path configuration (default: /hangfire)

2. **Jobs Not Processing**
   - Check if Hangfire server is registered: `AddHangfireServer()`
   - Verify worker count configuration
   - Check for exceptions in job execution

3. **Authentication Issues**
   - In dev: Should work without auth
   - In prod: Requires valid JWT token

4. **Database Connection Issues**
   - For SQL storage: Ensure connection string is valid
   - Hangfire auto-creates tables on first run
   - Check firewall rules

## Performance Testing

```powershell
# Start multiple jobs to test concurrency
$jobIds = @()
for ($i = 1; $i -le 5; $i++) {
    $response = Invoke-RestMethod -Uri "https://localhost:7234/api/v1/analysis/start" -Method POST -Headers $headers -Body $analysisRequest -ContentType "application/json" -SkipCertificateCheck
    $jobIds += $response.jobId
    Write-Host "Started job $i: $($response.jobId)"
}

# Monitor all jobs
foreach ($jobId in $jobIds) {
    $status = Invoke-RestMethod -Uri "https://localhost:7234/api/v1/analysis/status/$jobId" -Method GET -Headers $headers -SkipCertificateCheck
    Write-Host "Job $jobId: $($status.status) - $($status.progressPercentage)%"
}
```

## Deployment Readiness

After testing, ensure:
1. All tests pass
2. No formatting issues
3. Hangfire dashboard works
4. Jobs process successfully
5. Configuration is set correctly for production