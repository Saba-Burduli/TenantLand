using Microsoft.EntityFrameworkCore;
using PostyLand.Application.Common.Exceptions;
using PostyLand.Application.Common.Interfaces.AdminDbInterfaces;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;
using PostyLand.Persistence.Context;

namespace PostyLand.Persistence.Repositories;

public sealed class TenantMigrationService(
    MainDbContext mainDbContext,
    ITenantConnectionResolver tenantConnectionResolver) : ITenantMigrationService
{
    public async Task RunForTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await mainDbContext.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tenantId, cancellationToken);

        if (tenant is null)
        {
            throw new NotFoundException($"Tenant '{tenantId}' does not exist.");
        }

        var decryptedConnectionString =
            tenantConnectionResolver.ResolveDecryptedConnectionString(tenant.EncryptedConnectionString);

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(
                decryptedConnectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(TenantDbContext).Assembly.FullName))
            .Options;

        await using var tenantDbContext = new TenantDbContext(options);
        await tenantDbContext.Database.MigrateAsync(cancellationToken);
    }
}


