using Hangfire.Dashboard;
using PostyLand.API.Auth;

namespace PostyLand.API.Hangfire;

public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated is true &&
               AuthorizationClaimEvaluator.IsPlatformAdmin(httpContext.User);
    }
}
