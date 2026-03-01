using Microsoft.EntityFrameworkCore;
using PostyLand.Application.Common.Interfaces.AdminDbInterfaces;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;
using PostyLand.Domain.Entities;
using PostyLand.Persistence.Context;
using PostyLand.Persistence.Repositories.BaseRepository;

namespace PostyLand.Persistence.Repositories;

public sealed class SubscriptionStore(MainDbContext dbContext) : BaseRepository<Subscription>(dbContext), ISubscriptionStore
{
    public Task<Subscription?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return dbContext.Subscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);
    }

    public Task AddAsync(Subscription subscription, CancellationToken _)
    {
        return base.AddAsync(subscription);
    }
}


