Write-Host "SQL Analyzer - Direct Test" -ForegroundColor Green
Write-Host "==========================" -ForegroundColor Green

# Set environment variables
$env:SQLSERVER_TEST_CONNECTION = "Server=sqltest.schoolvision.net,14333;Database=SVDB_CaptureT;User Id=sv;Password=Gv51076!;TrustServerCertificate=true;"

# Compile a simple test program
$testCode = @'
using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SqlAnalyzer.Core.Connections;
using SqlAnalyzer.Core.Analyzers;

class Program
{
    static async Task Main()
    {
        try
        {
            Console.WriteLine("Setting up services...");
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddSingleton<IConnectionFactory, ConnectionFactory>();
            var serviceProvider = services.BuildServiceProvider();

            var connectionString = Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION");
            Console.WriteLine($"Connection string: {connectionString?.Replace("Password=Gv51076!", "Password=****")}");

            // Test basic connection
            Console.WriteLine("\n1. Testing basic SQL connection...");
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                await sqlConnection.OpenAsync();
                Console.WriteLine($"   Connected to: {sqlConnection.Database}");
                
                using (var command = sqlConnection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE type = 'U'";
                    var tableCount = await command.ExecuteScalarAsync();
                    Console.WriteLine($"   User tables: {tableCount}");
                }
            }

            // Test SqlAnalyzer connection
            Console.WriteLine("\n2. Testing SqlAnalyzer connection...");
            var factory = serviceProvider.GetRequiredService<IConnectionFactory>();
            var connection = factory.CreateConnection(connectionString, DatabaseType.SqlServer);
            await connection.OpenAsync();
            Console.WriteLine($"   Connected via SqlAnalyzer: {connection.DatabaseName}");

            // Test TableAnalyzer
            Console.WriteLine("\n3. Testing TableAnalyzer...");
            var logger = serviceProvider.GetRequiredService<ILogger<TableAnalyzer>>();
            var analyzer = new TableAnalyzer(connection, logger);
            
            var result = await analyzer.AnalyzeAsync();
            
            Console.WriteLine($"\n   Analysis Result:");
            Console.WriteLine($"   - Success: {result.Success}");
            Console.WriteLine($"   - Database: {result.DatabaseName}");
            Console.WriteLine($"   - Tables Analyzed: {result.Summary.TotalObjectsAnalyzed}");
            Console.WriteLine($"   - Findings: {result.Summary.TotalFindings}");
            
            if (!result.Success)
            {
                Console.WriteLine($"   - Error: {result.ErrorMessage}");
            }
            else
            {
                Console.WriteLine($"\n   Top 5 Findings:");
                int count = 0;
                foreach (var finding in result.Findings)
                {
                    if (count++ >= 5) break;
                    Console.WriteLine($"   - [{finding.Severity}] {finding.Message}");
                }
            }
            
            connection.Dispose();
            Console.WriteLine("\nAll tests completed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
'@

# Create project file
$projectContent = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.5" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\src\SqlAnalyzer.Core\SqlAnalyzer.Core.csproj" />
  </ItemGroup>
</Project>
'@

# Save files
$testPath = "DirectAnalyzerTest.cs"
$projectPath = "DirectAnalyzerTest.csproj"

$testCode | Out-File -FilePath $testPath -Encoding UTF8
$projectContent | Out-File -FilePath $projectPath -Encoding UTF8

Write-Host "`nBuilding test program..." -ForegroundColor Yellow
dotnet build $projectPath -v quiet

if ($LASTEXITCODE -eq 0) {
    Write-Host "Running test program..." -ForegroundColor Yellow
    Write-Host "======================" -ForegroundColor Yellow
    dotnet run --project $projectPath --no-build
} else {
    Write-Host "Build failed!" -ForegroundColor Red
}

# Clean up
Remove-Item $testPath -ErrorAction SilentlyContinue
Remove-Item $projectPath -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force "bin" -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force "obj" -ErrorAction SilentlyContinue

Write-Host "`nTest complete!" -ForegroundColor Green