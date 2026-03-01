using PostyLand.Domain.Enums;

namespace PostyLand.Domain.Entities;

public sealed class BillingHistory
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public BillingHistoryEntryType EntryType { get; set; }
    public BillingHistoryRecordStatus Status { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? ProviderReference { get; set; }
    public string? Note { get; set; }

    public Tenant? Tenant { get; set; }
    public Subscription? Subscription { get; set; }
}
