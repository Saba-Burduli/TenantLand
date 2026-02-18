using PostyLand.Application.Common.Contexts;
using PostyLand.Application.Common.Exceptions;
using PostyLand.Application.Common.Interfaces;
using PostyLand.Domain.Enums;

namespace PostyLand.Application.Features.Tenants;

public sealed class TenantResolverService(
    ITenantStore tenantStore,
    ISubscriptionStore subscriptionStore,
    ITenantConnectionResolver tenantConnectionResolver) : ITenantResolverService
{
    public async Task<TenantContext> ResolveAsync(string subdomain, CancellationToken cancellationToken)
    {
        var normalizedSubdomain = subdomain.Trim().ToLowerInvariant();
        var tenant = await tenantStore.GetBySubdomainAsync(normalizedSubdomain, cancellationToken);
        if (tenant is null)
        {
            throw new NotFoundException("Tenant was not found.");
        }

        if (!tenant.IsActive || tenant.Status != TenantOnboardingStatus.Completed)
        {
            throw new ForbiddenException("Tenant is not active or not fully onboarded.");
        }

        var subscription = await subscriptionStore.GetByTenantIdAsync(tenant.Id, cancellationToken);
        if (subscription is null)
        {
            throw new ForbiddenException("Tenant subscription was not found.");
        }

        var subscriptionInvalid = !subscription.IsActive ||
                                  subscription.BillingStatus != BillingStatus.Active ||
                                  subscription.RenewalDate < DateTime.UtcNow;
        if (subscriptionInvalid)
        {
            throw new ForbiddenException("Tenant subscription is not valid.");
        }

        return new TenantContext
        {
            TenantId = tenant.Id,
            Subdomain = tenant.Subdomain,
            DecryptedConnectionString =
                tenantConnectionResolver.ResolveDecryptedConnectionString(tenant.EncryptedConnectionString)
        };
    }
}
