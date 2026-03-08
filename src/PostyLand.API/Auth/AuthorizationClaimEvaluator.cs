using System.Security.Claims;
using PostyLand.Domain.Enums;

namespace PostyLand.API.Auth;

public static class AuthorizationClaimEvaluator
{
    public static bool IsPlatformAdmin(ClaimsPrincipal user)
    {
        var role = user.FindFirst(ClaimTypes.Role)?.Value
                   ?? user.FindFirst("Role")?.Value
                   ?? string.Empty;
        if (!Enum.TryParse<RoleStatus>(role, true, out var roleStatus) || roleStatus != RoleStatus.PlatformAdmin)
        {
            return false;
        }

        var scope = user.FindFirst("Scope")?.Value
                    ?? user.FindFirst("scope")?.Value
                    ?? string.Empty;
        if (string.IsNullOrWhiteSpace(scope))
        {
            return false;
        }

        var scopes = scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return scopes.Any(x => x.Equals("platform.admin", StringComparison.OrdinalIgnoreCase));
    }
}
