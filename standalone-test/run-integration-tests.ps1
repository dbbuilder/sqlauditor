Write-Host "SQL Analyzer - Integration Test Runner" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green

# Set environment variables for SQL Server connection
Write-Host "`nSetting environment variables..." -ForegroundColor Yellow
$env:SQLSERVER_TEST_CONNECTION = "Server=sqltest.schoolvision.net,14333;Database=SVDB_CaptureT;User Id=sv;Password=Gv51076!;TrustServerCertificate=true;"
$env:RUN_INTEGRATION_TESTS = "true"

Write-Host "Connection string set for SQL Server test database" -ForegroundColor Green

# Get current directory
$currentDir = Get-Location
Write-Host "Current directory: $currentDir" -ForegroundColor Yellow

# First, let's try the standalone test project which worked before
Write-Host "`n`nRunning Standalone Integration Tests..." -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow

$standaloneResult = dotnet test StandaloneTest.csproj --logger "console;verbosity=detailed" 2>&1 | Out-String
Write-Host $standaloneResult

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nStandalone tests passed!" -ForegroundColor Green
} else {
    Write-Host "`nStandalone tests failed!" -ForegroundColor Red
    Write-Host "Exit code: $LASTEXITCODE" -ForegroundColor Red
}

# Create a simple direct test as well
Write-Host "`n`nRunning Direct Connection Test..." -ForegroundColor Yellow
Write-Host "==================================" -ForegroundColor Yellow

$testCode = @'
using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

class Program
{
    static async Task Main()
    {
        var connectionString = Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION");
        Console.WriteLine($"Testing connection to SQL Server...");
        Console.WriteLine($"Connection String: {connectionString?.Replace("Password=Gv51076!", "Password=****")}");
        
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            Console.WriteLine($"\nConnected successfully!");
            Console.WriteLine($"Database: {connection.Database}");
            Console.WriteLine($"Server: {connection.DataSource}");
            
            // Count user tables
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE type = 'U'";
            var tableCount = await command.ExecuteScalarAsync();
            Console.WriteLine($"User tables found: {tableCount}");
            
            // List first 5 tables
            command.CommandText = "SELECT TOP 5 name FROM sys.tables WHERE type = 'U' ORDER BY name";
            using var reader = await command.ExecuteReaderAsync();
            Console.WriteLine("\nFirst 5 tables:");
            while (await reader.ReadAsync())
            {
                Console.WriteLine($"  - {reader.GetString(0)}");
            }
            
            Console.WriteLine("\nConnection test PASSED!");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nConnection test FAILED!");
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
'@

# Save the test file
$testPath = "DirectConnectionTest.cs"
$testCode | Out-File -FilePath $testPath -Encoding UTF8

# Create a temporary project for the direct test
$projectContent = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.5" />
  </ItemGroup>
</Project>
'@

$projectPath = "DirectTest.csproj"
$projectContent | Out-File -FilePath $projectPath -Encoding UTF8

Write-Host "Building direct connection test..." -ForegroundColor Yellow
dotnet build $projectPath -v quiet

if ($LASTEXITCODE -eq 0) {
    Write-Host "Running direct connection test..." -ForegroundColor Yellow
    dotnet run --project $projectPath --no-build
}

# Clean up temporary files
Remove-Item $testPath -ErrorAction SilentlyContinue
Remove-Item $projectPath -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force "bin" -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force "obj" -ErrorAction SilentlyContinue

Write-Host "`n`nIntegration Test Summary" -ForegroundColor Green
Write-Host "========================" -ForegroundColor Green
Write-Host "Environment variables were set correctly" -ForegroundColor Green
Write-Host "Check the test results above for details" -ForegroundColor Yellow

Write-Host "`nScript complete!" -ForegroundColor Green