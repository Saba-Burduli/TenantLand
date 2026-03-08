using Microsoft.Extensions.Logging;
using PostyLand.Application.Common.Contexts;
using PostyLand.Application.Common.Exceptions;
using PostyLand.Application.Common.Interfaces.AdminDbInterfaces;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;
using PostyLand.Domain.Enums;

namespace PostyLand.Application.Features.Tenants;

public sealed class TenantOnboardingOrchestrator(
    ITenantStore tenantStore,
    ITenantConnectionResolver tenantConnectionResolver,
    ITenantProvisioningService provisioningService,
    ILogger<TenantOnboardingOrchestrator> logger) : ITenantOnboardingOrchestrator
{
    public async Task ExecuteAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await tenantStore.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new NotFoundException($"Tenant '{tenantId}' does not exist.");

        try
        {
            tenant.Status = TenantOnboardingStatus.Pending;
            await tenantStore.SaveChangesAsync(cancellationToken);

            var tenantContext = new TenantContext
            {
                TenantId = tenant.Id,
                Subdomain = tenant.Subdomain,
                DecryptedConnectionString = tenantConnectionResolver.ResolveDecryptedConnectionString(
                    tenant.EncryptedConnectionString)
            };

            await provisioningService.CreateDatabaseAsync(tenantContext, cancellationToken);
            tenant.Status = TenantOnboardingStatus.DatabaseCreated;
            await tenantStore.SaveChangesAsync(cancellationToken);

            await provisioningService.ConfigureDnsAsync(tenantContext, cancellationToken);
            tenant.Status = TenantOnboardingStatus.DnsConfigured;
            await tenantStore.SaveChangesAsync(cancellationToken);

            await provisioningService.CreateBucketAsync(tenantContext, cancellationToken);
            tenant.Status = TenantOnboardingStatus.BucketCreated;
            await tenantStore.SaveChangesAsync(cancellationToken);

            tenant.Status = TenantOnboardingStatus.Completed;
            await tenantStore.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            tenant.Status = TenantOnboardingStatus.Failed;
            await tenantStore.SaveChangesAsync(cancellationToken);
            logger.LogError(ex, "Tenant onboarding failed for {TenantId}", tenant.Id);
            throw new InfrastructureException($"Onboarding failed for tenant '{tenant.Id}'.", ex);
        }
    }
}


