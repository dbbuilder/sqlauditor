using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SqlAnalyzer.Api.Models;

namespace SqlAnalyzer.Api.Services;

public interface IJwtService
{
    string GenerateToken(UserInfo user);
    ClaimsPrincipal? ValidateToken(string token);
}

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly AuthSettings _authSettings;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _authSettings = configuration.GetSection("Authentication").Get<AuthSettings>() ?? new AuthSettings();
        
        // Generate a default JWT secret if not configured
        if (string.IsNullOrEmpty(_authSettings.JwtSecret))
        {
            _authSettings.JwtSecret = GenerateDefaultJwtSecret();
            _logger.LogWarning("No JWT secret configured. Using generated secret. Configure 'Authentication:JwtSecret' for production.");
        }
    }

    public string GenerateToken(UserInfo user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_authSettings.JwtSecret);
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
            new("username", user.Username)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(_authSettings.JwtExpirationHours),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_authSettings.JwtSecret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed");
            return null;
        }
    }

    private string GenerateDefaultJwtSecret()
    {
        // Generate a secure random key for JWT signing
        var random = new Random();
        var bytes = new byte[64];
        random.NextBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}