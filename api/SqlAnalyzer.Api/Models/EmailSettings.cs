namespace SqlAnalyzer.Api.Models;

public class EmailSettings
{
    public bool Enabled { get; set; } = false;
    public string Provider { get; set; } = "SendGrid";
    public string SendGridApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "noreply@sqlanalyzer.com";
    public string FromName { get; set; } = "SQL Analyzer";
    public bool SendReportsOnCompletion { get; set; } = true;
}