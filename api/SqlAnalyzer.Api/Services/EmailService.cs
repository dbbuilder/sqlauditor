using SendGrid;
using SendGrid.Helpers.Mail;
using SqlAnalyzer.Api.Models;
using SqlAnalyzer.Core.Models;
using System.Text;

namespace SqlAnalyzer.Api.Services;

public interface IEmailService
{
    Task SendAnalysisReportAsync(string toEmail, string jobId, Core.Models.AnalysisResult result);
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

    public async Task SendAnalysisReportAsync(string toEmail, string jobId, Core.Models.AnalysisResult result)
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
                Subject = $"SQL Analysis Report - {result.DatabaseName} - {result.AnalysisEndTime:yyyy-MM-dd}",
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

    private string GeneratePlainTextReport(Core.Models.AnalysisResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=============================================================");
        sb.AppendLine("                    SQL ANALYSIS REPORT");
        sb.AppendLine("=============================================================");
        sb.AppendLine();
        sb.AppendLine($"Database:      {result.DatabaseName}");
        sb.AppendLine($"Server:        {result.ServerName}");
        sb.AppendLine($"Type:          {result.DatabaseType}");
        sb.AppendLine($"Analyzer:      {result.AnalyzerName}");
        sb.AppendLine($"Completed:     {result.AnalysisEndTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Duration:      {result.Duration.TotalMinutes:F2} minutes");
        sb.AppendLine($"Objects:       {result.Summary.TotalObjectsAnalyzed:N0} analyzed");
        sb.AppendLine($"Total Rows:    {result.Summary.TotalRows:N0}");
        sb.AppendLine();
        sb.AppendLine("=============================================================");
        sb.AppendLine("                      SUMMARY");
        sb.AppendLine("=============================================================");
        sb.AppendLine($"Total Findings: {result.Summary.TotalFindings}");
        sb.AppendLine($"  - Critical:   {result.Summary.CriticalFindings}");
        sb.AppendLine($"  - Error:      {result.Summary.ErrorFindings}");
        sb.AppendLine($"  - Warning:    {result.Summary.WarningFindings}");
        sb.AppendLine($"  - Info:       {result.Summary.InfoFindings}");
        sb.AppendLine();

        // Group findings by severity
        var findingsBySeverity = result.Findings.GroupBy(f => f.Severity).OrderBy(g => g.Key);
        
        foreach (var severityGroup in findingsBySeverity)
        {
            sb.AppendLine("=============================================================");
            sb.AppendLine($"                 {severityGroup.Key.ToString().ToUpper()} FINDINGS ({severityGroup.Count()})");
            sb.AppendLine("=============================================================");
            
            foreach (var finding in severityGroup)
            {
                sb.AppendLine();
                sb.AppendLine($"[{finding.Severity}] {finding.Message}");
                sb.AppendLine($"Category:     {finding.Category}");
                sb.AppendLine($"Object Type:  {finding.ObjectType}");
                if (!string.IsNullOrEmpty(finding.Schema))
                    sb.AppendLine($"Schema:       {finding.Schema}");
                if (!string.IsNullOrEmpty(finding.AffectedObject))
                    sb.AppendLine($"Object:       {finding.AffectedObject}");
                sb.AppendLine($"Description:  {finding.Description}");
                if (!string.IsNullOrEmpty(finding.Recommendation))
                    sb.AppendLine($"Recommendation: {finding.Recommendation}");
                sb.AppendLine(new string('-', 60));
            }
            sb.AppendLine();
        }

        // Add performance metrics if available
        if (result.Metrics != null && result.Metrics.Any())
        {
            sb.AppendLine("=============================================================");
            sb.AppendLine("                  PERFORMANCE METRICS");
            sb.AppendLine("=============================================================");
            
            foreach (var metric in result.Metrics)
            {
                sb.AppendLine();
                sb.AppendLine($"{metric.Name}: {metric.Value}");
                if (metric.Details != null && metric.Details.Any())
                {
                    foreach (var detail in metric.Details)
                    {
                        sb.AppendLine($"  - {detail.Key}: {detail.Value}");
                    }
                }
            }
            sb.AppendLine();
        }

        // Add recommendations
        if (result.Recommendations != null && result.Recommendations.Any())
        {
            sb.AppendLine("=============================================================");
            sb.AppendLine("                   RECOMMENDATIONS");
            sb.AppendLine("=============================================================");
            
            foreach (var rec in result.Recommendations.OrderBy(r => r.Priority))
            {
                sb.AppendLine();
                sb.AppendLine($"[{rec.Priority}] {rec.Title}");
                sb.AppendLine($"{rec.Description}");
                if (!string.IsNullOrEmpty(rec.EstimatedImpact))
                    sb.AppendLine($"Estimated Impact: {rec.EstimatedImpact}");
                if (rec.Actions != null && rec.Actions.Any())
                {
                    sb.AppendLine("Actions:");
                    foreach (var action in rec.Actions)
                    {
                        sb.AppendLine($"  - {action}");
                    }
                }
                sb.AppendLine(new string('-', 60));
            }
        }

        sb.AppendLine();
        sb.AppendLine("=============================================================");
        sb.AppendLine("                    END OF REPORT");
        sb.AppendLine("=============================================================");

        return sb.ToString();
    }

    private string GenerateHtmlReport(Core.Models.AnalysisResult result)
    {
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif; 
            line-height: 1.6; 
            color: #333; 
            margin: 0; 
            padding: 0;
            background-color: #f5f5f5;
        }}
        .container {{
            max-width: 1000px;
            margin: 0 auto;
            background: white;
            box-shadow: 0 0 20px rgba(0,0,0,0.1);
        }}
        .header {{ 
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white; 
            padding: 40px 30px;
            text-align: center;
        }}
        .header h1 {{ 
            margin: 0 0 10px 0; 
            font-size: 36px;
            font-weight: 300;
        }}
        .header p {{ 
            margin: 5px 0;
            opacity: 0.9;
        }}
        .content {{ 
            padding: 30px; 
        }}
        
        /* Summary Section */
        .summary {{ 
            background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
            padding: 25px; 
            border-radius: 10px; 
            margin: 0 0 30px 0;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        }}
        .summary h2 {{
            margin: 0 0 20px 0;
            color: #2c3e50;
        }}
        .summary-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
            gap: 15px;
            margin-top: 20px;
        }}
        .summary-item {{
            background: white;
            padding: 15px;
            border-radius: 8px;
            text-align: center;
            box-shadow: 0 2px 4px rgba(0,0,0,0.05);
        }}
        .summary-item .value {{
            font-size: 28px;
            font-weight: bold;
            margin-bottom: 5px;
        }}
        .summary-item .label {{
            color: #7f8c8d;
            font-size: 14px;
            text-transform: uppercase;
        }}
        .summary-item.critical .value {{ color: #e74c3c; }}
        .summary-item.error .value {{ color: #e67e22; }}
        .summary-item.warning .value {{ color: #f39c12; }}
        .summary-item.info .value {{ color: #3498db; }}
        
        /* Database Info */
        .database-info {{
            background: #f8f9fa;
            padding: 20px;
            border-radius: 8px;
            margin-bottom: 30px;
        }}
        .database-info h3 {{
            margin: 0 0 15px 0;
            color: #2c3e50;
        }}
        .info-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 15px;
        }}
        .info-item {{
            display: flex;
            align-items: center;
        }}
        .info-item .icon {{
            width: 40px;
            height: 40px;
            background: #e3f2fd;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            margin-right: 15px;
            font-size: 20px;
        }}
        .info-item .details {{
            flex: 1;
        }}
        .info-item .details .label {{
            font-size: 12px;
            color: #7f8c8d;
            text-transform: uppercase;
        }}
        .info-item .details .value {{
            font-size: 18px;
            font-weight: 600;
            color: #2c3e50;
        }}
        
        /* Findings */
        .findings-section {{
            margin-bottom: 30px;
        }}
        .finding {{ 
            border-left: 4px solid #3498db; 
            padding: 20px; 
            margin: 15px 0; 
            background: #fff;
            border-radius: 0 8px 8px 0;
            box-shadow: 0 2px 4px rgba(0,0,0,0.05);
            transition: box-shadow 0.3s ease;
        }}
        .finding:hover {{
            box-shadow: 0 4px 8px rgba(0,0,0,0.1);
        }}
        .finding.critical {{ 
            border-color: #e74c3c; 
            background: #fee;
        }}
        .finding.error {{ 
            border-color: #e67e22; 
            background: #fef6e6;
        }}
        .finding.warning {{ 
            border-color: #f39c12; 
            background: #fffbf0;
        }}
        .finding.info {{ 
            border-color: #3498db;
            background: #f0f8ff;
        }}
        .finding h3 {{
            margin: 0 0 10px 0;
            color: #2c3e50;
            font-size: 18px;
        }}
        .finding .metadata {{
            display: flex;
            gap: 20px;
            margin-bottom: 10px;
            font-size: 14px;
            color: #7f8c8d;
        }}
        .finding .metadata span {{
            display: flex;
            align-items: center;
        }}
        .finding .description {{
            margin: 10px 0;
            line-height: 1.6;
        }}
        .finding .recommendation {{
            background: rgba(52, 152, 219, 0.1);
            padding: 10px 15px;
            border-radius: 5px;
            margin-top: 10px;
            border-left: 3px solid #3498db;
        }}
        .finding .recommendation strong {{
            color: #2980b9;
        }}
        .finding .affected-object {{
            margin-top: 10px;
            padding: 8px 12px;
            background: rgba(0,0,0,0.05);
            border-radius: 5px;
            display: inline-block;
            font-family: monospace;
            font-size: 13px;
        }}
        
        /* Performance Metrics */
        .performance-section {{
            margin: 30px 0;
        }}
        .metric-card {{
            background: #fff;
            border-radius: 8px;
            padding: 20px;
            margin-bottom: 20px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.05);
        }}
        .metric-card h4 {{
            margin: 0 0 15px 0;
            color: #2c3e50;
            display: flex;
            align-items: center;
        }}
        .metric-card h4 .count {{
            margin-left: auto;
            background: #e3f2fd;
            padding: 2px 10px;
            border-radius: 20px;
            font-size: 14px;
            font-weight: normal;
            color: #2196f3;
        }}
        .metric-items {{
            display: grid;
            gap: 10px;
        }}
        .metric-item {{
            padding: 10px;
            background: #f8f9fa;
            border-radius: 5px;
            border-left: 3px solid #2196f3;
        }}
        .metric-item .title {{
            font-weight: 600;
            color: #2c3e50;
            margin-bottom: 5px;
        }}
        .metric-item .details {{
            font-size: 14px;
            color: #7f8c8d;
        }}
        .metric-item .impact {{
            float: right;
            background: #ffeb3b;
            padding: 2px 8px;
            border-radius: 3px;
            font-size: 12px;
            color: #333;
        }}
        
        /* Recommendations */
        .recommendations-section {{
            margin-top: 40px;
            padding-top: 30px;
            border-top: 2px solid #e0e0e0;
        }}
        .recommendation-card {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 25px;
            border-radius: 10px;
            margin-bottom: 20px;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        }}
        .recommendation-card h4 {{
            margin: 0 0 10px 0;
            font-size: 20px;
        }}
        .recommendation-card .priority {{
            display: inline-block;
            background: rgba(255,255,255,0.2);
            padding: 3px 10px;
            border-radius: 20px;
            font-size: 12px;
            margin-bottom: 10px;
        }}
        .recommendation-card .description {{
            margin: 10px 0;
            opacity: 0.95;
        }}
        .recommendation-card .actions {{
            margin-top: 15px;
            padding-top: 15px;
            border-top: 1px solid rgba(255,255,255,0.2);
        }}
        .recommendation-card .actions h5 {{
            margin: 0 0 10px 0;
            font-size: 14px;
            opacity: 0.9;
        }}
        .recommendation-card .actions ul {{
            margin: 0;
            padding-left: 20px;
        }}
        .recommendation-card .actions li {{
            margin: 5px 0;
            font-family: monospace;
            font-size: 13px;
            background: rgba(0,0,0,0.1);
            padding: 5px 10px;
            border-radius: 3px;
            list-style: none;
        }}
        
        /* Footer */
        .footer {{
            background: #2c3e50;
            color: white;
            padding: 30px;
            text-align: center;
            margin-top: 50px;
        }}
        .footer p {{
            margin: 5px 0;
            opacity: 0.8;
        }}
        .footer a {{
            color: #3498db;
            text-decoration: none;
        }}
        
        /* Responsive */
        @media (max-width: 768px) {{
            .summary-grid {{
                grid-template-columns: repeat(2, 1fr);
            }}
            .info-grid {{
                grid-template-columns: 1fr;
            }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>SQL Analysis Report</h1>
            <p><strong>{result.DatabaseName}</strong> on {result.ServerName}</p>
            <p>Analysis Type: {result.DatabaseType} | Completed: {result.AnalysisEndTime:MMMM dd, yyyy HH:mm:ss}</p>
            <p>Duration: {result.Duration.TotalMinutes:F2} minutes | Objects Analyzed: {result.Summary.TotalObjectsAnalyzed}</p>
        </div>
        
        <div class='content'>
            <!-- Summary Section -->
            <div class='summary'>
                <h2>Executive Summary</h2>
                <p>This comprehensive analysis identified <strong>{result.Summary.TotalFindings}</strong> findings across your database.</p>
                <div class='summary-grid'>
                    <div class='summary-item critical'>
                        <div class='value'>{result.Summary.CriticalFindings}</div>
                        <div class='label'>Critical</div>
                    </div>
                    <div class='summary-item error'>
                        <div class='value'>{result.Summary.ErrorFindings}</div>
                        <div class='label'>Errors</div>
                    </div>
                    <div class='summary-item warning'>
                        <div class='value'>{result.Summary.WarningFindings}</div>
                        <div class='label'>Warnings</div>
                    </div>
                    <div class='summary-item info'>
                        <div class='value'>{result.Summary.InfoFindings}</div>
                        <div class='label'>Info</div>
                    </div>
                </div>
            </div>
            
            <!-- Database Information -->
            <div class='database-info'>
                <h3>Database Information</h3>
                <div class='info-grid'>
                    <div class='info-item'>
                        <div class='icon'>📊</div>
                        <div class='details'>
                            <div class='label'>Total Rows</div>
                            <div class='value'>{result.Summary.TotalRows:N0}</div>
                        </div>
                    </div>
                    <div class='info-item'>
                        <div class='icon'>📁</div>
                        <div class='details'>
                            <div class='label'>Objects Analyzed</div>
                            <div class='value'>{result.Summary.TotalObjectsAnalyzed:N0}</div>
                        </div>
                    </div>
                    <div class='info-item'>
                        <div class='icon'>⏱️</div>
                        <div class='details'>
                            <div class='label'>Analysis Time</div>
                            <div class='value'>{result.Duration.TotalMinutes:F1} min</div>
                        </div>
                    </div>
                    <div class='info-item'>
                        <div class='icon'>🔍</div>
                        <div class='details'>
                            <div class='label'>Analyzer Version</div>
                            <div class='value'>{result.AnalyzerName}</div>
                        </div>
                    </div>
                </div>
            </div>";

        // Add all findings grouped by severity
        var findingsBySeverity = result.Findings.GroupBy(f => f.Severity).OrderBy(g => g.Key);
        
        foreach (var severityGroup in findingsBySeverity)
        {
            html += $@"
            <div class='findings-section'>
                <h3>{severityGroup.Key} Findings ({severityGroup.Count()})</h3>";
            
            foreach (var finding in severityGroup)
            {
                html += $@"
                <div class='finding {finding.Severity.ToString().ToLower()}'>
                    <h3>{finding.Message}</h3>
                    <div class='metadata'>
                        <span><strong>Category:</strong> {finding.Category}</span>
                        <span><strong>Type:</strong> {finding.ObjectType}</span>
                        {(string.IsNullOrEmpty(finding.Schema) ? "" : $"<span><strong>Schema:</strong> {finding.Schema}</span>")}
                    </div>
                    <div class='description'>{finding.Description}</div>
                    {(string.IsNullOrEmpty(finding.Recommendation) ? "" : $@"
                    <div class='recommendation'>
                        <strong>Recommendation:</strong> {finding.Recommendation}
                    </div>")}
                    {(string.IsNullOrEmpty(finding.AffectedObject) ? "" : $@"
                    <div class='affected-object'>{finding.Schema}.{finding.AffectedObject}</div>")}
                </div>";
            }
            
            html += @"
            </div>";
        }

        // Add performance metrics if available
        if (result.Metrics != null && result.Metrics.Any())
        {
            html += @"
            <div class='performance-section'>
                <h2>Performance Metrics</h2>";

            foreach (var metric in result.Metrics)
            {
                html += $@"
                <div class='metric-card'>
                    <h4>{metric.Name} <span class='count'>{metric.Value}</span></h4>
                    <div class='metric-items'>";

                if (metric.Details != null && metric.Details.Any())
                {
                    foreach (var detail in metric.Details)
                    {
                        html += $@"
                        <div class='metric-item'>
                            <div class='title'>{detail.Key}</div>
                            <div class='details'>{detail.Value}</div>
                        </div>";
                    }
                }

                html += @"
                    </div>
                </div>";
            }

            html += @"
            </div>";
        }

        // Add recommendations if available
        if (result.Recommendations != null && result.Recommendations.Any())
        {
            html += @"
            <div class='recommendations-section'>
                <h2>Recommendations</h2>";

            foreach (var rec in result.Recommendations.OrderBy(r => r.Priority))
            {
                html += $@"
                <div class='recommendation-card'>
                    <div class='priority'>Priority: {rec.Priority}</div>
                    <h4>{rec.Title}</h4>
                    <div class='description'>{rec.Description}</div>
                    {(rec.EstimatedImpact != null ? $"<p><strong>Estimated Impact:</strong> {rec.EstimatedImpact}</p>" : "")}
                    {(rec.Actions != null && rec.Actions.Any() ? $@"
                    <div class='actions'>
                        <h5>Suggested Actions:</h5>
                        <ul>
                            {string.Join("", rec.Actions.Select(a => $"<li>{a}</li>"))}
                        </ul>
                    </div>" : "")}
                </div>";
            }

            html += @"
            </div>";
        }

        html += @"
        </div>
        
        <div class='footer'>
            <p>Generated by SQL Analyzer</p>
            <p>This report contains sensitive database information. Please handle with care.</p>
            <p>For more information, visit <a href='https://sqlanalyzer.com'>sqlanalyzer.com</a></p>
        </div>
    </div>
</body>
</html>";

        return html;
    }
}