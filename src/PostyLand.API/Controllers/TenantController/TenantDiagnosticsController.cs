using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostyLand.Application.Common.Interfaces.AdminDbInterfaces;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;
using PostyLand.Persistence.Context;

namespace PostyLand.API.Controllers;

[Route("api/tenant")]
public sealed class TenantDiagnosticsController(
    ITenantProvider tenantProvider,
    IUserContextProvider userContextProvider,
    TenantDbContext tenantDbContext) : TenantBaseController
{
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        var tenant = tenantProvider.GetRequiredTenant();
        var user = userContextProvider.Current;
        return Ok(new
        {
            tenantId = tenant.TenantId,
            subdomain = tenant.Subdomain,
            database = tenantDbContext.Database.GetDbConnection().Database,
            userId = user?.UserId,
            role = user?.Role,
            scope = user?.Scope
        });
    }
}


