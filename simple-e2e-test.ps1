# SQL Analyzer Simple E2E Test
Write-Host "`n=== SQL Analyzer End-to-End Test ===" -ForegroundColor Green
Write-Host "Testing basic functionality against SQL Server`n" -ForegroundColor Green

$connectionString = "Server=sqltest.schoolvision.net,14333;Database=SVDB_CaptureT;User Id=sv;Password=Gv51076!;TrustServerCertificate=true;"
$testsPassed = 0
$testsFailed = 0

function Test-Feature {
    param($Name, $Test)
    
    Write-Host -NoNewline "Testing $Name... "
    try {
        & $Test
        Write-Host "[PASSED]" -ForegroundColor Green
        $script:testsPassed++
    }
    catch {
        Write-Host "[FAILED] - $_" -ForegroundColor Red
        $script:testsFailed++
    }
}

# Test 1: Basic Connection
Test-Feature "Basic Connection" {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    if ($conn.State -ne "Open") { throw "Connection not open" }
    $conn.Close()
}

# Test 2: Read-Only Query with NOLOCK
Test-Feature "Read-Only Query (NOLOCK)" {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) FROM sys.tables WITH (NOLOCK)"
    $result = $cmd.ExecuteScalar()
    
    if ($result -le 0) { throw "No tables found" }
    $conn.Close()
}

# Test 3: Transaction Safety
Test-Feature "Transaction Safety" {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    
    $tran = $conn.BeginTransaction()
    try {
        $cmd = $conn.CreateCommand()
        $cmd.Transaction = $tran
        $cmd.CommandText = "SELECT TOP 1 name FROM sys.tables WITH (NOLOCK)"
        $result = $cmd.ExecuteScalar()
        $tran.Rollback()
    }
    catch {
        $tran.Rollback()
        throw
    }
    finally {
        $conn.Close()
    }
}

# Test 4: Metadata Analysis
Test-Feature "Metadata Analysis" {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
        SELECT TOP 5
            s.name AS SchemaName,
            t.name AS TableName,
            p.rows AS [RowCount]
        FROM sys.tables t WITH (NOLOCK)
        INNER JOIN sys.schemas s WITH (NOLOCK) ON t.schema_id = s.schema_id
        INNER JOIN sys.partitions p WITH (NOLOCK) ON t.object_id = p.object_id
        WHERE p.index_id IN (0, 1)
        ORDER BY p.rows DESC
"@
    
    $reader = $cmd.ExecuteReader()
    $count = 0
    while ($reader.Read()) { $count++ }
    $reader.Close()
    
    if ($count -eq 0) { throw "No metadata retrieved" }
    $conn.Close()
}

# Test 5: Connection Pooling
Test-Feature "Connection Pooling" {
    # Test rapid connection open/close
    for ($i = 1; $i -le 5; $i++) {
        $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = "SELECT 1"
        $result = $cmd.ExecuteScalar()
        $conn.Close()
    }
}

# Test 6: Query Performance
Test-Feature "Query Performance" {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $cmd = $conn.CreateCommand()
    $cmd.CommandTimeout = 30
    $cmd.CommandText = @"
        SELECT 
            COUNT(*) as TotalObjects,
            SUM(CASE WHEN type = 'U' THEN 1 ELSE 0 END) as UserTables,
            SUM(CASE WHEN type = 'V' THEN 1 ELSE 0 END) as Views,
            SUM(CASE WHEN type IN ('P', 'FN', 'IF', 'TF') THEN 1 ELSE 0 END) as Procedures
        FROM sys.objects WITH (NOLOCK)
"@
    
    $reader = $cmd.ExecuteReader()
    if ($reader.Read()) {
        $totalObjects = $reader["TotalObjects"]
        Write-Host "(Found $totalObjects objects in $($sw.ElapsedMilliseconds)ms) " -NoNewline
    }
    $reader.Close()
    $conn.Close()
    
    if ($sw.ElapsedMilliseconds -gt 5000) { 
        throw "Query took too long: $($sw.ElapsedMilliseconds)ms" 
    }
}

# Test 7: Security Validation
Test-Feature "Security Validation" {
    # Test that connection string has required security flags
    if (-not $connectionString.Contains("TrustServerCertificate=true")) {
        throw "Missing TrustServerCertificate flag"
    }
    if (-not $connectionString.Contains("Password=")) {
        throw "Missing password in connection string"
    }
}

# Summary
Write-Host "`n=== Test Summary ===" -ForegroundColor Cyan
Write-Host "Total Tests: $($testsPassed + $testsFailed)" -ForegroundColor White
Write-Host "Passed: $testsPassed" -ForegroundColor Green
Write-Host "Failed: $testsFailed" -ForegroundColor $(if ($testsFailed -gt 0) { "Red" } else { "Green" })
Write-Host ""

if ($testsFailed -eq 0) {
    Write-Host "All E2E tests passed successfully!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "Some tests failed. Please check the results above." -ForegroundColor Red
    exit 1
}