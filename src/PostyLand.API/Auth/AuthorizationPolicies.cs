using PostyLand.Domain.Enums;

namespace PostyLand.API.Auth;

public static class AuthorizationPolicies
{
    public const string PlatformAdmin = nameof(RoleStatus.PlatformAdmin) + "Policy";
}
