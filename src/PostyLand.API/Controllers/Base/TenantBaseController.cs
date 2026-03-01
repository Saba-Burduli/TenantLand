using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PostyLand.API.Controllers;

[ApiController]
[Authorize]
public abstract class TenantBaseController : ControllerBase
{
}
