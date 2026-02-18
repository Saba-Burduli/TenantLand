using Microsoft.EntityFrameworkCore;
using Npgsql;
using PostyLand.Application.Common.Exceptions;
using PostyLand.Application.Common.Interfaces;
using PostyLand.Domain.Entities;
using PostyLand.Persistence.Context;

namespace PostyLand.Persistence.Repositories;

public sealed class TenantStore(MainDbContext dbContext) : ITenantStore
{
    public Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken)
    {
        return dbContext.Tenants.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Subdomain == subdomain, cancellationToken);
    }

    public Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return dbContext.Tenants.SingleOrDefaultAsync(x => x.Id == tenantId, cancellationToken);
    }

    public Task<bool> ExistsBySubdomainAsync(string subdomain, CancellationToken cancellationToken)
    {
        return dbContext.Tenants.AnyAsync(x => x.Subdomain == subdomain, cancellationToken);
    }

    public Task AddAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        return dbContext.Tenants.AddAsync(tenant, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return SaveChangesInternalAsync(cancellationToken);
    }

    private async Task SaveChangesInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new ConflictException("Unique constraint violation occurred while saving tenant data.");
        }
    }
}
