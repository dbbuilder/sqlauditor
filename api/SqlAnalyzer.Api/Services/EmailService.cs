using SendGrid;
using SendGrid.Helpers.Mail;
using SqlAnalyzer.Api.Models;
using System.Text;

namespace SqlAnalyzer.Api.Services;

public interface IEmailService
{
    Task SendAnalysisReportAsync(string toEmail, string jobId, AnalysisResult result);
    Task SendAnalysisFailureNotificationAsync(string toEmail, string jobId, string errorMessage);
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;
    private readonly ISendGridClient? _sendGridClient;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _emailSettings = configuration.GetSection("Email").Get<EmailSettings>() ?? new EmailSettings();
        _logger = logger;

        // Check environment variable first, then fall back to config
        var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY") ?? _emailSettings.SendGridApiKey;
        
        // Override settings from environment if available
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("EMAIL_ENABLED")))
        {
            _emailSettings.Enabled = bool.Parse(Environment.GetEnvironmentVariable("EMAIL_ENABLED")!);
        }
        
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("EMAIL_FROM")))
        {
            _emailSettings.FromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM")!;
        }

        if (_emailSettings.Enabled && !string.IsNullOrEmpty(apiKey))
        {
            _sendGridClient = new SendGridClient(apiKey);
            _logger.LogInformation("Email service initialized with SendGrid");
        }
        else
        {
            _logger.LogInformation("Email service is disabled or SendGrid API key not configured");
        }
    }

    public async Task SendAnalysisReportAsync(string toEmail, string jobId, AnalysisResult result)
    {
        if (!_emailSettings.Enabled || _sendGridClient == null)
        {
            _logger.LogInformation("Email service is disabled or not configured");
            return;
        }

        try
        {
            var msg = new SendGridMessage
            {
                From = new EmailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                Subject = $"SQL Analysis Report - {result.DatabaseName} - {result.CompletedAt:yyyy-MM-dd}",
                PlainTextContent = GeneratePlainTextReport(result),
                HtmlContent = GenerateHtmlReport(result)
            };

            msg.AddTo(new EmailAddress(toEmail));
            
            // Add report as attachment
            var reportJson = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            var reportBytes = Encoding.UTF8.GetBytes(reportJson);
            msg.AddAttachment($"analysis-report-{jobId}.json", Convert.ToBase64String(reportBytes));

            var response = await _sendGridClient.SendEmailAsync(msg);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                _logger.LogInformation("Analysis report email sent successfully to {Email} for job {JobId}", toEmail, jobId);
            }
            else
            {
                _logger.LogWarning("Failed to send analysis report email. Status: {Status}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending analysis report email");
        }
    }

    public async Task SendAnalysisFailureNotificationAsync(string toEmail, string jobId, string errorMessage)
    {
        if (!_emailSettings.Enabled || _sendGridClient == null)
        {
            return;
        }

        try
        {
            var msg = new SendGridMessage
            {
                From = new EmailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                Subject = "SQL Analysis Failed",
                PlainTextContent = $"Analysis job {jobId} failed with error: {errorMessage}",
                HtmlContent = $"<p>Analysis job <strong>{jobId}</strong> failed with error:</p><p>{errorMessage}</p>"
            };

            msg.AddTo(new EmailAddress(toEmail));
            
            await _sendGridClient.SendEmailAsync(msg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending failure notification email");
        }
    }

    private string GeneratePlainTextReport(AnalysisResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"SQL Analysis Report");
        sb.AppendLine($"Database: {result.DatabaseName}");
        sb.AppendLine($"Server: {result.ServerName}");
        sb.AppendLine($"Completed: {result.CompletedAt}");
        sb.AppendLine($"Duration: {result.Duration}ms");
        sb.AppendLine();
        sb.AppendLine($"Summary:");
        sb.AppendLine($"- Total Findings: {result.TotalFindings}");
        sb.AppendLine($"- Critical: {result.CriticalFindings}");
        sb.AppendLine($"- High: {result.HighFindings}");
        sb.AppendLine($"- Medium: {result.MediumFindings}");
        sb.AppendLine($"- Low: {result.LowFindings}");
        sb.AppendLine();
        
        if (result.Findings.Any())
        {
            sb.AppendLine("Top Findings:");
            foreach (var finding in result.Findings.Take(5))
            {
                sb.AppendLine($"- [{finding.Severity}] {finding.Title}: {finding.Description}");
            }
        }

        return sb.ToString();
    }

    private string GenerateHtmlReport(AnalysisResult result)
    {
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .header {{ background-color: #2c3e50; color: white; padding: 20px; }}
        .content {{ padding: 20px; }}
        .summary {{ background-color: #f4f4f4; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .finding {{ border-left: 4px solid #3498db; padding: 10px; margin: 10px 0; }}
        .critical {{ border-color: #e74c3c; }}
        .high {{ border-color: #e67e22; }}
        .medium {{ border-color: #f39c12; }}
        .low {{ border-color: #95a5a6; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>SQL Analysis Report</h1>
        <p>Database: {result.DatabaseName} | Server: {result.ServerName}</p>
        <p>Completed: {result.CompletedAt:yyyy-MM-dd HH:mm:ss} | Duration: {result.Duration}ms</p>
    </div>
    <div class='content'>
        <div class='summary'>
            <h2>Summary</h2>
            <p>Total Findings: {result.TotalFindings}</p>
            <ul>
                <li>Critical: {result.CriticalFindings}</li>
                <li>High: {result.HighFindings}</li>
                <li>Medium: {result.MediumFindings}</li>
                <li>Low: {result.LowFindings}</li>
            </ul>
        </div>
        <h2>Top Findings</h2>";

        foreach (var finding in result.Findings.Take(10))
        {
            html += $@"
        <div class='finding {finding.Severity.ToLower()}'>
            <h3>{finding.Title}</h3>
            <p><strong>Severity:</strong> {finding.Severity} | <strong>Category:</strong> {finding.Category}</p>
            <p>{finding.Description}</p>
            {(string.IsNullOrEmpty(finding.Recommendation) ? "" : $"<p><strong>Recommendation:</strong> {finding.Recommendation}</p>")}
        </div>";
        }

        html += @"
    </div>
</body>
</html>";

        return html;
    }
}