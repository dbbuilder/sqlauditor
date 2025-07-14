using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

class Program
{
    static async Task Main()
    {
        try
        {
            var connectionString = Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION");
            Console.WriteLine("Testing SQL Analyzer TableAnalyzer...");
            Console.WriteLine("=====================================");
            
            // Load the assembly
            var assemblyPath = @"..\src\SqlAnalyzer.Core\bin\Release\net8.0\SqlAnalyzer.Core.dll";
            var assembly = Assembly.LoadFrom(assemblyPath);
            
            // Get the types we need
            var connectionFactoryType = assembly.GetType("SqlAnalyzer.Core.Connections.ConnectionFactory");
            var databaseTypeEnum = assembly.GetType("SqlAnalyzer.Core.DatabaseType");
            var tableAnalyzerType = assembly.GetType("SqlAnalyzer.Core.Analyzers.TableAnalyzer");
            
            // Create logger
            var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole());
            var connectionLogger = loggerFactory.CreateLogger(connectionFactoryType);
            var analyzerLogger = loggerFactory.CreateLogger(tableAnalyzerType);
            
            // Create connection factory
            var factory = Activator.CreateInstance(connectionFactoryType, connectionLogger);
            
            // Get SqlServer enum value
            var sqlServerValue = Enum.Parse(databaseTypeEnum, "SqlServer");
            
            // Create connection
            var createConnectionMethod = connectionFactoryType.GetMethod("CreateConnection");
            var connection = createConnectionMethod.Invoke(factory, new object[] { connectionString, sqlServerValue });
            
            // Open connection
            var openAsyncMethod = connection.GetType().GetMethod("OpenAsync");
            await (Task)openAsyncMethod.Invoke(connection, null);
            
            Console.WriteLine("Connected to database!");
            
            // Create analyzer
            var analyzer = Activator.CreateInstance(tableAnalyzerType, connection, analyzerLogger);
            
            // Run analysis
            var analyzeAsyncMethod = tableAnalyzerType.GetMethod("AnalyzeAsync");
            var resultTask = (Task)analyzeAsyncMethod.Invoke(analyzer, null);
            await resultTask;
            
            // Get result
            var result = ((dynamic)resultTask).Result;
            
            Console.WriteLine($"\nAnalysis completed!");
            Console.WriteLine($"Success: {result.Success}");
            Console.WriteLine($"Database: {result.DatabaseName}");
            Console.WriteLine($"Tables analyzed: {result.Summary.TotalObjectsAnalyzed}");
            Console.WriteLine($"Findings: {result.Summary.TotalFindings}");
            
            if (!result.Success)
            {
                Console.WriteLine($"Error: {result.ErrorMessage}");
            }
            else
            {
                Console.WriteLine("\nTop findings:");
                int count = 0;
                foreach (var finding in result.Findings)
                {
                    if (count++ >= 5) break;
                    Console.WriteLine($"- [{finding.Severity}] {finding.Message}");
                }
            }
            
            // Dispose connection
            ((IDisposable)connection).Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner: {ex.InnerException.Message}");
            }
        }
    }
}
