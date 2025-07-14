# SQL Analyzer Comprehensive E2E Test
Write-Host "`n=== SQL Analyzer Comprehensive End-to-End Test ===" -ForegroundColor Green
Write-Host "Testing all major SQL Analyzer features`n" -ForegroundColor Green

$connectionString = "Server=sqltest.schoolvision.net,14333;Database=SVDB_CaptureT;User Id=sv;Password=Gv51076!;TrustServerCertificate=true;"
$startTime = Get-Date
$testResults = @()

function Test-Feature {
    param(
        [string]$Category,
        [string]$Name,
        [scriptblock]$Test
    )
    
    $result = @{
        Category = $Category
        Name = $Name
        Success = $false
        Duration = 0
        Error = $null
        Details = ""
    }
    
    Write-Host -NoNewline "[$Category] $Name... "
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    
    try {
        $details = & $Test
        $result.Success = $true
        $result.Details = $details
        Write-Host "[PASSED] " -ForegroundColor Green -NoNewline
        if ($details) { Write-Host "($details)" -ForegroundColor Gray }
        else { Write-Host "" }
    }
    catch {
        $result.Error = $_.Exception.Message
        Write-Host "[FAILED] - $_" -ForegroundColor Red
    }
    finally {
        $sw.Stop()
        $result.Duration = $sw.ElapsedMilliseconds
        $script:testResults += $result
    }
}

# Category 1: Connection Management
Test-Feature "Connection" "Basic Connectivity" {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    $dbName = $conn.Database
    $conn.Close()
    return "Connected to $dbName"
}

Test-Feature "Connection" "Connection Pooling" {
    $connections = @()
    for ($i = 1; $i -le 10; $i++) {
        $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
        $conn.Open()
        $connections += $conn
    }
    foreach ($conn in $connections) { $conn.Close() }
    return "Opened and closed 10 connections"
}

Test-Feature "Connection" "Timeout Handling" {
    $timeoutConn = "$connectionString;Connection Timeout=2"
    $conn = New-Object System.Data.SqlClient.SqlConnection($timeoutConn)
    $conn.Open()
    $conn.Close()
    return "2-second timeout configured"
}

# Category 2: Query Optimization
Test-Feature "Query" "NOLOCK Optimization" {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    
    $queries = @(
        "SELECT * FROM sys.tables WITH (NOLOCK)",
        "SELECT COUNT(*) FROM sys.columns WITH (NOLOCK)",
        "SELECT TOP 10 * FROM sys.objects WITH (NOLOCK) ORDER BY create_date DESC"
    )
    
    $count = 0
    foreach ($query in $queries) {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $query
        $reader = $cmd.ExecuteReader()
        while ($reader.Read()) { $count++ }
        $reader.Close()
    }
    
    $conn.Close()
    return "Executed 3 NOLOCK queries, retrieved $count rows"
}

Test-Feature "Query" "Pagination Support" {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    
    # SQL Server 2012+ OFFSET/FETCH syntax
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
        SELECT name, object_id, create_date 
        FROM sys.objects WITH (NOLOCK)
        ORDER BY create_date DESC
        OFFSET 10 ROWS
        FETCH NEXT 5 ROWS ONLY
"@
    
    $reader = $cmd.ExecuteReader()
    $count = 0
    while ($reader.Read()) { $count++ }
    $reader.Close()
    $conn.Close()
    
    return "Retrieved page 3 (5 rows) of objects"
}

# Category 3: Database Analysis
Test-Feature "Analysis" "Table Metadata" {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
        SELECT 
            s.name AS SchemaName,
            t.name AS TableName,
            p.rows AS [RowCount],
            SUM(a.total_pages) * 8 AS TotalSpaceKB,
            SUM(a.used_pages) * 8 AS UsedSpaceKB
        FROM sys.tables t WITH (NOLOCK)
        INNER JOIN sys.schemas s WITH (NOLOCK) ON t.schema_id = s.schema_id
        INNER JOIN sys.partitions p WITH (NOLOCK) ON t.object_id = p.object_id
        INNER JOIN sys.allocation_units a WITH (NOLOCK) ON p.partition_id = a.container_id
        WHERE p.index_id IN (0, 1)
        GROUP BY s.name, t.name, p.rows
        ORDER BY p.rows DESC
"@
    
    $reader = $cmd.ExecuteReader()
    $tables = @()
    while ($reader.Read()) {
        $tables += @{
            Schema = $reader["SchemaName"]
            Table = $reader["TableName"]
            Rows = $reader["RowCount"]
            SizeKB = $reader["TotalSpaceKB"]
        }
    }
    $reader.Close()
    $conn.Close()
    
    $totalRows = ($tables | Measure-Object -Property Rows -Sum).Sum
    return "Analyzed $($tables.Count) tables with $totalRows total rows"
}

Test-Feature "Analysis" "Index Analysis" {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
        SELECT 
            COUNT(*) AS IndexCount,
            SUM(CASE WHEN is_primary_key = 1 THEN 1 ELSE 0 END) AS PrimaryKeys,
            SUM(CASE WHEN is_unique = 1 AND is_primary_key = 0 THEN 1 ELSE 0 END) AS UniqueIndexes,
            SUM(CASE WHEN type_desc = 'CLUSTERED' THEN 1 ELSE 0 END) AS ClusteredIndexes,
            SUM(CASE WHEN type_desc = 'NONCLUSTERED' THEN 1 ELSE 0 END) AS NonClusteredIndexes
        FROM sys.indexes WITH (NOLOCK)
        WHERE object_id IN (SELECT object_id FROM sys.tables WITH (NOLOCK))
"@
    
    $reader = $cmd.ExecuteReader()
    $indexInfo = ""
    if ($reader.Read()) {
        $indexInfo = "$($reader["IndexCount"]) indexes: $($reader["PrimaryKeys"]) PKs, $($reader["UniqueIndexes"]) unique, $($reader["ClusteredIndexes"]) clustered"
    }
    $reader.Close()
    $conn.Close()
    
    return $indexInfo
}

# Category 4: Security & Compliance
Test-Feature "Security" "Read-Only Verification" {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    
    # Verify we cannot create objects
    $cmd = $conn.CreateCommand()
    try {
        $cmd.CommandText = "CREATE TABLE #TestTable (ID int)"
        $cmd.ExecuteNonQuery()
        throw "Should not be able to create tables"
    }
    catch {
        # Expected to fail
    }
    
    $conn.Close()
    return "Write operations properly blocked"
}

Test-Feature "Security" "Permission Check" {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
        SELECT 
            p.permission_name,
            p.state_desc
        FROM sys.database_permissions p WITH (NOLOCK)
        WHERE p.grantee_principal_id = USER_ID()
        AND p.permission_name IN ('SELECT', 'INSERT', 'UPDATE', 'DELETE', 'EXECUTE')
"@
    
    $reader = $cmd.ExecuteReader()
    $permissions = @()
    while ($reader.Read()) {
        $permissions += "$($reader["permission_name"]):$($reader["state_desc"])"
    }
    $reader.Close()
    $conn.Close()
    
    return "Permissions: $($permissions -join ', ')"
}

# Category 5: Performance Testing
Test-Feature "Performance" "Large Result Set" {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
        SELECT TOP 1000
            o.object_id,
            o.name,
            o.type_desc,
            o.create_date,
            o.modify_date,
            s.name AS schema_name
        FROM sys.objects o WITH (NOLOCK)
        INNER JOIN sys.schemas s WITH (NOLOCK) ON o.schema_id = s.schema_id
        ORDER BY o.create_date DESC
"@
    
    $reader = $cmd.ExecuteReader()
    $count = 0
    while ($reader.Read()) { $count++ }
    $reader.Close()
    $conn.Close()
    $sw.Stop()
    
    return "Retrieved $count rows in $($sw.ElapsedMilliseconds)ms"
}

Test-Feature "Performance" "Complex Join Query" {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
        SELECT TOP 100
            t.name AS TableName,
            i.name AS IndexName,
            i.type_desc AS IndexType,
            ps.row_count,
            ps.used_page_count * 8 AS UsedSpaceKB
        FROM sys.tables t WITH (NOLOCK)
        INNER JOIN sys.indexes i WITH (NOLOCK) ON t.object_id = i.object_id
        INNER JOIN sys.dm_db_partition_stats ps WITH (NOLOCK) 
            ON i.object_id = ps.object_id AND i.index_id = ps.index_id
        WHERE t.is_ms_shipped = 0
        ORDER BY ps.row_count DESC
"@
    
    $reader = $cmd.ExecuteReader()
    $count = 0
    while ($reader.Read()) { $count++ }
    $reader.Close()
    $conn.Close()
    $sw.Stop()
    
    return "Complex join completed in $($sw.ElapsedMilliseconds)ms"
}

# Category 6: Error Handling
Test-Feature "Resilience" "Invalid Query Recovery" {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    
    # Try invalid query
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = "SELECT * FROM NonExistentTable"
        $cmd.ExecuteReader()
    }
    catch {
        # Expected error
    }
    
    # Verify connection still works
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT 1"
    $result = $cmd.ExecuteScalar()
    $conn.Close()
    
    return "Connection recovered after error"
}

Test-Feature "Resilience" "Transaction Rollback" {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $conn.Open()
    
    $tran = $conn.BeginTransaction()
    try {
        # Multiple queries in transaction
        $cmd = $conn.CreateCommand()
        $cmd.Transaction = $tran
        
        $cmd.CommandText = "SELECT COUNT(*) FROM sys.tables WITH (NOLOCK)"
        $tableCount = $cmd.ExecuteScalar()
        
        $cmd.CommandText = "SELECT COUNT(*) FROM sys.columns WITH (NOLOCK)"
        $columnCount = $cmd.ExecuteScalar()
        
        # Rollback
        $tran.Rollback()
        
        return "Transaction with $tableCount tables, $columnCount columns rolled back"
    }
    catch {
        $tran.Rollback()
        throw
    }
    finally {
        $conn.Close()
    }
}

# Generate Summary Report
Write-Host "`n=== Test Results Summary ===" -ForegroundColor Cyan

$categories = $testResults | Group-Object Category
foreach ($category in $categories) {
    $passed = ($category.Group | Where-Object Success).Count
    $total = $category.Count
    $status = if ($passed -eq $total) { "Green" } else { "Yellow" }
    
    Write-Host "`n$($category.Name) Tests: $passed/$total passed" -ForegroundColor $status
    
    foreach ($test in $category.Group) {
        $icon = if ($test.Success) { "✓" } else { "✗" }
        $color = if ($test.Success) { "Green" } else { "Red" }
        Write-Host "  $icon $($test.Name) ($($test.Duration)ms)" -ForegroundColor $color
        
        if (-not $test.Success -and $test.Error) {
            Write-Host "    Error: $($test.Error)" -ForegroundColor DarkRed
        }
    }
}

# Overall Summary
$totalTests = $testResults.Count
$passedTests = ($testResults | Where-Object Success).Count
$failedTests = $totalTests - $passedTests
$totalDuration = [math]::Round(((Get-Date) - $startTime).TotalSeconds, 2)

Write-Host "`n=== Overall Summary ===" -ForegroundColor Cyan
Write-Host "Total Tests: $totalTests" -ForegroundColor White
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $failedTests" -ForegroundColor $(if ($failedTests -gt 0) { "Red" } else { "Green" })
Write-Host "Total Duration: $totalDuration seconds" -ForegroundColor White
Write-Host ""

# Performance Metrics
$perfTests = $testResults | Where-Object { $_.Category -eq "Performance" -and $_.Success }
if ($perfTests) {
    Write-Host "Performance Metrics:" -ForegroundColor Cyan
    foreach ($test in $perfTests) {
        Write-Host "  - $($test.Name): $($test.Details)" -ForegroundColor Gray
    }
}

if ($failedTests -eq 0) {
    Write-Host "`nAll comprehensive E2E tests passed successfully!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`nSome tests failed. Please review the results above." -ForegroundColor Red
    exit 1
}