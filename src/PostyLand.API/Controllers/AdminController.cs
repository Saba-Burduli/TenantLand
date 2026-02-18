using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostyLand.API.Auth;
using PostyLand.Application.Common.Interfaces;

namespace PostyLand.API.Controllers;

[ApiController]
[Route("api/admin/tenants")]
[Authorize(Policy = AuthorizationPolicies.PlatformAdmin)]
public sealed class AdminController(ITenantMigrationService tenantMigrationService) : ControllerBase
{
    [HttpPost("{tenantId:guid}/migrations/run")]
    public async Task<IActionResult> RunTenantMigration(Guid tenantId, CancellationToken cancellationToken)
    {
        await tenantMigrationService.RunForTenantAsync(tenantId, cancellationToken);
        return Ok(new { tenantId, status = "MigrationCompleted" });
    }
}
