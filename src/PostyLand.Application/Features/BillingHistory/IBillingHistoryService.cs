namespace PostyLand.Application.Features.BillingHistory;

public interface IBillingHistoryService
{
    Task<BillingHistoryItem> CreateForTenantAsync(
        Guid tenantId,
        CreateBillingHistoryRequest request,
        CancellationToken cancellationToken);

    Task<BillingHistoryItem> CreateForAdminAsync(
        Guid tenantId,
        CreateBillingHistoryRequest request,
        CancellationToken cancellationToken);

    Task<BillingHistoryListResponse> GetForTenantAsync(
        Guid tenantId,
        GetBillingHistoryRequest request,
        CancellationToken cancellationToken);

    Task<BillingHistoryListResponse> GetForAdminAsync(
        Guid tenantId,
        GetBillingHistoryRequest request,
        CancellationToken cancellationToken);
}
