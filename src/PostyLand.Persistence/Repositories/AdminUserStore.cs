using PostyLand.Application.Common.Interfaces.AdminDbInterfaces;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;
using PostyLand.Domain.Entities;
using PostyLand.Domain.Enums;
using PostyLand.Persistence.Context;
using PostyLand.Persistence.Repositories.BaseRepository;
using Microsoft.EntityFrameworkCore;

namespace PostyLand.Persistence.Repositories;

public sealed class AdminUserStore(MainDbContext dbContext) : BaseRepository<AdminUser>(dbContext), IAdminUserStore
{
    public Task AddAsync(AdminUser adminUser, CancellationToken _)
    {
        return base.AddAsync(adminUser);
    }

    public Task<AdminUser?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return dbContext.AdminUsers.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
    }

    public Task<AdminUser?> GetByExternalProviderAsync(
        ExternalAuthProvider provider,
        string externalProviderId,
        CancellationToken cancellationToken)
    {
        return dbContext.AdminUsers.FirstOrDefaultAsync(
            x => x.ExternalProvider == provider && x.ExternalProviderId == externalProviderId,
            cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}


