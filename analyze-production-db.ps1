# SQL Analyzer - Production Database Analysis
param(
    [string]$OutputPath = ".\analysis-results"
)

Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║          SQL ANALYZER - PRODUCTION DATABASE ANALYSIS         ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

$connectionString = "Server=sqltest.schoolvision.net,14333;Database=SVDB_CaptureT;User Id=sv;Password=Gv51076!;TrustServerCertificate=true;"
$startTime = Get-Date
$findings = @()

# Create output directory
if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath | Out-Null
}

function Add-Finding {
    param(
        [string]$Category,
        [string]$Severity,  # Critical, High, Medium, Low, Info
        [string]$Title,
        [string]$Description,
        [object]$Data = $null
    )
    
    $script:findings += @{
        Category = $Category
        Severity = $Severity
        Title = $Title
        Description = $Description
        Data = $Data
        Timestamp = Get-Date
    }
}

function Execute-Query {
    param(
        [string]$Query,
        [string]$Description = "Executing query"
    )
    
    Write-Host "  • $Description... " -NoNewline
    try {
        $conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
        $conn.Open()
        
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $Query
        $cmd.CommandTimeout = 60
        
        $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
        $dataset = New-Object System.Data.DataSet
        $adapter.Fill($dataset) | Out-Null
        
        $conn.Close()
        
        Write-Host "✓" -ForegroundColor Green
        return $dataset.Tables[0]
    }
    catch {
        Write-Host "✗" -ForegroundColor Red
        Write-Host "    Error: $_" -ForegroundColor DarkRed
        return $null
    }
}

# 1. DATABASE OVERVIEW
Write-Host "`n▶ DATABASE OVERVIEW" -ForegroundColor Yellow

$dbInfo = Execute-Query @"
SELECT 
    DB_NAME() AS DatabaseName,
    SERVERPROPERTY('ProductVersion') AS ServerVersion,
    SERVERPROPERTY('Edition') AS Edition,
    SERVERPROPERTY('EngineEdition') AS EngineEdition,
    compatibility_level,
    recovery_model_desc,
    state_desc,
    is_auto_create_stats_on,
    is_auto_update_stats_on,
    is_fulltext_enabled,
    page_verify_option_desc
FROM sys.databases WITH (NOLOCK)
WHERE database_id = DB_ID()
"@ -Description "Getting database properties"

if ($dbInfo) {
    Write-Host "  Database: $($dbInfo.DatabaseName)" -ForegroundColor Cyan
    Write-Host "  Server: SQL Server $($dbInfo.ServerVersion) - $($dbInfo.Edition)" -ForegroundColor Cyan
    Write-Host "  Recovery Model: $($dbInfo.recovery_model_desc)" -ForegroundColor Cyan
}

# 2. DATABASE SIZE ANALYSIS
Write-Host "`n▶ DATABASE SIZE ANALYSIS" -ForegroundColor Yellow

$sizeInfo = Execute-Query @"
SELECT 
    type_desc,
    COUNT(*) as file_count,
    SUM(size) * 8 / 1024 AS total_size_mb,
    SUM(CASE WHEN max_size = -1 THEN 0 ELSE max_size * 8 / 1024 END) AS max_size_mb
FROM sys.database_files WITH (NOLOCK)
GROUP BY type_desc
"@ -Description "Analyzing database file sizes"

$spaceUsage = Execute-Query @"
SELECT 
    SUM(total_pages) * 8 / 1024 AS total_space_mb,
    SUM(used_pages) * 8 / 1024 AS used_space_mb,
    SUM(data_pages) * 8 / 1024 AS data_space_mb,
    (SUM(total_pages) - SUM(used_pages)) * 8 / 1024 AS unused_space_mb
FROM sys.allocation_units WITH (NOLOCK)
"@ -Description "Calculating space usage"

if ($spaceUsage.unused_space_mb -gt 1000) {
    Add-Finding -Category "Storage" -Severity "Medium" -Title "Significant Unused Space" `
        -Description "Database has $([math]::Round($spaceUsage.unused_space_mb, 2)) MB of unused space that could be reclaimed"
}

# 3. TABLE ANALYSIS
Write-Host "`n▶ TABLE ANALYSIS" -ForegroundColor Yellow

$tableAnalysis = Execute-Query @"
WITH TableSizes AS (
    SELECT 
        s.name AS SchemaName,
        t.name AS TableName,
        p.rows AS RowCount,
        SUM(a.total_pages) * 8 AS TotalSpaceKB,
        SUM(a.used_pages) * 8 AS UsedSpaceKB,
        SUM(a.data_pages) * 8 AS DataSpaceKB,
        t.create_date,
        t.modify_date
    FROM sys.tables t WITH (NOLOCK)
    INNER JOIN sys.schemas s WITH (NOLOCK) ON t.schema_id = s.schema_id
    INNER JOIN sys.indexes i WITH (NOLOCK) ON t.object_id = i.object_id
    INNER JOIN sys.partitions p WITH (NOLOCK) ON i.object_id = p.object_id AND i.index_id = p.index_id
    INNER JOIN sys.allocation_units a WITH (NOLOCK) ON p.partition_id = a.container_id
    WHERE i.index_id IN (0, 1)  -- Heap or Clustered
    GROUP BY s.name, t.name, p.rows, t.create_date, t.modify_date
)
SELECT TOP 20 * FROM TableSizes
ORDER BY TotalSpaceKB DESC
"@ -Description "Analyzing table sizes and row counts"

$emptyTables = Execute-Query @"
SELECT 
    s.name AS SchemaName,
    t.name AS TableName,
    t.create_date
FROM sys.tables t WITH (NOLOCK)
INNER JOIN sys.schemas s WITH (NOLOCK) ON t.schema_id = s.schema_id
WHERE t.object_id NOT IN (
    SELECT DISTINCT object_id 
    FROM sys.partitions WITH (NOLOCK)
    WHERE rows > 0
)
"@ -Description "Finding empty tables"

if ($emptyTables.Rows.Count -gt 0) {
    Add-Finding -Category "Tables" -Severity "Low" -Title "Empty Tables Found" `
        -Description "Found $($emptyTables.Rows.Count) empty tables that may be unused" `
        -Data $emptyTables
}

# 4. INDEX ANALYSIS
Write-Host "`n▶ INDEX ANALYSIS" -ForegroundColor Yellow

$missingIndexes = Execute-Query @"
SELECT TOP 10
    ROUND(s.avg_total_user_cost * s.avg_user_impact * (s.user_seeks + s.user_scans), 0) AS ImprovementMeasure,
    s.avg_total_user_cost,
    s.avg_user_impact,
    s.user_seeks + s.user_scans AS total_reads,
    d.statement AS TableName,
    d.equality_columns,
    d.inequality_columns,
    d.included_columns
FROM sys.dm_db_missing_index_details d WITH (NOLOCK)
INNER JOIN sys.dm_db_missing_index_groups g WITH (NOLOCK) ON d.index_handle = g.index_handle
INNER JOIN sys.dm_db_missing_index_group_stats s WITH (NOLOCK) ON g.index_group_handle = s.group_handle
WHERE database_id = DB_ID()
ORDER BY ImprovementMeasure DESC
"@ -Description "Checking for missing indexes"

if ($missingIndexes -and $missingIndexes.Rows.Count -gt 0) {
    Add-Finding -Category "Performance" -Severity "High" -Title "Missing Indexes Detected" `
        -Description "Found $($missingIndexes.Rows.Count) missing indexes that could improve query performance" `
        -Data $missingIndexes
}

$duplicateIndexes = Execute-Query @"
WITH IndexColumns AS (
    SELECT 
        t.object_id,
        i.index_id,
        i.name AS index_name,
        STRING_AGG(c.name, ',') WITHIN GROUP (ORDER BY ic.key_ordinal) AS key_columns
    FROM sys.indexes i WITH (NOLOCK)
    INNER JOIN sys.index_columns ic WITH (NOLOCK) ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    INNER JOIN sys.columns c WITH (NOLOCK) ON ic.object_id = c.object_id AND ic.column_id = c.column_id
    INNER JOIN sys.tables t WITH (NOLOCK) ON i.object_id = t.object_id
    WHERE ic.is_included_column = 0
    GROUP BY t.object_id, i.index_id, i.name
)
SELECT 
    t.name AS TableName,
    ic1.index_name AS Index1,
    ic2.index_name AS Index2,
    ic1.key_columns
FROM IndexColumns ic1
INNER JOIN IndexColumns ic2 ON ic1.object_id = ic2.object_id 
    AND ic1.index_id < ic2.index_id
    AND ic1.key_columns = ic2.key_columns
INNER JOIN sys.tables t WITH (NOLOCK) ON ic1.object_id = t.object_id
"@ -Description "Looking for duplicate indexes"

if ($duplicateIndexes -and $duplicateIndexes.Rows.Count -gt 0) {
    Add-Finding -Category "Performance" -Severity "Medium" -Title "Duplicate Indexes Found" `
        -Description "Found duplicate indexes that could be consolidated to save space" `
        -Data $duplicateIndexes
}

# 5. FRAGMENTATION ANALYSIS
Write-Host "`n▶ FRAGMENTATION ANALYSIS" -ForegroundColor Yellow

$fragmentation = Execute-Query @"
SELECT TOP 20
    OBJECT_NAME(ps.object_id) AS TableName,
    i.name AS IndexName,
    ps.index_type_desc,
    ps.avg_fragmentation_in_percent,
    ps.page_count,
    ps.record_count
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ps
INNER JOIN sys.indexes i WITH (NOLOCK) ON ps.object_id = i.object_id AND ps.index_id = i.index_id
WHERE ps.avg_fragmentation_in_percent > 10
    AND ps.page_count > 100
ORDER BY ps.avg_fragmentation_in_percent DESC
"@ -Description "Checking index fragmentation"

$highFragmentation = $fragmentation | Where-Object { $_.avg_fragmentation_in_percent -gt 30 }
if ($highFragmentation.Count -gt 0) {
    Add-Finding -Category "Performance" -Severity "High" -Title "High Index Fragmentation" `
        -Description "Found $($highFragmentation.Count) indexes with fragmentation > 30%" `
        -Data $highFragmentation
}

# 6. STATISTICS ANALYSIS
Write-Host "`n▶ STATISTICS ANALYSIS" -ForegroundColor Yellow

$outdatedStats = Execute-Query @"
SELECT TOP 20
    OBJECT_NAME(s.object_id) AS TableName,
    s.name AS StatName,
    sp.last_updated,
    sp.rows,
    sp.rows_sampled,
    sp.modification_counter,
    CAST(100.0 * sp.modification_counter / NULLIF(sp.rows, 0) AS DECIMAL(5,2)) AS PercentModified
FROM sys.stats s WITH (NOLOCK)
CROSS APPLY sys.dm_db_stats_properties(s.object_id, s.stats_id) sp
WHERE sp.rows > 1000
    AND sp.modification_counter > 0
    AND (sp.modification_counter > sp.rows * 0.1 OR DATEDIFF(day, sp.last_updated, GETDATE()) > 30)
ORDER BY PercentModified DESC
"@ -Description "Checking statistics freshness"

if ($outdatedStats -and $outdatedStats.Rows.Count -gt 0) {
    Add-Finding -Category "Performance" -Severity "Medium" -Title "Outdated Statistics" `
        -Description "Found $($outdatedStats.Rows.Count) statistics that need updating" `
        -Data $outdatedStats
}

# 7. QUERY PERFORMANCE ANALYSIS
Write-Host "`n▶ QUERY PERFORMANCE ANALYSIS" -ForegroundColor Yellow

$slowQueries = Execute-Query @"
SELECT TOP 10
    qs.total_elapsed_time / qs.execution_count / 1000 AS avg_duration_ms,
    qs.total_logical_reads / qs.execution_count AS avg_logical_reads,
    qs.execution_count,
    qs.total_elapsed_time / 1000 AS total_duration_ms,
    SUBSTRING(st.text, (qs.statement_start_offset/2) + 1,
        ((CASE qs.statement_end_offset
            WHEN -1 THEN DATALENGTH(st.text)
            ELSE qs.statement_end_offset
        END - qs.statement_start_offset)/2) + 1) AS query_text,
    qp.query_plan
FROM sys.dm_exec_query_stats qs WITH (NOLOCK)
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
CROSS APPLY sys.dm_exec_query_plan(qs.plan_handle) qp
WHERE qs.total_elapsed_time / qs.execution_count / 1000 > 1000  -- queries averaging > 1 second
ORDER BY qs.total_elapsed_time / qs.execution_count DESC
"@ -Description "Finding slow queries"

if ($slowQueries -and $slowQueries.Rows.Count -gt 0) {
    Add-Finding -Category "Performance" -Severity "High" -Title "Slow Queries Detected" `
        -Description "Found $($slowQueries.Rows.Count) queries with average duration > 1 second" `
        -Data ($slowQueries | Select-Object avg_duration_ms, execution_count, query_text)
}

# 8. SECURITY ANALYSIS
Write-Host "`n▶ SECURITY ANALYSIS" -ForegroundColor Yellow

$permissions = Execute-Query @"
SELECT 
    p.permission_name,
    p.state_desc,
    pr.name AS principal_name,
    pr.type_desc AS principal_type
FROM sys.database_permissions p WITH (NOLOCK)
INNER JOIN sys.database_principals pr WITH (NOLOCK) ON p.grantee_principal_id = pr.principal_id
WHERE p.major_id = 0
    AND pr.name NOT IN ('public', 'dbo', 'guest', 'sys', 'INFORMATION_SCHEMA')
ORDER BY pr.name, p.permission_name
"@ -Description "Reviewing database permissions"

$elevatedPermissions = $permissions | Where-Object { 
    $_.permission_name -in @('CONTROL', 'ALTER', 'CREATE', 'DROP', 'BACKUP DATABASE') 
}
if ($elevatedPermissions.Count -gt 0) {
    Add-Finding -Category "Security" -Severity "Medium" -Title "Elevated Permissions Found" `
        -Description "Found users with elevated database permissions that should be reviewed" `
        -Data $elevatedPermissions
}

# 9. OBJECT DEPENDENCIES
Write-Host "`n▶ OBJECT DEPENDENCIES" -ForegroundColor Yellow

$orphanedObjects = Execute-Query @"
WITH ObjectDependencies AS (
    SELECT 
        o.object_id,
        o.name AS object_name,
        o.type_desc,
        COUNT(d.referenced_major_id) AS dependency_count
    FROM sys.objects o WITH (NOLOCK)
    LEFT JOIN sys.sql_expression_dependencies d WITH (NOLOCK) ON o.object_id = d.referencing_id
    WHERE o.type IN ('P', 'FN', 'IF', 'TF', 'V')  -- Procedures, Functions, Views
        AND o.is_ms_shipped = 0
    GROUP BY o.object_id, o.name, o.type_desc
)
SELECT * FROM ObjectDependencies
WHERE dependency_count = 0
ORDER BY object_name
"@ -Description "Finding orphaned objects"

# 10. GENERATE REPORT
Write-Host "`n▶ GENERATING ANALYSIS REPORT" -ForegroundColor Yellow

$reportPath = Join-Path $OutputPath "database-analysis-$(Get-Date -Format 'yyyyMMdd-HHmmss').html"

$html = @"
<!DOCTYPE html>
<html>
<head>
    <title>SQL Analyzer - Database Analysis Report</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }
        .container { max-width: 1200px; margin: 0 auto; background-color: white; padding: 20px; box-shadow: 0 0 10px rgba(0,0,0,0.1); }
        h1 { color: #2c3e50; border-bottom: 3px solid #3498db; padding-bottom: 10px; }
        h2 { color: #34495e; margin-top: 30px; }
        .info-box { background-color: #ecf0f1; padding: 15px; border-radius: 5px; margin: 10px 0; }
        .finding { margin: 15px 0; padding: 15px; border-radius: 5px; }
        .critical { background-color: #e74c3c; color: white; }
        .high { background-color: #e67e22; color: white; }
        .medium { background-color: #f39c12; color: white; }
        .low { background-color: #95a5a6; color: white; }
        .info { background-color: #3498db; color: white; }
        table { border-collapse: collapse; width: 100%; margin: 10px 0; }
        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
        th { background-color: #34495e; color: white; }
        tr:nth-child(even) { background-color: #f2f2f2; }
        .summary { display: flex; justify-content: space-around; margin: 20px 0; }
        .metric { text-align: center; padding: 20px; background-color: #3498db; color: white; border-radius: 5px; }
        .metric h3 { margin: 0; font-size: 2em; }
        .metric p { margin: 5px 0 0 0; }
    </style>
</head>
<body>
    <div class="container">
        <h1>SQL Analyzer - Database Analysis Report</h1>
        <div class="info-box">
            <p><strong>Database:</strong> $($dbInfo.DatabaseName)</p>
            <p><strong>Server:</strong> SQL Server $($dbInfo.ServerVersion)</p>
            <p><strong>Analysis Date:</strong> $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')</p>
            <p><strong>Duration:</strong> $([math]::Round(((Get-Date) - $startTime).TotalSeconds, 2)) seconds</p>
        </div>

        <h2>Executive Summary</h2>
        <div class="summary">
            <div class="metric">
                <h3>$($tableAnalysis.Rows.Count)</h3>
                <p>Tables Analyzed</p>
            </div>
            <div class="metric">
                <h3>$($findings.Count)</h3>
                <p>Total Findings</p>
            </div>
            <div class="metric">
                <h3>$([math]::Round($spaceUsage.total_space_mb, 0))</h3>
                <p>Total Size (MB)</p>
            </div>
        </div>

        <h2>Key Findings</h2>
"@

foreach ($severity in @("Critical", "High", "Medium", "Low", "Info")) {
    $severityFindings = $findings | Where-Object { $_.Severity -eq $severity }
    if ($severityFindings.Count -gt 0) {
        foreach ($finding in $severityFindings) {
            $html += @"
        <div class="finding $($severity.ToLower())">
            <h3>[$severity] $($finding.Title)</h3>
            <p>$($finding.Description)</p>
"@
            if ($finding.Data -and $finding.Data.Rows.Count -gt 0) {
                $html += "<table><tr>"
                foreach ($col in $finding.Data.Columns) {
                    $html += "<th>$($col.ColumnName)</th>"
                }
                $html += "</tr>"
                
                foreach ($row in $finding.Data.Rows[0..4]) {  # Show first 5 rows
                    $html += "<tr>"
                    foreach ($col in $finding.Data.Columns) {
                        $html += "<td>$($row[$col.ColumnName])</td>"
                    }
                    $html += "</tr>"
                }
                
                if ($finding.Data.Rows.Count -gt 5) {
                    $html += "<tr><td colspan='$($finding.Data.Columns.Count)'>... and $($finding.Data.Rows.Count - 5) more rows</td></tr>"
                }
                $html += "</table>"
            }
            $html += "</div>"
        }
    }
}

$html += @"
        <h2>Top Tables by Size</h2>
        <table>
            <tr>
                <th>Schema</th>
                <th>Table Name</th>
                <th>Row Count</th>
                <th>Total Size (MB)</th>
                <th>Data Size (MB)</th>
            </tr>
"@

foreach ($table in $tableAnalysis.Rows[0..9]) {
    $html += @"
            <tr>
                <td>$($table.SchemaName)</td>
                <td>$($table.TableName)</td>
                <td>$($table.RowCount.ToString("N0"))</td>
                <td>$([math]::Round($table.TotalSpaceKB / 1024, 2))</td>
                <td>$([math]::Round($table.DataSpaceKB / 1024, 2))</td>
            </tr>
"@
}

$html += @"
        </table>

        <h2>Recommendations</h2>
        <ul>
"@

# Generate recommendations based on findings
if ($findings | Where-Object { $_.Category -eq "Performance" -and $_.Severity -in @("Critical", "High") }) {
    $html += "<li><strong>Performance:</strong> Address missing indexes and update outdated statistics to improve query performance</li>"
}
if ($findings | Where-Object { $_.Category -eq "Storage" }) {
    $html += "<li><strong>Storage:</strong> Consider reclaiming unused space and removing empty tables</li>"
}
if ($findings | Where-Object { $_.Category -eq "Security" }) {
    $html += "<li><strong>Security:</strong> Review and audit elevated permissions to ensure principle of least privilege</li>"
}

$html += @"
        </ul>
    </div>
</body>
</html>
"@

$html | Out-File -FilePath $reportPath -Encoding UTF8

Write-Host "  • Report saved to: $reportPath" -ForegroundColor Green

# Also save findings as JSON for programmatic access
$jsonPath = Join-Path $OutputPath "database-analysis-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
$findings | ConvertTo-Json -Depth 3 | Out-File -FilePath $jsonPath -Encoding UTF8
Write-Host "  • JSON data saved to: $jsonPath" -ForegroundColor Green

# Display summary
Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║                    ANALYSIS COMPLETE                         ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Green

Write-Host "`nSummary of Findings:" -ForegroundColor Cyan
$findingsSummary = $findings | Group-Object Severity
foreach ($group in $findingsSummary) {
    $color = switch ($group.Name) {
        "Critical" { "Red" }
        "High" { "DarkYellow" }
        "Medium" { "Yellow" }
        "Low" { "Gray" }
        "Info" { "Cyan" }
        default { "White" }
    }
    Write-Host "  • $($group.Name): $($group.Count) findings" -ForegroundColor $color
}

Write-Host "`nTotal Analysis Time: $([math]::Round(((Get-Date) - $startTime).TotalSeconds, 2)) seconds" -ForegroundColor White

# Open report in default browser
if ($Host.UI.PromptForChoice("Open Report", "Would you like to open the analysis report in your browser?", @("&Yes", "&No"), 0) -eq 0) {
    Start-Process $reportPath
}