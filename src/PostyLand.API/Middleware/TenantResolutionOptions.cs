namespace PostyLand.API.Middleware;

public sealed class TenantResolutionOptions
{
    public const string SectionName = "TenantResolution";
    public string RootDomain { get; init; } = "postyland.com";
    public string SubdomainHeader { get; init; } = "X-Tenant-Subdomain";
    public string[] ReservedSubdomains { get; init; } = ["api", "www"];
    public string[] BypassPathPrefixes { get; init; } =
    [
        "/health",
        "/hangfire",
        "/openapi",
        "/swagger",
        "/api/tenants/register",
        "/api/admin"
    ];
}
