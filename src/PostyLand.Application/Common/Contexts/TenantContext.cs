namespace PostyLand.Application.Common.Contexts;

public sealed class TenantContext
{
    public Guid TenantId { get; init; }
    public string Subdomain { get; init; } = string.Empty;
    public string DecryptedConnectionString { get; init; } = string.Empty;
}
