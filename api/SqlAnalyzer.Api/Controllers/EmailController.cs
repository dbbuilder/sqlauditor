using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlAnalyzer.Api.Services;
using SqlAnalyzer.Core.Models;

namespace SqlAnalyzer.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailController> _logger;

        public EmailController(IEmailService emailService, ILogger<EmailController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Send a test email to verify email configuration
        /// </summary>
        [HttpPost("test")]
        public async Task<IActionResult> SendTestEmail([FromBody] TestEmailRequest request)
        {
            try
            {
                // Create a sample analysis result for testing
                var testResult = new AnalysisResult
                {
                    AnalyzerName = "Test Analyzer",
                    DatabaseName = "TestDatabase",
                    ServerName = "TestServer",
                    DatabaseType = "SQL Server",
                    AnalysisStartTime = DateTime.UtcNow.AddMinutes(-5),
                    AnalysisEndTime = DateTime.UtcNow,
                    Success = true,
                    Summary = new AnalysisSummary
                    {
                        TotalObjectsAnalyzed = 10,
                        CriticalFindings = 1,
                        ErrorFindings = 2,
                        WarningFindings = 3,
                        InfoFindings = 4,
                        TotalRows = 1000
                    },
                    Findings = new List<Finding>
                    {
                        new Finding
                        {
                            Severity = Severity.Critical,
                            Category = "Performance",
                            Message = "Missing index on Orders table",
                            Description = "The Orders table is missing a critical index on the CustomerID column",
                            Recommendation = "CREATE INDEX IX_Orders_CustomerID ON Orders(CustomerID)",
                            AffectedObject = "Orders",
                            ObjectType = "Table",
                            Schema = "dbo"
                        },
                        new Finding
                        {
                            Severity = Severity.Warning,
                            Category = "Security",
                            Message = "Weak password policy",
                            Description = "The database allows passwords shorter than 8 characters",
                            Recommendation = "Update password policy to require minimum 8 characters",
                            AffectedObject = "Server",
                            ObjectType = "Configuration",
                            Schema = "N/A"
                        }
                    }
                };

                await _emailService.SendAnalysisReportAsync(
                    request.Email,
                    "test-" + Guid.NewGuid().ToString(),
                    testResult
                );

                return Ok(new
                {
                    success = true,
                    message = $"Test email sent to {request.Email}. Please check your inbox."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test email");
                return Ok(new
                {
                    success = false,
                    message = "Failed to send test email",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get email configuration status
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetEmailStatus()
        {
            var config = Configuration.GetSection("Email");
            var enabled = config.GetValue<bool>("Enabled");
            var hasApiKey = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SENDGRID_API_KEY")) ||
                           !string.IsNullOrEmpty(config["SendGridApiKey"]);

            return Ok(new
            {
                enabled,
                configured = hasApiKey,
                provider = config["Provider"] ?? "SendGrid",
                fromEmail = config["FromEmail"] ?? "noreply@sqlanalyzer.com"
            });
        }

        private IConfiguration Configuration => HttpContext.RequestServices.GetRequiredService<IConfiguration>();
    }

    public class TestEmailRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}