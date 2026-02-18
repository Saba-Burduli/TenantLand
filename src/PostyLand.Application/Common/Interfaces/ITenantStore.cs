using PostyLand.Domain.Entities;

namespace PostyLand.Application.Common.Interfaces;

public interface ITenantStore
{
    Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken);
    Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<bool> ExistsBySubdomainAsync(string subdomain, CancellationToken cancellationToken);
    Task AddAsync(Tenant tenant, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
