using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PostyLand.Persistence.Context;

namespace PostyLand.Persistence.Factories;

public sealed class DesignTimeTenantDbContextFactory : IDesignTimeDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("POSTYLAND_TENANT_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=postyland_tenant_template_dev;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(TenantDbContext).Assembly.FullName))
            .Options;

        return new TenantDbContext(options);
    }
}
