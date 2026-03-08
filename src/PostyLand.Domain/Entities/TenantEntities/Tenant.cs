using PostyLand.Domain.Enums;

namespace PostyLand.Domain.Entities;

public sealed class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string EncryptedConnectionString { get; set; } = string.Empty;
    public TenantOnboardingStatus Status { get; set; } = TenantOnboardingStatus.Pending;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Subscription? Subscription { get; set; }
    public ICollection<BillingHistory> BillingHistoryEntries { get; set; } = [];
}
