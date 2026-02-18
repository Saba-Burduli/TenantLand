using PostyLand.Application.Common.Contexts;
using PostyLand.Application.Common.Exceptions;
using PostyLand.Application.Common.Interfaces;

namespace PostyLand.Infrastructure.MultiTenancy;

public sealed class TenantProvider : ITenantProvider
{
    public TenantContext? Current { get; private set; }

    public void Set(TenantContext tenantContext)
    {
        Current = tenantContext;
    }

    public TenantContext GetRequiredTenant()
    {
        return Current ?? throw new ForbiddenException("Tenant context was not set.");
    }
}
