using Microsoft.EntityFrameworkCore;
using PostyLand.Persistence.Context.TenantModels;

namespace PostyLand.Persistence.Context;

public sealed class TenantDbContext(DbContextOptions<TenantDbContext> options) : DbContext(options)
{
    public DbSet<TenantMigrationMarker> MigrationMarkers => Set<TenantMigrationMarker>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantMigrationMarker>(entity =>
        {
            entity.ToTable("tenant_migration_markers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
        });
    }
}
