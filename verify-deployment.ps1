# Deployment Verification Script
param(
    [Parameter(Mandatory=$false)]
    [string]$ApiUrl = "https://sqlanalyzer-api.azurewebsites.net",
    
    [Parameter(Mandatory=$false)]
    [string]$ExpectedVersion = "",
    
    [Parameter(Mandatory=$false)]
    [string]$ExpectedDeploymentId = "",
    
    [switch]$Detailed
)

Write-Host "🔍 SQL Analyzer Deployment Verification" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Target: $ApiUrl" -ForegroundColor Yellow

$results = @{
    Success = $true
    Tests = @()
}

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Endpoint,
        [scriptblock]$Validation = { $true }
    )
    
    Write-Host "`n🧪 Testing: $Name" -ForegroundColor Yellow
    Write-Host "   Endpoint: $Endpoint" -ForegroundColor Gray
    
    $test = @{
        Name = $Name
        Endpoint = $Endpoint
        Success = $false
        Response = $null
        Error = $null
        Duration = 0
    }
    
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    
    try {
        $response = Invoke-RestMethod -Uri $Endpoint -Method Get -TimeoutSec 30
        $test.Response = $response
        $test.Duration = $stopwatch.ElapsedMilliseconds
        
        if (& $Validation) {
            Write-Host "   ✅ Success ($($test.Duration)ms)" -ForegroundColor Green
            $test.Success = $true
        } else {
            Write-Host "   ❌ Validation failed" -ForegroundColor Red
            $script:results.Success = $false
        }
        
        if ($Detailed) {
            Write-Host "   Response:" -ForegroundColor Gray
            $response | ConvertTo-Json -Depth 10 | Write-Host -ForegroundColor Gray
        }
        
    } catch {
        $test.Error = $_.Exception.Message
        Write-Host "   ❌ Failed: $_" -ForegroundColor Red
        $script:results.Success = $false
    }
    
    $stopwatch.Stop()
    $results.Tests += $test
    return $test
}

# Test 1: Version endpoint
$versionTest = Test-Endpoint -Name "Version API" -Endpoint "$ApiUrl/api/version" -Validation {
    $response.version -and $response.deployment -and $response.runtime
}

if ($versionTest.Success -and $versionTest.Response) {
    $currentVersion = $versionTest.Response.version.assembly
    $currentDeploymentId = $versionTest.Response.deployment.deploymentId
    
    Write-Host "`n📋 Deployment Info:" -ForegroundColor Cyan
    Write-Host "   Version: $currentVersion" -ForegroundColor White
    Write-Host "   Deployment ID: $currentDeploymentId" -ForegroundColor White
    Write-Host "   Environment: $($versionTest.Response.deployment.environment)" -ForegroundColor White
    Write-Host "   Commit: $($versionTest.Response.deployment.commit)" -ForegroundColor White
    Write-Host "   Build: #$($versionTest.Response.deployment.buildNumber)" -ForegroundColor White
    Write-Host "   Runtime: $($versionTest.Response.runtime.framework)" -ForegroundColor White
    Write-Host "   OS: $($versionTest.Response.runtime.os)" -ForegroundColor White
    
    # Version validation
    if ($ExpectedVersion -and $currentVersion -ne $ExpectedVersion) {
        Write-Host "`n⚠️  Version Mismatch!" -ForegroundColor Yellow
        Write-Host "   Expected: $ExpectedVersion" -ForegroundColor Yellow
        Write-Host "   Actual: $currentVersion" -ForegroundColor Yellow
        $results.Success = $false
    }
    
    # Deployment ID validation
    if ($ExpectedDeploymentId -and $currentDeploymentId -ne $ExpectedDeploymentId) {
        Write-Host "`n⚠️  Deployment ID Mismatch!" -ForegroundColor Yellow
        Write-Host "   Expected: $ExpectedDeploymentId" -ForegroundColor Yellow
        Write-Host "   Actual: $currentDeploymentId" -ForegroundColor Yellow
        $results.Success = $false
    }
}

# Test 2: Health endpoint
Test-Endpoint -Name "Health Check" -Endpoint "$ApiUrl/api/version/health" -Validation {
    $response.status -eq "Healthy"
} | Out-Null

# Test 3: Main health check
Test-Endpoint -Name "Main Health" -Endpoint "$ApiUrl/health" -Validation {
    $response.status -eq "Healthy"
} | Out-Null

# Test 4: Analysis types endpoint
Test-Endpoint -Name "Analysis Types" -Endpoint "$ApiUrl/api/v1/analysis/types" -Validation {
    $response -is [array] -and $response.Count -gt 0
} | Out-Null

# Test 5: Swagger/OpenAPI
Test-Endpoint -Name "Swagger UI" -Endpoint "$ApiUrl/swagger/index.html" -Validation {
    $true  # Just check if accessible
} | Out-Null

# Performance summary
Write-Host "`n📊 Performance Summary:" -ForegroundColor Cyan
$avgResponseTime = ($results.Tests | Where-Object { $_.Success } | Measure-Object -Property Duration -Average).Average
Write-Host "   Average Response Time: $([Math]::Round($avgResponseTime, 2))ms" -ForegroundColor White

$slowest = $results.Tests | Where-Object { $_.Success } | Sort-Object -Property Duration -Descending | Select-Object -First 1
if ($slowest) {
    Write-Host "   Slowest Endpoint: $($slowest.Name) ($($slowest.Duration)ms)" -ForegroundColor Yellow
}

# Final summary
Write-Host "`n📈 Test Summary:" -ForegroundColor Cyan
Write-Host "==================" -ForegroundColor Cyan

$passedTests = ($results.Tests | Where-Object { $_.Success }).Count
$totalTests = $results.Tests.Count

Write-Host "Passed: $passedTests/$totalTests" -ForegroundColor $(if ($passedTests -eq $totalTests) { "Green" } else { "Yellow" })

foreach ($test in $results.Tests) {
    $icon = if ($test.Success) { "✅" } else { "❌" }
    $color = if ($test.Success) { "Green" } else { "Red" }
    Write-Host "$icon $($test.Name)" -ForegroundColor $color
}

# Exit code
if ($results.Success) {
    Write-Host "`n✅ Deployment verification PASSED!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n❌ Deployment verification FAILED!" -ForegroundColor Red
    exit 1
}