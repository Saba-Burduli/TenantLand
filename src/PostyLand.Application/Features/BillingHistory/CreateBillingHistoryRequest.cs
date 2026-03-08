using PostyLand.Domain.Enums;

namespace PostyLand.Application.Features.BillingHistory;

public sealed class CreateBillingHistoryRequest
{
    public Guid? SubscriptionId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public BillingHistoryEntryType EntryType { get; init; }
    public BillingHistoryRecordStatus Status { get; init; }
    public DateTime OccurredAt { get; init; }
    public string? ProviderReference { get; init; }
    public string? Note { get; init; }
}
