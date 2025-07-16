using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace SqlAnalyzer.Api.Middleware;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly bool _requireAuthentication;
    
    public HangfireAuthorizationFilter(bool requireAuthentication = true)
    {
        _requireAuthentication = requireAuthentication;
    }
    
    public bool Authorize([NotNull] DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Allow access in development without authentication
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        if (isDevelopment && !_requireAuthentication)
        {
            return true;
        }

        // Check for localhost access
        if (httpContext.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            httpContext.Request.Host.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // In production, require authentication
        // Note: Hangfire dashboard doesn't automatically integrate with JWT auth
        // For production, consider using basic auth or IP restrictions
        
        // For now, check if user is authenticated (this requires the auth middleware to run first)
        if (httpContext.User.Identity?.IsAuthenticated ?? false)
        {
            // Additional check: only allow admin users
            return httpContext.User.IsInRole("Admin") || 
                   httpContext.User.Identity.Name == "admin";
        }
        
        return false;
    }
}