using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostyLand.Application.Features.Tenants;

namespace PostyLand.API.Controllers;

[Route("api/tenants")]
public sealed class TenantsController(ITenantRegistrationService tenantRegistrationService) : TenantBaseController
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterTenantRequest request, CancellationToken cancellationToken)
    {
        var result = await tenantRegistrationService.RegisterAsync(request, cancellationToken);
        return Accepted(new
        {
            result.TenantId,
            result.OnboardingJobId,
            result.Status
        });
    }
}
