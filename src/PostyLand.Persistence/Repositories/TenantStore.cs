using Microsoft.EntityFrameworkCore;
using Npgsql;
using PostyLand.Application.Common.Exceptions;
using PostyLand.Application.Common.Interfaces.AdminDbInterfaces;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;
using PostyLand.Domain.Entities;
using PostyLand.Persistence.Context;
using PostyLand.Persistence.Repositories.BaseRepository;

namespace PostyLand.Persistence.Repositories;

public sealed class TenantStore(MainDbContext dbContext) : BaseRepository<Tenant>(dbContext), ITenantStore
{
    public Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken)
    {
        return dbContext.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Subdomain == subdomain, cancellationToken);
    }

    public Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken _)
    {
        return base.GetByIdAsync(tenantId);
    }

    public Task<bool> ExistsBySubdomainAsync(string subdomain, CancellationToken cancellationToken)
    {
        return dbContext.Tenants.AnyAsync(x => x.Subdomain == subdomain, cancellationToken);
    }

    public Task AddAsync(Tenant tenant, CancellationToken _)
    {
        return base.AddAsync(tenant);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return SaveChangesInternalAsync(cancellationToken);
    }

    private async Task SaveChangesInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            await base.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new ConflictException("Unique constraint violation occurred while saving tenant data.");
        }
    }
}


