namespace PostyLand.Application.Features.BillingHistory;

public sealed class GetBillingHistoryRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
