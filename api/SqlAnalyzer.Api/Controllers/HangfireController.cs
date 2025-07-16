using Hangfire;
using Hangfire.Common;
using Hangfire.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SqlAnalyzer.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class HangfireController : ControllerBase
    {
        private readonly ILogger<HangfireController> _logger;

        public HangfireController(ILogger<HangfireController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get Hangfire dashboard statistics
        /// </summary>
        [HttpGet("stats")]
        public IActionResult GetStats()
        {
            try
            {
                using var connection = JobStorage.Current.GetConnection();
                
                var stats = new
                {
                    Servers = connection.GetSetCount("servers"),
                    Enqueued = connection.GetSetCount("queues:default"),
                    Processing = connection.GetSetCount("processing"),
                    Succeeded = connection.GetSetCount("succeeded"),
                    Failed = connection.GetSetCount("failed"),
                    Scheduled = connection.GetSetCount("schedule"),
                    Recurring = connection.GetSetCount("recurring-jobs")
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Hangfire stats");
                return Ok(new
                {
                    Servers = 0,
                    Enqueued = 0,
                    Processing = 0,
                    Succeeded = 0,
                    Failed = 0,
                    Scheduled = 0,
                    Recurring = 0,
                    Error = "Stats unavailable"
                });
            }
        }

        /// <summary>
        /// Get recent jobs
        /// </summary>
        [HttpGet("jobs")]
        public IActionResult GetJobs([FromQuery] int count = 10)
        {
            try
            {
                using var connection = JobStorage.Current.GetConnection();
                var monitor = JobStorage.Current.GetMonitoringApi();

                var jobs = new
                {
                    Enqueued = monitor.EnqueuedJobs("default", 0, count),
                    Processing = monitor.ProcessingJobs(0, count),
                    Succeeded = monitor.SucceededJobs(0, count),
                    Failed = monitor.FailedJobs(0, count)
                };

                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Hangfire jobs");
                return Ok(new { Error = "Jobs unavailable" });
            }
        }

        /// <summary>
        /// Get dashboard access info
        /// </summary>
        [HttpGet("dashboard-info")]
        public IActionResult GetDashboardInfo()
        {
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            
            return Ok(new
            {
                DashboardUrl = "/hangfire",
                RequiresAuthentication = !isDevelopment,
                Note = isDevelopment 
                    ? "Dashboard is accessible without authentication in development" 
                    : "Dashboard requires authentication. For production, use the API endpoints or access from localhost.",
                AlternativeEndpoints = new
                {
                    Stats = "/api/v1/hangfire/stats",
                    Jobs = "/api/v1/hangfire/jobs"
                }
            });
        }
    }
}