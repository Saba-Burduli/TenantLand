using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PostyLand.Application.Common.Interfaces.AdminDbInterfaces;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;
using PostyLand.Persistence.Context;

namespace PostyLand.Persistence.Factories;

public sealed class TenantDbContextFactory(
    ITenantProvider tenantProvider) : IDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext()
    {
        var tenant = tenantProvider.GetRequiredTenant();
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(
                tenant.DecryptedConnectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(TenantDbContext).Assembly.FullName))
            .Options;

        return new TenantDbContext(options);
    }

    Task<TenantDbContext> IDbContextFactory<TenantDbContext>.CreateDbContextAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(CreateDbContext());
    }
}


