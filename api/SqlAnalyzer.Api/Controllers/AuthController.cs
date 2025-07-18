using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlAnalyzer.Api.Models;
using SqlAnalyzer.Api.Services;

namespace SqlAnalyzer.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly AuthSettings _authSettings;

    public AuthController(
        IJwtService jwtService,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _jwtService = jwtService;
        _configuration = configuration;
        _logger = logger;
        // First try to get from Authentication section, then use defaults from Jwt if available
        _authSettings = configuration.GetSection("Authentication").Get<AuthSettings>() ?? new AuthSettings();

        // If no authentication settings found, check if we have Jwt configuration
        if (string.IsNullOrEmpty(_authSettings.JwtSecret))
        {
            var jwtKey = configuration["Jwt:Key"];
            if (!string.IsNullOrEmpty(jwtKey))
            {
                _authSettings.JwtSecret = jwtKey;
            }
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Login attempt for user {Username}. Expected: {ExpectedUsername}",
                request.Username, _authSettings.DefaultUsername);

            // Simple authentication - check against configured username/password
            // In production, this should check against a user database
            if (request.Username == _authSettings.DefaultUsername &&
                request.Password == _authSettings.DefaultPassword)
            {
                var user = new UserInfo
                {
                    Username = request.Username,
                    Role = "Admin"
                };

                var token = _jwtService.GenerateToken(user);
                var expiresAt = DateTime.UtcNow.AddHours(_authSettings.JwtExpirationHours);

                _logger.LogInformation("User {Username} logged in successfully", request.Username);

                return Ok(new LoginResponse
                {
                    Token = token,
                    Username = user.Username,
                    ExpiresAt = expiresAt
                });
            }

            _logger.LogWarning("Failed login attempt for user {Username}", request.Username);
            return Unauthorized(new { message = "Invalid username or password" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    [HttpGet("verify")]
    [Authorize]
    public IActionResult Verify()
    {
        // This endpoint verifies if the current token is valid
        var username = User.Identity?.Name;
        return Ok(new
        {
            authenticated = true,
            username = username,
            role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
        });
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        // In a stateless JWT system, logout is handled client-side
        // This endpoint can be used for logging or token blacklisting in the future
        _logger.LogInformation("User {Username} logged out", User.Identity?.Name ?? "Anonymous");
        return Ok(new { message = "Logged out successfully" });
    }

    private string HashPassword(string password)
    {
        // For the default password, we'll use a simple comparison
        // In production, all passwords should be properly hashed
        return BCrypt.Net.BCrypt.HashPassword(password);
    }
}