using PostyLand.Domain.Entities;

namespace PostyLand.Application.Common.Interfaces.TenantInterfaces;

public interface IBillingHistoryStore
{
    Task AddAsync(BillingHistory entry, CancellationToken cancellationToken);
    Task<bool> SubscriptionBelongsToTenantAsync(Guid subscriptionId, Guid tenantId, CancellationToken cancellationToken);
    Task<(IReadOnlyCollection<BillingHistory> Items, int TotalCount)> GetByTenantAsync(
        Guid tenantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

