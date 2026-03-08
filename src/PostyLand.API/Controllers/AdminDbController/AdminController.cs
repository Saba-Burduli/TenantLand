using Microsoft.AspNetCore.Mvc;
using PostyLand.Application.Common.Interfaces.AdminDbInterfaces;
using PostyLand.Application.Common.Interfaces.TenantInterfaces;
using PostyLand.Application.Features.BillingHistory;

namespace PostyLand.API.Controllers;

[Route("api/admin/tenants")]
public sealed class AdminController(
    ITenantMigrationService tenantMigrationService,
    IBillingHistoryService billingHistoryService) : AdminBaseController
{
    [HttpPost("{tenantId:guid}/migrations/run")]
    public async Task<IActionResult> RunTenantMigration(Guid tenantId, CancellationToken cancellationToken)
    {
        await tenantMigrationService.RunForTenantAsync(tenantId, cancellationToken);
        return Ok(new { tenantId, status = "MigrationCompleted" });
    }

    [HttpGet("{tenantId:guid}/billing-history")]
    public async Task<IActionResult> GetTenantBillingHistory(
        Guid tenantId,
        [FromQuery] GetBillingHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await billingHistoryService.GetForAdminAsync(tenantId, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{tenantId:guid}/billing-history")]
    public async Task<IActionResult> CreateTenantBillingHistory(
        Guid tenantId,
        [FromBody] CreateBillingHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var item = await billingHistoryService.CreateForAdminAsync(tenantId, request, cancellationToken);
        return Ok(item);
    }
}


