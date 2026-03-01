namespace PostyLand.Application.Features.BillingHistory;

public sealed class BillingHistoryListResponse
{
    public IReadOnlyCollection<BillingHistoryItem> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
