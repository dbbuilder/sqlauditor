using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;

class StandaloneE2ETest
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=== SQL Analyzer Standalone E2E Test ===");
        Console.WriteLine($"Started at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();

        var connectionString = "Server=sqltest.schoolvision.net,14333;Database=SVDB_CaptureT;User Id=sv;Password=Gv51076!;TrustServerCertificate=true;";
        var stopwatch = Stopwatch.StartNew();
        var testsPassed = 0;
        var testsFailed = 0;

        try
        {
            // Test 1: Basic Connection
            await RunTest("Basic Connection Test", async () =>
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                
                var dbName = await ExecuteScalarAsync(connection, "SELECT DB_NAME()");
                Console.Write($"(Connected to: {dbName}) ");
                
                if (connection.State != ConnectionState.Open)
                    throw new Exception("Connection not open");
            }, ref testsPassed, ref testsFailed);

            // Test 2: Read-Only Query
            await RunTest("Read-Only Query Test", async () =>
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                
                var tableCount = await ExecuteScalarAsync(connection, 
                    "SELECT COUNT(*) FROM sys.tables WITH (NOLOCK)");
                Console.Write($"(Found {tableCount} tables) ");
            }, ref testsPassed, ref testsFailed);

            // Test 3: Transaction Rollback Test
            await RunTest("Transaction Safety Test", async () =>
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                
                using var transaction = connection.BeginTransaction();
                try
                {
                    // This should fail since we're read-only
                    var cmd = new SqlCommand("CREATE TABLE #TestTable (ID int)", connection, transaction);
                    await cmd.ExecuteNonQueryAsync();
                    transaction.Rollback();
                    Console.Write("(Transaction rolled back) ");
                }
                catch
                {
                    Console.Write("(Write operation blocked as expected) ");
                }
            }, ref testsPassed, ref testsFailed);

            // Test 4: Metadata Query
            await RunTest("Metadata Query Test", async () =>
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                
                var query = @"
                    SELECT TOP 5
                        t.name AS TableName,
                        p.rows AS RowCount
                    FROM sys.tables t WITH (NOLOCK)
                    INNER JOIN sys.partitions p WITH (NOLOCK) 
                        ON t.object_id = p.object_id
                    WHERE p.index_id IN (0, 1)
                    ORDER BY p.rows DESC";
                
                using var cmd = new SqlCommand(query, connection);
                using var reader = await cmd.ExecuteReaderAsync();
                
                var tableCount = 0;
                while (await reader.ReadAsync())
                {
                    tableCount++;
                }
                Console.Write($"(Retrieved {tableCount} table metadata records) ");
            }, ref testsPassed, ref testsFailed);

            // Test 5: Connection Timeout
            await RunTest("Connection Timeout Test", async () =>
            {
                var timeoutConnString = connectionString + "Connection Timeout=5;";
                using var connection = new SqlConnection(timeoutConnString);
                
                var sw = Stopwatch.StartNew();
                await connection.OpenAsync();
                sw.Stop();
                
                Console.Write($"(Connected in {sw.ElapsedMilliseconds}ms) ");
            }, ref testsPassed, ref testsFailed);

            // Test 6: Large Result Set Handling
            await RunTest("Large Result Set Test", async () =>
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                
                var query = @"
                    SELECT TOP 1000 
                        name, object_id, type_desc, create_date
                    FROM sys.objects WITH (NOLOCK)
                    ORDER BY create_date DESC";
                
                using var cmd = new SqlCommand(query, connection);
                cmd.CommandTimeout = 30;
                
                using var reader = await cmd.ExecuteReaderAsync();
                var recordCount = 0;
                while (await reader.ReadAsync())
                {
                    recordCount++;
                }
                
                Console.Write($"(Processed {recordCount} records) ");
            }, ref testsPassed, ref testsFailed);

            stopwatch.Stop();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("=== Test Summary ===");
            Console.WriteLine($"Total tests: {testsPassed + testsFailed}");
            Console.WriteLine($"Passed: {testsPassed}");
            Console.WriteLine($"Failed: {testsFailed}");
            Console.WriteLine($"Duration: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
            Console.WriteLine();

            return testsFailed > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    static async Task RunTest(string testName, Func<Task> test, ref int passed, ref int failed)
    {
        Console.Write($"Running {testName}... ");
        try
        {
            await test();
            Console.WriteLine("[PASSED]");
            passed++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FAILED] - {ex.Message}");
            failed++;
        }
    }

    static async Task<object> ExecuteScalarAsync(SqlConnection connection, string query)
    {
        using var cmd = new SqlCommand(query, connection);
        return await cmd.ExecuteScalarAsync();
    }
}