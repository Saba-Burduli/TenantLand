using Microsoft.EntityFrameworkCore;
using PostyLand.Application.Common.Interfaces;
using PostyLand.Domain.Entities;
using PostyLand.Persistence.Context;

namespace PostyLand.Persistence.Repositories;

public sealed class SubscriptionStore(MainDbContext dbContext) : ISubscriptionStore
{
    public Task<Subscription?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return dbContext.Subscriptions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);
    }

    public Task AddAsync(Subscription subscription, CancellationToken cancellationToken)
    {
        return dbContext.Subscriptions.AddAsync(subscription, cancellationToken).AsTask();
    }
}
