using PostyLand.Application.Common.Contexts;

namespace PostyLand.Application.Features.Tenants;

public interface ITenantResolverService
{
    Task<TenantContext> ResolveAsync(string subdomain, CancellationToken cancellationToken);
}
