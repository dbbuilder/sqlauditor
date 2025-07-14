using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using SqlAnalyzer.Api.Models;
using SqlAnalyzer.Api.Tests.E2E.Fixtures;
using SqlAnalyzer.Core.Connections;
using Xunit;

namespace SqlAnalyzer.Api.Tests.E2E.Tests
{
    public class AnalysisApiE2ETests : IClassFixture<ApiTestFixture>
    {
        private readonly ApiTestFixture _fixture;
        private readonly HttpClient _client;

        public AnalysisApiE2ETests(ApiTestFixture fixture)
        {
            _fixture = fixture;
            _client = _fixture.CreateClient();
        }

        [Fact]
        public async Task HealthCheck_ShouldReturnHealthy()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Healthy");
        }

        [Fact]
        public async Task TestConnection_WithValidConnection_ShouldSucceed()
        {
            // Arrange
            var request = new ConnectionTestRequest
            {
                ConnectionString = _fixture.DatabaseConnectionString,
                DatabaseType = DatabaseType.SqlServer
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/analysis/test-connection", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<dynamic>();
            ((bool)result.success).Should().BeTrue();
        }

        [Fact]
        public async Task TestConnection_WithInvalidConnection_ShouldReturnError()
        {
            // Arrange
            var request = new ConnectionTestRequest
            {
                ConnectionString = "Server=invalid;Database=test;User Id=sa;Password=wrong;",
                DatabaseType = DatabaseType.SqlServer
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/analysis/test-connection", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<dynamic>();
            ((bool)result.success).Should().BeFalse();
        }

        [Fact]
        public async Task GetAnalysisTypes_ShouldReturnTypes()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/analysis/types");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var types = await response.Content.ReadFromJsonAsync<dynamic[]>();
            types.Should().NotBeEmpty();
            types.Should().Contain(t => (string)t.id == "comprehensive");
        }

        [Fact]
        public async Task StartAnalysis_WithValidRequest_ShouldReturnJobId()
        {
            // Arrange
            await SetupTestDatabaseAsync();
            
            var request = new AnalysisRequest
            {
                ConnectionString = _fixture.DatabaseConnectionString,
                DatabaseType = DatabaseType.SqlServer,
                AnalysisType = "quick",
                Options = new AnalysisOptions
                {
                    IncludeIndexAnalysis = true,
                    IncludeFragmentation = false,
                    TimeoutMinutes = 5
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/analysis/start", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<dynamic>();
            string jobId = result.jobId;
            jobId.Should().NotBeNullOrEmpty();
            ((string)result.status).Should().Be("Started");
        }

        [Fact]
        public async Task FullAnalysisWorkflow_ShouldCompleteSuccessfully()
        {
            // Arrange
            await SetupTestDatabaseAsync();
            var hub = await _fixture.GetSignalRConnectionAsync();
            
            var progressUpdates = new List<AnalysisStatus>();
            hub.On<AnalysisStatus>("AnalysisProgress", status =>
            {
                progressUpdates.Add(status);
            });

            var request = new AnalysisRequest
            {
                ConnectionString = _fixture.DatabaseConnectionString,
                DatabaseType = DatabaseType.SqlServer,
                AnalysisType = "quick"
            };

            // Act - Start analysis
            var startResponse = await _client.PostAsJsonAsync("/api/v1/analysis/start", request);
            startResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var startResult = await startResponse.Content.ReadFromJsonAsync<dynamic>();
            string jobId = startResult.jobId;

            // Wait for completion
            var completed = await WaitForAnalysisCompletionAsync(jobId, TimeSpan.FromSeconds(30));
            completed.Should().BeTrue("Analysis should complete within 30 seconds");

            // Get results
            var resultsResponse = await _client.GetAsync($"/api/v1/analysis/results/{jobId}");
            resultsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var results = await resultsResponse.Content.ReadFromJsonAsync<AnalysisResult>();
            
            // Assert results
            results.Should().NotBeNull();
            results.JobId.Should().Be(jobId);
            results.Database.Should().NotBeNull();
            results.Database.Name.Should().Be("TestDB");
            results.Database.TableCount.Should().BeGreaterThan(0);
            
            // Check progress updates
            progressUpdates.Should().NotBeEmpty();
            progressUpdates.Should().Contain(p => p.Status == "Running");
            progressUpdates.Should().Contain(p => p.Status == "Completed");
            progressUpdates.Last().ProgressPercentage.Should().Be(100);
        }

        [Fact]
        public async Task CancelAnalysis_ShouldStopRunningAnalysis()
        {
            // Arrange
            await SetupTestDatabaseAsync();
            
            var request = new AnalysisRequest
            {
                ConnectionString = _fixture.DatabaseConnectionString,
                DatabaseType = DatabaseType.SqlServer,
                AnalysisType = "comprehensive", // Long running
                Options = new AnalysisOptions { TimeoutMinutes = 30 }
            };

            // Start analysis
            var startResponse = await _client.PostAsJsonAsync("/api/v1/analysis/start", request);
            var startResult = await startResponse.Content.ReadFromJsonAsync<dynamic>();
            string jobId = startResult.jobId;

            // Wait for it to start running
            await Task.Delay(1000);

            // Act - Cancel
            var cancelResponse = await _client.PostAsync($"/api/v1/analysis/cancel/{jobId}", null);

            // Assert
            cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            
            // Check status
            var statusResponse = await _client.GetAsync($"/api/v1/analysis/status/{jobId}");
            var status = await statusResponse.Content.ReadFromJsonAsync<AnalysisStatus>();
            status.Status.Should().BeOneOf("Cancelled", "Cancelling");
        }

        [Fact]
        public async Task GetAnalysisHistory_ShouldReturnPaginatedResults()
        {
            // Arrange - Run a few analyses
            await SetupTestDatabaseAsync();
            
            for (int i = 0; i < 3; i++)
            {
                var request = new AnalysisRequest
                {
                    ConnectionString = _fixture.DatabaseConnectionString,
                    DatabaseType = DatabaseType.SqlServer,
                    AnalysisType = "quick"
                };
                
                await _client.PostAsJsonAsync("/api/v1/analysis/start", request);
                await Task.Delay(500);
            }

            // Act
            var response = await _client.GetAsync("/api/v1/analysis/history?page=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var history = await response.Content.ReadFromJsonAsync<AnalysisHistoryItem[]>();
            history.Should().NotBeNull();
            history.Length.Should().BeGreaterOrEqualTo(3);
        }

        [Fact]
        public async Task ExportResults_ShouldReturnFile()
        {
            // Arrange
            await SetupTestDatabaseAsync();
            
            // Run analysis
            var request = new AnalysisRequest
            {
                ConnectionString = _fixture.DatabaseConnectionString,
                DatabaseType = DatabaseType.SqlServer,
                AnalysisType = "quick"
            };
            
            var startResponse = await _client.PostAsJsonAsync("/api/v1/analysis/start", request);
            var startResult = await startResponse.Content.ReadFromJsonAsync<dynamic>();
            string jobId = startResult.jobId;
            
            // Wait for completion
            await WaitForAnalysisCompletionAsync(jobId, TimeSpan.FromSeconds(30));

            // Act - Export as JSON
            var exportResponse = await _client.GetAsync($"/api/v1/analysis/export/{jobId}?format=json");

            // Assert
            exportResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            exportResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
            
            var content = await exportResponse.Content.ReadAsByteArrayAsync();
            content.Should().NotBeEmpty();
        }

        private async Task SetupTestDatabaseAsync()
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(_fixture.DatabaseConnectionString);
            await connection.OpenAsync();
            
            // Create test tables
            using var command = connection.CreateCommand();
            command.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TestTable1')
                BEGIN
                    CREATE TABLE TestTable1 (
                        Id INT PRIMARY KEY IDENTITY,
                        Name NVARCHAR(100),
                        CreatedDate DATETIME DEFAULT GETDATE()
                    );
                    
                    CREATE INDEX IX_TestTable1_Name ON TestTable1(Name);
                    
                    -- Insert test data
                    INSERT INTO TestTable1 (Name) 
                    SELECT 'Test ' + CAST(number AS VARCHAR(10))
                    FROM master..spt_values 
                    WHERE type = 'P' AND number BETWEEN 1 AND 1000;
                END
                
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TestTable2')
                BEGIN
                    CREATE TABLE TestTable2 (
                        Id INT PRIMARY KEY,
                        Description NVARCHAR(500)
                    );
                END";
            
            await command.ExecuteNonQueryAsync();
        }

        private async Task<bool> WaitForAnalysisCompletionAsync(string jobId, TimeSpan timeout)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            while (stopwatch.Elapsed < timeout)
            {
                var response = await _client.GetAsync($"/api/v1/analysis/status/{jobId}");
                if (response.IsSuccessStatusCode)
                {
                    var status = await response.Content.ReadFromJsonAsync<AnalysisStatus>();
                    if (status?.Status == "Completed" || status?.Status == "Failed")
                    {
                        return true;
                    }
                }
                
                await Task.Delay(500);
            }
            
            return false;
        }
    }
}