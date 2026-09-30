using AndreaTV.Api.Modules.Security.Models;
using AndreaTV.Api.Modules.Security.Services;
using Microsoft.AspNetCore.Mvc;

namespace AndreaTV.Api.Modules.Security.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpGet("status")]
    [ProducesResponseType<AuthStatus>(StatusCodes.Status200OK)]
    public ActionResult<AuthStatus> GetStatus() => Ok(authService.GetStatus());
}
