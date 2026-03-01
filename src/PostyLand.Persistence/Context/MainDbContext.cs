using Microsoft.EntityFrameworkCore;
using PostyLand.Domain.Entities;

namespace PostyLand.Persistence.Context;

public sealed class MainDbContext(DbContextOptions<MainDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<BillingHistory> BillingHistoryEntries => Set<BillingHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DbContext).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MainDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
