using FluentValidation;
using PostyLand.Application.Common.Exceptions;
using PostyLand.Application.Common.Interfaces.AdminDbInterfaces;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;
using PostyLand.Domain.Entities;
using DomainBillingHistory = PostyLand.Domain.Entities.BillingHistory;

namespace PostyLand.Application.Features.BillingHistory;

public sealed class BillingHistoryService(
    IValidator<CreateBillingHistoryRequest> createValidator,
    IBillingHistoryStore billingHistoryStore,
    ITenantStore tenantStore,
    IPaymentService paymentService,
    IUserContextProvider userContextProvider) : IBillingHistoryService
{
    public Task<BillingHistoryItem> CreateForTenantAsync(
        Guid tenantId,
        CreateBillingHistoryRequest request,
        CancellationToken cancellationToken)
    {
        return CreateInternalAsync(tenantId, request, validateTenantExists: false, cancellationToken);
    }

    public Task<BillingHistoryItem> CreateForAdminAsync(
        Guid tenantId,
        CreateBillingHistoryRequest request,
        CancellationToken cancellationToken)
    {
        return CreateInternalAsync(tenantId, request, validateTenantExists: true, cancellationToken);
    }

    private async Task<BillingHistoryItem> CreateInternalAsync(
        Guid tenantId,
        CreateBillingHistoryRequest request,
        bool validateTenantExists,
        CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (validateTenantExists)
        {
            var tenant = await tenantStore.GetByIdAsync(tenantId, cancellationToken);
            if (tenant is null)
            {
                throw new NotFoundException($"Tenant '{tenantId}' was not found.");
            }
        }

        if (request.SubscriptionId.HasValue)
        {
            var belongsToTenant = await billingHistoryStore.SubscriptionBelongsToTenantAsync(
                request.SubscriptionId.Value,
                tenantId,
                cancellationToken);
            if (!belongsToTenant)
            {
                throw new ValidationException("Subscription does not belong to the resolved tenant.");
            }
        }

        var userId = userContextProvider.Current?.UserId ?? Guid.Empty;
        var paymentProcessed = await paymentService.ProcessPaymentAsync(
            request.Amount,
            request.Currency.Trim(),
            userId);
        if (!paymentProcessed)
        {
            throw new InfrastructureException("Payment processing failed.");
        }

        var entry = new DomainBillingHistory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SubscriptionId = request.SubscriptionId,
            Amount = request.Amount,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            EntryType = request.EntryType,
            Status = request.Status,
            OccurredAt = request.OccurredAt.ToUniversalTime(),
            ProviderReference = request.ProviderReference?.Trim(),
            Note = request.Note?.Trim()
        };

        await billingHistoryStore.AddAsync(entry, cancellationToken);
        await billingHistoryStore.SaveChangesAsync(cancellationToken);

        return ToItem(entry);
    }

    public Task<BillingHistoryListResponse> GetForTenantAsync(
        Guid tenantId,
        GetBillingHistoryRequest request,
        CancellationToken cancellationToken)
    {
        return GetByTenantInternalAsync(tenantId, request, cancellationToken);
    }

    public async Task<BillingHistoryListResponse> GetForAdminAsync(
        Guid tenantId,
        GetBillingHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantStore.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            throw new NotFoundException($"Tenant '{tenantId}' was not found.");
        }

        return await GetByTenantInternalAsync(tenantId, request, cancellationToken);
    }

    private async Task<BillingHistoryListResponse> GetByTenantInternalAsync(
        Guid tenantId,
        GetBillingHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize switch
        {
            <= 0 => 50,
            > 200 => 200,
            _ => request.PageSize
        };

        var (items, totalCount) = await billingHistoryStore.GetByTenantAsync(tenantId, page, pageSize, cancellationToken);
        return new BillingHistoryListResponse
        {
            Items = items.Select(ToItem).ToArray(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private static BillingHistoryItem ToItem(DomainBillingHistory entry)
    {
        return new BillingHistoryItem
        {
            Id = entry.Id,
            TenantId = entry.TenantId,
            SubscriptionId = entry.SubscriptionId,
            Amount = entry.Amount,
            Currency = entry.Currency,
            EntryType = entry.EntryType,
            Status = entry.Status,
            OccurredAt = entry.OccurredAt,
            ProviderReference = entry.ProviderReference,
            Note = entry.Note
        };
    }
}


