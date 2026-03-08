using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostyLand.Application.Features.ExternalAuth;
using PostyLand.Domain.Enums;

namespace PostyLand.API.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public sealed class ExternalAuthController(IExternalAuthService externalAuthService) : ControllerBase
{
    [HttpGet("google/login")]
    public IActionResult GoogleLogin([FromQuery] string subdomain, [FromQuery] string? returnUrl = null)
    {
        var redirectUrl = externalAuthService.GetLoginUrl(ExternalAuthProvider.Google, subdomain, returnUrl);
        return Redirect(redirectUrl);
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback(
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken cancellationToken)
    {
        var result = await externalAuthService.HandleCallbackAsync(
            ExternalAuthProvider.Google,
            code,
            state,
            cancellationToken);
        return Redirect(result.RedirectUrl);
    }

    [HttpGet("microsoft/login")]
    public IActionResult MicrosoftLogin([FromQuery] string subdomain, [FromQuery] string? returnUrl = null)
    {
        var redirectUrl = externalAuthService.GetLoginUrl(ExternalAuthProvider.Microsoft, subdomain, returnUrl);
        return Redirect(redirectUrl);
    }

    [HttpGet("microsoft/callback")]
    public async Task<IActionResult> MicrosoftCallback(
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken cancellationToken)
    {
        var result = await externalAuthService.HandleCallbackAsync(
            ExternalAuthProvider.Microsoft,
            code,
            state,
            cancellationToken);
        return Redirect(result.RedirectUrl);
    }
}
