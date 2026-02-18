using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PostyLand.Application.Common.Interfaces;
using PostyLand.Persistence.Context;
using PostyLand.Persistence.Factories;
using PostyLand.Persistence.Repositories;

namespace PostyLand.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var mainDbConnectionString = configuration.GetConnectionString("MainDb")
            ?? throw new InvalidOperationException("Connection string 'MainDb' was not found.");

        services.AddDbContext<MainDbContext>(options =>
            options.UseNpgsql(mainDbConnectionString, npgsql => npgsql.MigrationsAssembly(typeof(MainDbContext).Assembly.FullName)));

        services.AddDbContextFactory<TenantDbContext>(_ => { }, ServiceLifetime.Scoped);
        services.AddScoped<IDbContextFactory<TenantDbContext>, TenantDbContextFactory>();
        services.AddScoped<TenantDbContext>(provider =>
            provider.GetRequiredService<IDbContextFactory<TenantDbContext>>().CreateDbContext());

        services.AddScoped<ITenantStore, TenantStore>();
        services.AddScoped<ISubscriptionStore, SubscriptionStore>();
        services.AddScoped<IAdminUserStore, AdminUserStore>();
        services.AddScoped<ITenantMigrationService, TenantMigrationService>();

        return services;
    }
}
