using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostyLand.API.Auth;

namespace PostyLand.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.PlatformAdmin)]
public abstract class AdminBaseController : ControllerBase
{
}
