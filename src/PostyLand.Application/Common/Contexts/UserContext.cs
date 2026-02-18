namespace PostyLand.Application.Common.Contexts;

public sealed class UserContext
{
    public Guid UserId { get; init; }
    public Guid TenantId { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;
}
