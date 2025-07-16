# SQL Analyzer - Simplified Production Database Analysis
Write-Host "`n=== SQL ANALYZER - DATABASE ANALYSIS ===" -ForegroundColor Cyan
Write-Host "Analyzing SVDB_CaptureT database`n" -ForegroundColor Cyan

$connectionString = "Server=sqltest.schoolvision.net,14333;Database=SVDB_CaptureT;User Id=sv;Password=Gv51076!;TrustServerCertificate=true;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)

try {
    $conn.Open()
    Write-Host "Connected successfully to database`n" -ForegroundColor Green

    # 1. DATABASE OVERVIEW
    Write-Host "DATABASE OVERVIEW:" -ForegroundColor Yellow
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
SELECT 
    DB_NAME() AS DatabaseName,
    SERVERPROPERTY('ProductVersion') AS Version,
    SERVERPROPERTY('Edition') AS Edition,
    (SELECT COUNT(*) FROM sys.tables) AS TableCount,
    (SELECT COUNT(*) FROM sys.procedures) AS ProcedureCount,
    (SELECT COUNT(*) FROM sys.views) AS ViewCount
"@
    $reader = $cmd.ExecuteReader()
    if ($reader.Read()) {
        Write-Host "  Database: $($reader["DatabaseName"])"
        Write-Host "  Version: SQL Server $($reader["Version"])"
        Write-Host "  Edition: $($reader["Edition"])"
        Write-Host "  Tables: $($reader["TableCount"])"
        Write-Host "  Procedures: $($reader["ProcedureCount"])"
        Write-Host "  Views: $($reader["ViewCount"])"
    }
    $reader.Close()

    # 2. TABLE ANALYSIS
    Write-Host "`nTOP 10 TABLES BY ROW COUNT:" -ForegroundColor Yellow
    $cmd.CommandText = @"
SELECT TOP 10
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
    $tableData = @()
    while ($reader.Read()) {
        $tableData += [PSCustomObject]@{
            Schema = $reader["SchemaName"]
            Table = $reader["TableName"]
            RowCount = $reader["RowCount"]
        }
    }
    $reader.Close()
    
    $tableData | Format-Table -AutoSize

    # 3. DATABASE SIZE
    Write-Host "DATABASE SIZE INFORMATION:" -ForegroundColor Yellow
    $cmd.CommandText = @"
SELECT 
    type_desc AS FileType,
    COUNT(*) AS FileCount,
    CAST(SUM(CAST(size AS BIGINT)) * 8.0 / 1024 AS DECIMAL(10,2)) AS TotalSizeMB
FROM sys.database_files WITH (NOLOCK)
GROUP BY type_desc
"@
    
    $reader = $cmd.ExecuteReader()
    while ($reader.Read()) {
        Write-Host "  $($reader["FileType"]): $($reader["FileCount"]) file(s), $($reader["TotalSizeMB"]) MB"
    }
    $reader.Close()

    # 4. INDEX STATISTICS
    Write-Host "`nINDEX STATISTICS:" -ForegroundColor Yellow
    $cmd.CommandText = @"
SELECT 
    COUNT(*) AS TotalIndexes,
    SUM(CASE WHEN is_primary_key = 1 THEN 1 ELSE 0 END) AS PrimaryKeys,
    SUM(CASE WHEN is_unique = 1 AND is_primary_key = 0 THEN 1 ELSE 0 END) AS UniqueIndexes,
    SUM(CASE WHEN type = 1 THEN 1 ELSE 0 END) AS ClusteredIndexes,
    SUM(CASE WHEN type = 2 THEN 1 ELSE 0 END) AS NonClusteredIndexes,
    SUM(CASE WHEN is_disabled = 1 THEN 1 ELSE 0 END) AS DisabledIndexes
FROM sys.indexes WITH (NOLOCK)
WHERE object_id IN (SELECT object_id FROM sys.tables)
"@
    
    $reader = $cmd.ExecuteReader()
    if ($reader.Read()) {
        Write-Host "  Total Indexes: $($reader["TotalIndexes"])"
        Write-Host "  Primary Keys: $($reader["PrimaryKeys"])"
        Write-Host "  Unique Indexes: $($reader["UniqueIndexes"])"
        Write-Host "  Clustered: $($reader["ClusteredIndexes"])"
        Write-Host "  Non-Clustered: $($reader["NonClusteredIndexes"])"
        Write-Host "  Disabled: $($reader["DisabledIndexes"])"
    }
    $reader.Close()

    # 5. MISSING INDEXES (if any)
    Write-Host "`nTOP 5 MISSING INDEXES BY IMPACT:" -ForegroundColor Yellow
    $cmd.CommandText = @"
SELECT TOP 5
    ROUND(s.avg_total_user_cost * s.avg_user_impact * (s.user_seeks + s.user_scans), 0) AS ImpactScore,
    d.statement AS [Table],
    d.equality_columns AS EqualityColumns,
    d.inequality_columns AS InequalityColumns,
    d.included_columns AS IncludedColumns
FROM sys.dm_db_missing_index_details d WITH (NOLOCK)
INNER JOIN sys.dm_db_missing_index_groups g WITH (NOLOCK) ON d.index_handle = g.index_handle
INNER JOIN sys.dm_db_missing_index_group_stats s WITH (NOLOCK) ON g.index_group_handle = s.group_handle
WHERE database_id = DB_ID()
ORDER BY ImpactScore DESC
"@
    
    $reader = $cmd.ExecuteReader()
    $missingIndexes = @()
    while ($reader.Read()) {
        $missingIndexes += [PSCustomObject]@{
            Impact = $reader["ImpactScore"]
            Table = $reader["Table"]
            Columns = $reader["EqualityColumns"]
        }
    }
    $reader.Close()
    
    if ($missingIndexes.Count -gt 0) {
        $missingIndexes | Format-Table -AutoSize
        Write-Host "  ⚠ Found $($missingIndexes.Count) missing indexes that could improve performance" -ForegroundColor Yellow
    } else {
        Write-Host "  ✓ No significant missing indexes detected" -ForegroundColor Green
    }

    # 6. FRAGMENTATION CHECK
    Write-Host "`nINDEX FRAGMENTATION (>30%):" -ForegroundColor Yellow
    $cmd.CommandText = @"
SELECT TOP 10
    OBJECT_NAME(ps.object_id) AS TableName,
    i.name AS IndexName,
    ps.avg_fragmentation_in_percent AS FragmentationPercent,
    ps.page_count AS PageCount
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ps
INNER JOIN sys.indexes i WITH (NOLOCK) ON ps.object_id = i.object_id AND ps.index_id = i.index_id
WHERE ps.avg_fragmentation_in_percent > 30
    AND ps.page_count > 100
ORDER BY ps.avg_fragmentation_in_percent DESC
"@
    
    $reader = $cmd.ExecuteReader()
    $fragmented = @()
    while ($reader.Read()) {
        $fragmented += [PSCustomObject]@{
            Table = $reader["TableName"]
            Index = $reader["IndexName"]
            Fragmentation = [math]::Round($reader["FragmentationPercent"], 2)
            Pages = $reader["PageCount"]
        }
    }
    $reader.Close()
    
    if ($fragmented.Count -gt 0) {
        $fragmented | Format-Table -AutoSize
        Write-Host "  ⚠ Found $($fragmented.Count) highly fragmented indexes" -ForegroundColor Yellow
    } else {
        Write-Host "  ✓ No significant fragmentation detected" -ForegroundColor Green
    }

    # 7. STATISTICS AGE
    Write-Host "`nOUTDATED STATISTICS:" -ForegroundColor Yellow
    $cmd.CommandText = @"
SELECT TOP 10
    OBJECT_NAME(s.object_id) AS TableName,
    s.name AS StatName,
    sp.last_updated,
    sp.rows,
    sp.modification_counter,
    DATEDIFF(day, sp.last_updated, GETDATE()) AS DaysSinceUpdate
FROM sys.stats s WITH (NOLOCK)
CROSS APPLY sys.dm_db_stats_properties(s.object_id, s.stats_id) sp
WHERE sp.rows > 1000
    AND DATEDIFF(day, sp.last_updated, GETDATE()) > 30
ORDER BY DaysSinceUpdate DESC
"@
    
    $reader = $cmd.ExecuteReader()
    $outdatedStats = 0
    while ($reader.Read()) {
        if ($outdatedStats -eq 0) {
            Write-Host "  Table                    Stat Name              Last Updated    Days Old"
            Write-Host "  ----                     ---------              ------------    --------"
        }
        $outdatedStats++
        Write-Host ("  {0,-25} {1,-20} {2,-15} {3}" -f 
            $reader["TableName"], 
            $reader["StatName"], 
            $reader["last_updated"].ToString("yyyy-MM-dd"),
            $reader["DaysSinceUpdate"])
    }
    $reader.Close()
    
    if ($outdatedStats -gt 0) {
        Write-Host "  ⚠ Found $outdatedStats statistics older than 30 days" -ForegroundColor Yellow
    } else {
        Write-Host "  ✓ All statistics are up to date" -ForegroundColor Green
    }

    # 8. EMPTY TABLES
    Write-Host "`nEMPTY TABLES:" -ForegroundColor Yellow
    $cmd.CommandText = @"
SELECT 
    s.name AS SchemaName,
    t.name AS TableName
FROM sys.tables t WITH (NOLOCK)
INNER JOIN sys.schemas s WITH (NOLOCK) ON t.schema_id = s.schema_id
WHERE NOT EXISTS (
    SELECT 1 FROM sys.partitions p WITH (NOLOCK)
    WHERE p.object_id = t.object_id AND p.rows > 0
)
ORDER BY s.name, t.name
"@
    
    $reader = $cmd.ExecuteReader()
    $emptyTables = @()
    while ($reader.Read()) {
        $emptyTables += "$($reader["SchemaName"]).$($reader["TableName"])"
    }
    $reader.Close()
    
    if ($emptyTables.Count -gt 0) {
        Write-Host "  Found $($emptyTables.Count) empty tables:"
        $emptyTables | ForEach-Object { Write-Host "    - $_" }
    } else {
        Write-Host "  ✓ No empty tables found" -ForegroundColor Green
    }

    # 9. SUMMARY RECOMMENDATIONS
    Write-Host "`n=== ANALYSIS SUMMARY ===" -ForegroundColor Cyan
    Write-Host "`nKey Findings:" -ForegroundColor Yellow
    
    $findings = @()
    
    if ($missingIndexes.Count -gt 0) {
        $findings += "• $($missingIndexes.Count) missing indexes detected - consider creating these indexes"
    }
    
    if ($fragmented.Count -gt 0) {
        $findings += "• $($fragmented.Count) indexes with high fragmentation - consider rebuilding"
    }
    
    if ($outdatedStats -gt 0) {
        $findings += "• $outdatedStats outdated statistics - consider updating statistics"
    }
    
    if ($emptyTables.Count -gt 0) {
        $findings += "• $($emptyTables.Count) empty tables - consider removing if unused"
    }
    
    if ($findings.Count -eq 0) {
        Write-Host "  ✓ No significant issues found - database appears well-maintained" -ForegroundColor Green
    } else {
        foreach ($finding in $findings) {
            Write-Host "  $finding" -ForegroundColor Yellow
        }
    }

    # Save results
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $resultFile = "analysis-summary-$timestamp.txt"
    
    @"
SQL ANALYZER - DATABASE ANALYSIS REPORT
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Database: SVDB_CaptureT
Server: sqltest.schoolvision.net,14333

SUMMARY:
- Tables: $($tableData.Count) analyzed
- Total Rows: $(($tableData | Measure-Object -Property RowCount -Sum).Sum)
- Missing Indexes: $($missingIndexes.Count)
- Fragmented Indexes: $($fragmented.Count)
- Outdated Statistics: $outdatedStats
- Empty Tables: $($emptyTables.Count)

TOP TABLES:
$($tableData | Out-String)

RECOMMENDATIONS:
$(if ($findings.Count -eq 0) { "No significant issues found" } else { $findings -join "`n" })
"@ | Out-File -FilePath $resultFile -Encoding UTF8

    Write-Host "`nAnalysis report saved to: $resultFile" -ForegroundColor Green

}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
}
finally {
    if ($conn.State -eq "Open") {
        $conn.Close()
    }
}

Write-Host "`nAnalysis complete!" -ForegroundColor Green