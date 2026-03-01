using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PostyLand.Domain.Entities;

namespace PostyLand.Persistence.Configurations;

public abstract class BaseEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : class
{
    public abstract void Configure(EntityTypeBuilder<TEntity> entity);
}

public sealed class TenantEntityConfiguration : BaseEntityConfiguration<Tenant>
{
    public override void Configure(EntityTypeBuilder<Tenant> entity)
    {
        entity.ToTable("tenants");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Subdomain).HasMaxLength(63).IsRequired();
        entity.Property(x => x.EncryptedConnectionString).IsRequired();
        entity.Property(x => x.IsActive).IsRequired();
        entity.Property(x => x.CreatedAt).IsRequired();
        entity.HasIndex(x => x.Subdomain).IsUnique();
    }
}

public sealed class SubscriptionEntityConfiguration : BaseEntityConfiguration<Subscription>
{
    public override void Configure(EntityTypeBuilder<Subscription> entity)
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
    }
}

public sealed class AdminUserEntityConfiguration : BaseEntityConfiguration<AdminUser>
{
    public override void Configure(EntityTypeBuilder<AdminUser> entity)
    {
        entity.ToTable("admin_users");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
        entity.Property(x => x.PasswordHash).IsRequired();
        entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(50).IsRequired();
        entity.HasIndex(x => x.Email).IsUnique();
    }
}

public sealed class BillingHistoryEntityConfiguration : BaseEntityConfiguration<BillingHistory>
{
    public override void Configure(EntityTypeBuilder<BillingHistory> entity)
    {
        entity.ToTable("billing_history");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        entity.Property(x => x.EntryType).IsRequired();
        entity.Property(x => x.Status).IsRequired();
        entity.Property(x => x.OccurredAt).IsRequired();
        entity.Property(x => x.ProviderReference).HasMaxLength(200);
        entity.Property(x => x.Note).HasMaxLength(1000);
        entity.HasIndex(x => new { x.TenantId, x.OccurredAt });

        entity.HasOne(x => x.Tenant)
            .WithMany(x => x.BillingHistoryEntries)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(x => x.Subscription)
            .WithMany(x => x.BillingHistoryEntries)
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
