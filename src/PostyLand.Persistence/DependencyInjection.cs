using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PostyLand.Application.Common.Interfaces.AdminDbInterfaces;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;
using PostyLand.Persistence.Context;
using PostyLand.Persistence.Factories;
using PostyLand.Persistence.Repositories;
using PostyLand.Persistence.Repositories.BaseRepository;
using PostyLand.Persistence.Repositories.Interfaces;

namespace PostyLand.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var mainDbConnectionString = configuration.GetConnectionString("MainDb")
            ?? throw new InvalidOperationException("Connection string 'MainDb' was not found.");

        services.AddDbContext<MainDbContext>(options =>
            options.UseNpgsql(mainDbConnectionString, npgsql => npgsql.MigrationsAssembly(typeof(MainDbContext).Assembly.FullName)));
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<MainDbContext>());

        services.AddDbContextFactory<TenantDbContext>(_ => { }, ServiceLifetime.Scoped);
        services.AddScoped<IDbContextFactory<TenantDbContext>, TenantDbContextFactory>();
        services.AddScoped<TenantDbContext>(provider =>
            provider.GetRequiredService<IDbContextFactory<TenantDbContext>>().CreateDbContext());

        services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
        services.AddScoped<ITenantStore, TenantStore>();
        services.AddScoped<ISubscriptionStore, SubscriptionStore>();
        services.AddScoped<IAdminUserStore, AdminUserStore>();
        services.AddScoped<IBillingHistoryStore, BillingHistoryStore>();
        services.AddScoped<ITenantMigrationService, TenantMigrationService>();

        return services;
    }
}


