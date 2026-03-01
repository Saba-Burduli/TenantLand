using Microsoft.EntityFrameworkCore;
using PostyLand.Application.Common.Interfaces.AdminDbInterfaces;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;
using PostyLand.Domain.Entities;
using PostyLand.Persistence.Context;
using PostyLand.Persistence.Repositories.BaseRepository;

namespace PostyLand.Persistence.Repositories;

public sealed class BillingHistoryStore(MainDbContext dbContext) : BaseRepository<BillingHistory>(dbContext), IBillingHistoryStore
{
    public Task AddAsync(BillingHistory entry, CancellationToken _)
    {
        return base.AddAsync(entry);
    }

    public Task<bool> SubscriptionBelongsToTenantAsync(Guid subscriptionId, Guid tenantId, CancellationToken cancellationToken)
    {
        return dbContext.Subscriptions
            .AsNoTracking()
            .AnyAsync(x => x.Id == subscriptionId && x.TenantId == tenantId, cancellationToken);
    }

    public async Task<(IReadOnlyCollection<BillingHistory> Items, int TotalCount)> GetByTenantAsync(
        Guid tenantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.BillingHistoryEntries
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return base.SaveChangesAsync();
    }
}


