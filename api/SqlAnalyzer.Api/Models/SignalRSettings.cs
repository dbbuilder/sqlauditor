namespace SqlAnalyzer.Api.Models;

public class SignalRSettings
{
    public bool Enabled { get; set; } = false;
    public bool EnableDetailedErrors { get; set; } = false;
    public int KeepAliveInterval { get; set; } = 15; // seconds
    public int ClientTimeoutInterval { get; set; } = 30; // seconds
}