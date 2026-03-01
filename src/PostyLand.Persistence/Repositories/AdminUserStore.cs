using PostyLand.Application.Common.Interfaces.AdminDbInterfaces;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;
using PostyLand.Domain.Entities;
using PostyLand.Persistence.Context;
using PostyLand.Persistence.Repositories.BaseRepository;

namespace PostyLand.Persistence.Repositories;

public sealed class AdminUserStore(MainDbContext dbContext) : BaseRepository<AdminUser>(dbContext), IAdminUserStore
{
    public Task AddAsync(AdminUser adminUser, CancellationToken _)
    {
        return base.AddAsync(adminUser);
    }
}


