namespace SqlAnalyzer.Api.Models;

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class UserInfo
{
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
}

public class AuthSettings
{
    public string JwtSecret { get; set; } = string.Empty;
    public int JwtExpirationHours { get; set; } = 24;
    public string DefaultUsername { get; set; } = "admin";
    public string DefaultPassword { get; set; } = "SqlAnalyzer2024!";
}