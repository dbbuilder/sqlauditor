# Simple connection test script
Write-Host "Testing SQL Server Connection..." -ForegroundColor Green

$connectionString = "Server=sqltest.schoolvision.net,14333;Database=SVDB_CaptureT;User Id=sv;Password=Gv51076!;TrustServerCertificate=true;"

try {
    Add-Type -AssemblyName "System.Data"
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    
    Write-Host "Opening connection..." -ForegroundColor Yellow
    $connection.Open()
    
    Write-Host "Connection successful!" -ForegroundColor Green
    Write-Host "Server Version: $($connection.ServerVersion)" -ForegroundColor Cyan
    
    # Run a simple query
    $command = $connection.CreateCommand()
    $command.CommandText = "SELECT DB_NAME() AS DatabaseName, @@VERSION AS ServerInfo"
    $reader = $command.ExecuteReader()
    
    if ($reader.Read()) {
        Write-Host "Database: $($reader["DatabaseName"])" -ForegroundColor Cyan
        Write-Host "Server Info:" -ForegroundColor Cyan
        Write-Host $reader["ServerInfo"] -ForegroundColor Gray
    }
    
    $reader.Close()
    
    # Test table count
    $command.CommandText = "SELECT COUNT(*) AS TableCount FROM sys.tables WITH (NOLOCK)"
    $tableCount = $command.ExecuteScalar()
    Write-Host "Table Count: $tableCount" -ForegroundColor Cyan
    
    $connection.Close()
    Write-Host "`nAll tests passed!" -ForegroundColor Green
}
catch {
    Write-Host "Connection failed: $_" -ForegroundColor Red
}