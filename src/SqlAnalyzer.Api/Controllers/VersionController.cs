using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SqlAnalyzer.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VersionController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public VersionController(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        /// <summary>
        /// Get version and deployment information
        /// </summary>
        [HttpGet]
        public IActionResult GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "1.0.0.0";
            var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? version;
            
            // Get build timestamp from environment variable (set during CI/CD)
            var buildTimestamp = Environment.GetEnvironmentVariable("BUILD_TIMESTAMP") ?? "Local Build";
            var commitSha = Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "Unknown";
            var runNumber = Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER") ?? "0";
            var deploymentId = Environment.GetEnvironmentVariable("DEPLOYMENT_ID") ?? Guid.NewGuid().ToString().Substring(0, 8);

            return Ok(new
            {
                application = "SQL Analyzer API",
                version = new
                {
                    assembly = version,
                    informational = informationalVersion,
                    api = "v1"
                },
                deployment = new
                {
                    timestamp = buildTimestamp,
                    deploymentId = deploymentId,
                    environment = _environment.EnvironmentName,
                    commit = commitSha.Length > 7 ? commitSha.Substring(0, 7) : commitSha,
                    buildNumber = runNumber
                },
                runtime = new
                {
                    framework = RuntimeInformation.FrameworkDescription,
                    os = RuntimeInformation.OSDescription,
                    architecture = RuntimeInformation.ProcessArchitecture.ToString()
                },
                health = new
                {
                    status = "Healthy",
                    uptime = GetUptime()
                }
            });
        }

        /// <summary>
        /// Simple health check endpoint
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new 
            { 
                status = "Healthy", 
                timestamp = DateTime.UtcNow,
                version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0"
            });
        }

        private string GetUptime()
        {
            var uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();
            return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
        }
    }
}