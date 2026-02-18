using Microsoft.EntityFrameworkCore;
using PostyLand.Domain.Entities;

namespace PostyLand.Persistence.Context;

public sealed class MainDbContext(DbContextOptions<MainDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Subdomain).HasMaxLength(63).IsRequired();
            entity.Property(x => x.EncryptedConnectionString).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => x.Subdomain).IsUnique();
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.ToTable("subscriptions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Plan).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EmployeeLimit).IsRequired();
            entity.Property(x => x.BillingStatus).IsRequired();
            entity.Property(x => x.RenewalDate).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasOne(x => x.Tenant)
                .WithOne(x => x.Subscription)
                .HasForeignKey<Subscription>(x => x.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.TenantId).IsUnique();
        });

        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.ToTable("admin_users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
        });
    }
}
