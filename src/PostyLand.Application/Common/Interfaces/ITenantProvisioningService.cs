using PostyLand.Application.Common.Contexts;

namespace PostyLand.Application.Common.Interfaces;

public interface ITenantProvisioningService
{
    Task CreateDatabaseAsync(TenantContext tenantContext, CancellationToken cancellationToken);
    Task ConfigureDnsAsync(TenantContext tenantContext, CancellationToken cancellationToken);
    Task CreateBucketAsync(TenantContext tenantContext, CancellationToken cancellationToken);
}
