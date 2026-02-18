using PostyLand.Domain.Entities;

namespace PostyLand.Application.Common.Interfaces;

public interface ISubscriptionStore
{
    Task<Subscription?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken);
    Task AddAsync(Subscription subscription, CancellationToken cancellationToken);
}
