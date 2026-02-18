using PostyLand.Application.Common.Interfaces;
using PostyLand.Domain.Entities;
using PostyLand.Persistence.Context;

namespace PostyLand.Persistence.Repositories;

public sealed class AdminUserStore(MainDbContext dbContext) : IAdminUserStore
{
    public Task AddAsync(AdminUser adminUser, CancellationToken cancellationToken)
    {
        return dbContext.AdminUsers.AddAsync(adminUser, cancellationToken).AsTask();
    }
}
