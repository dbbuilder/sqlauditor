using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

public class MinimalTest
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Testing SQL Server connection...");
        
        var connectionString = "Server=sqltest.schoolvision.net,14333;Database=SVDB_CaptureT;User Id=sv;Password=Gv51076!;TrustServerCertificate=true;";
        
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            Console.WriteLine($"Connected to: {connection.Database}");
            Console.WriteLine($"Server: {connection.DataSource}");
            Console.WriteLine($"State: {connection.State}");
            
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE type = 'U'";
            var result = await command.ExecuteScalarAsync();
            
            Console.WriteLine($"User tables count: {result}");
            
            // Test read-only query
            command.CommandText = @"
                SELECT TOP 5 
                    s.name AS SchemaName,
                    t.name AS TableName,
                    t.create_date
                FROM sys.tables t
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                WHERE t.type = 'U'
                ORDER BY t.create_date DESC";
            
            using var reader = await command.ExecuteReaderAsync();
            Console.WriteLine("\nRecent tables:");
            while (await reader.ReadAsync())
            {
                Console.WriteLine($"- {reader["SchemaName"]}.{reader["TableName"]} (Created: {reader["create_date"]})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}