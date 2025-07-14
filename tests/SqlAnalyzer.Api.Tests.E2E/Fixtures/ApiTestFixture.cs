using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Testcontainers.MsSql;

namespace SqlAnalyzer.Api.Tests.E2E.Fixtures
{
    public class ApiTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private MsSqlContainer? _sqlContainer;
        private HubConnection? _hubConnection;
        public int ApiPort { get; private set; }
        public int DatabasePort { get; private set; }
        public string DatabaseConnectionString { get; private set; } = string.Empty;
        public string ApiBaseUrl => $"http://localhost:{ApiPort}";
        public string HubUrl => $"{ApiBaseUrl}/hubs/analysis";

        public async Task InitializeAsync()
        {
            // Get available ports
            ApiPort = TestConfiguration.GetNextAvailablePort(
                TestConfiguration.ApiPortRangeStart, 
                TestConfiguration.ApiPortRangeEnd);
            DatabasePort = TestConfiguration.GetNextAvailablePort(
                TestConfiguration.DatabasePortRangeStart, 
                TestConfiguration.DatabasePortRangeEnd);

            // Start SQL Server container
            _sqlContainer = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2019-latest")
                .WithPassword("Test@123456!")
                .WithPortBinding(DatabasePort, 1433)
                .WithEnvironment("ACCEPT_EULA", "Y")
                .WithEnvironment("MSSQL_PID", "Developer")
                .Build();

            await _sqlContainer.StartAsync();

            DatabaseConnectionString = $"Server=localhost,{DatabasePort};Database=TestDB;User Id=sa;Password=Test@123456!;TrustServerCertificate=true;";
            
            // Wait for SQL Server to be ready
            await WaitForSqlServerAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseUrls($"http://localhost:{ApiPort}");
            
            builder.ConfigureTestServices(services =>
            {
                // Override configuration for testing
                services.AddSingleton<IConfiguration>(provider =>
                {
                    var configuration = new ConfigurationBuilder()
                        .AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:TestDatabase"] = DatabaseConnectionString,
                            ["SqlAnalyzer:EnableCaching"] = "false",
                            ["SqlAnalyzer:MaxConcurrentAnalyses"] = "10",
                            ["Redis:Enabled"] = "false",
                            ["Logging:LogLevel:Default"] = "Warning"
                        })
                        .Build();
                    return configuration;
                });
            });

            builder.ConfigureServices(services =>
            {
                // Add test-specific services
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Warning);
                });
            });
        }

        public async Task<HubConnection> GetSignalRConnectionAsync()
        {
            if (_hubConnection == null)
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(HubUrl)
                    .Build();

                await _hubConnection.StartAsync();
            }

            return _hubConnection;
        }

        private async Task WaitForSqlServerAsync()
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(DatabaseConnectionString);
            var retries = 30;
            
            while (retries > 0)
            {
                try
                {
                    await connection.OpenAsync();
                    await connection.CloseAsync();
                    return;
                }
                catch
                {
                    retries--;
                    await Task.Delay(1000);
                }
            }

            throw new InvalidOperationException("SQL Server did not start in time");
        }

        public async Task DisposeAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }

            if (_sqlContainer != null)
            {
                await _sqlContainer.DisposeAsync();
            }

            TestConfiguration.ReleasePort(ApiPort);
            TestConfiguration.ReleasePort(DatabasePort);
        }
    }
}