using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlAnalyzer.Api.Services;
using SqlAnalyzer.Api.Models;

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
                    JobId = "test-" + Guid.NewGuid().ToString(),
                    AnalyzedAt = DateTime.UtcNow,
                    Database = new DatabaseInfo
                    {
                        Name = "TestDatabase",
                        ServerVersion = "Microsoft SQL Server 2019",
                        Edition = "Enterprise",
                        SizeMB = 1024,
                        TableCount = 25,
                        IndexCount = 45,
                        ProcedureCount = 15,
                        ViewCount = 10,
                        TotalRows = 100000
                    },
                    Findings = new List<Finding>
                    {
                        new Finding
                        {
                            Severity = "Critical",
                            Category = "Performance",
                            Title = "Missing index on Orders table",
                            Description = "The Orders table is missing a critical index on the CustomerID column",
                            Impact = "Query performance degradation of up to 80% on customer lookups"
                        },
                        new Finding
                        {
                            Severity = "Medium",
                            Category = "Security",
                            Title = "Weak password policy",
                            Description = "The database allows passwords shorter than 8 characters",
                            Impact = "Increased risk of unauthorized access"
                        }
                    },
                    Recommendations = new List<Recommendation>
                    {
                        new Recommendation
                        {
                            Category = "Performance",
                            Title = "Create Missing Indexes",
                            Description = "Add indexes to improve query performance",
                            Priority = "High",
                            EstimatedImpact = "20-50% query performance improvement",
                            Actions = new List<string> { "CREATE INDEX IX_Orders_CustomerID ON Orders(CustomerID)" }
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