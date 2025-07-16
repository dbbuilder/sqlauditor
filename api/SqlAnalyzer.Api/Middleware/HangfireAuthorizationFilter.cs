using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace SqlAnalyzer.Api.Middleware;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize([NotNull] DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Allow access in development
        if (httpContext.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // In production, require authentication
        return httpContext.User.Identity?.IsAuthenticated ?? false;
    }
}