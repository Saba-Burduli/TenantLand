using PostyLand.Application.Common.Contexts;

namespace PostyLand.Application.Common.Interfaces.TenantInterfaces;

public interface ITenantProvider
{
    TenantContext? Current { get; }
    void Set(TenantContext tenantContext);
    TenantContext GetRequiredTenant();
}

