using PostyLand.Domain.Enums;

namespace PostyLand.Application.Common.Contexts;

public sealed class UserContext
{
    public Guid UserId { get; init; }
    public Guid TenantId { get; init; }
    public string Subdomain { get; init; } = string.Empty;
    public RoleStatus Role { get; init; }
    public string Scope { get; init; } = string.Empty;
}
