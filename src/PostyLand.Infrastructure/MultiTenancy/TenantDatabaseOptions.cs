namespace PostyLand.Infrastructure.MultiTenancy;

public sealed class TenantDatabaseOptions
{
    public const string SectionName = "TenantDatabase";
    public string Template { get; init; } = string.Empty;
}
