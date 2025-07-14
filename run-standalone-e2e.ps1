Write-Host "Compiling Standalone E2E Test..." -ForegroundColor Yellow

# Compile the standalone test
$compileResult = & csc.exe /out:StandaloneE2ETest.exe /reference:System.Data.dll /reference:System.Data.SqlClient.dll StandaloneE2ETest.cs 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "Compilation failed!" -ForegroundColor Red
    Write-Host $compileResult
    exit 1
}

Write-Host "Compilation successful!" -ForegroundColor Green
Write-Host ""

# Run the test
Write-Host "Running Standalone E2E Test..." -ForegroundColor Green
Write-Host "=============================" -ForegroundColor Green
Write-Host ""

.\StandaloneE2ETest.exe

$testResult = $LASTEXITCODE

# Cleanup
Remove-Item "StandaloneE2ETest.exe" -ErrorAction SilentlyContinue

if ($testResult -eq 0) {
    Write-Host ""
    Write-Host "All tests passed!" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "Some tests failed!" -ForegroundColor Red
    exit 1
}