using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SqlAnalyzer.Api.Hubs;
using SqlAnalyzer.Api.Models;
using SqlAnalyzer.Api.Services;
using SqlAnalyzer.Core.Analyzers;
using SqlAnalyzer.Core.Connections;

namespace SqlAnalyzer.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class AnalysisController : ControllerBase
    {
        private readonly IAnalysisService _analysisService;
        private readonly IConnectionFactory _connectionFactory;
        private readonly IHubContext<AnalysisHub> _hubContext;
        private readonly ILogger<AnalysisController> _logger;

        public AnalysisController(
            IAnalysisService analysisService,
            IConnectionFactory connectionFactory,
            IHubContext<AnalysisHub> hubContext,
            ILogger<AnalysisController> logger)
        {
            _analysisService = analysisService;
            _connectionFactory = connectionFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Start a new database analysis
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> StartAnalysis([FromBody] AnalysisRequest request)
        {
            try
            {
                // Validate request
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Start analysis (returns immediately with job ID)
                var jobId = await _analysisService.StartAnalysisAsync(request);

                return Ok(new
                {
                    jobId,
                    status = "Started",
                    message = "Analysis started successfully. Use the jobId to track progress."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start analysis");
                return StatusCode(500, new { error = "Failed to start analysis", details = ex.Message });
            }
        }

        /// <summary>
        /// Get analysis status
        /// </summary>
        [HttpGet("status/{jobId}")]
        public async Task<IActionResult> GetAnalysisStatus(string jobId)
        {
            var status = await _analysisService.GetAnalysisStatusAsync(jobId);
            if (status == null)
                return NotFound(new { error = "Analysis job not found" });

            return Ok(status);
        }

        /// <summary>
        /// Get analysis results
        /// </summary>
        [HttpGet("results/{jobId}")]
        public async Task<IActionResult> GetAnalysisResults(string jobId)
        {
            var results = await _analysisService.GetAnalysisResultsAsync(jobId);
            if (results == null)
                return NotFound(new { error = "Results not found" });

            return Ok(results);
        }

        /// <summary>
        /// Test database connection
        /// </summary>
        [HttpPost("test-connection")]
        public async Task<IActionResult> TestConnection([FromBody] ConnectionTestRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection(
                    request.ConnectionString, 
                    request.DatabaseType);

                await connection.OpenAsync();
                
                var dbName = await connection.ExecuteScalarAsync("SELECT DB_NAME()");

                return Ok(new
                {
                    success = true,
                    databaseName = dbName,
                    serverVersion = await connection.ExecuteScalarAsync("SELECT @@VERSION") as string ?? "Unknown"
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get available analysis types
        /// </summary>
        [HttpGet("types")]
        public IActionResult GetAnalysisTypes()
        {
            return Ok(new[]
            {
                new { id = "quick", name = "Quick Analysis", description = "Basic health check and overview" },
                new { id = "performance", name = "Performance Analysis", description = "Indexes, fragmentation, slow queries" },
                new { id = "security", name = "Security Audit", description = "Permissions, vulnerabilities, compliance" },
                new { id = "comprehensive", name = "Comprehensive Analysis", description = "Full database analysis" }
            });
        }

        /// <summary>
        /// Cancel running analysis
        /// </summary>
        [HttpPost("cancel/{jobId}")]
        public async Task<IActionResult> CancelAnalysis(string jobId)
        {
            var cancelled = await _analysisService.CancelAnalysisAsync(jobId);
            if (!cancelled)
                return NotFound(new { error = "Analysis job not found or already completed" });

            return Ok(new { message = "Analysis cancelled successfully" });
        }

        /// <summary>
        /// Get analysis history
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetAnalysisHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var history = await _analysisService.GetAnalysisHistoryAsync(page, pageSize);
            return Ok(history);
        }

        /// <summary>
        /// Export analysis results
        /// </summary>
        [HttpGet("export/{jobId}")]
        public async Task<IActionResult> ExportResults(string jobId, [FromQuery] string format = "pdf")
        {
            var exportData = await _analysisService.ExportResultsAsync(jobId, format);
            if (exportData == null)
                return NotFound(new { error = "Results not found" });

            var contentType = format.ToLower() switch
            {
                "pdf" => "application/pdf",
                "html" => "text/html",
                "json" => "application/json",
                _ => "application/octet-stream"
            };

            return File(exportData, contentType, $"analysis-{jobId}.{format}");
        }
    }
}