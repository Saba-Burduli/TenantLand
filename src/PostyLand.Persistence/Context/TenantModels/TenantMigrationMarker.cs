namespace PostyLand.Persistence.Context.TenantModels;

public sealed class TenantMigrationMarker
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
