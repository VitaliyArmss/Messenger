using Microsoft.AspNetCore.Mvc;

namespace Messenger.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("register")]
    public IActionResult Register()
    {
        throw new NotImplementedException();
    }

    [HttpPost("login")]
    public IActionResult Login()
    {
        throw new NotImplementedException();
    }

    [HttpPost("refresh")]
    public IActionResult Refresh()
    {
        throw new NotImplementedException();
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        throw new NotImplementedException();
    }
}