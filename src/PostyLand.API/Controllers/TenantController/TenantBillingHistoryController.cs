using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostyLand.Application.Common.Interfaces.AdminDbInterfaces;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;
using PostyLand.Application.Features.BillingHistory;

namespace PostyLand.API.Controllers;

[Route("api/tenant/billing-history")]
public sealed class TenantBillingHistoryController(
    ITenantProvider tenantProvider,
    IBillingHistoryService billingHistoryService) : TenantBaseController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBillingHistoryRequest request, CancellationToken cancellationToken)
    {
        var tenantId = tenantProvider.GetRequiredTenant().TenantId;
        var item = await billingHistoryService.CreateForTenantAsync(tenantId, request, cancellationToken);
        return Ok(item);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] GetBillingHistoryRequest request, CancellationToken cancellationToken)
    {
        var tenantId = tenantProvider.GetRequiredTenant().TenantId;
        var result = await billingHistoryService.GetForTenantAsync(tenantId, request, cancellationToken);
        return Ok(result);
    }
}


