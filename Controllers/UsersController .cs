using Microsoft.AspNetCore.Mvc;

namespace Messenger.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        throw new NotImplementedException();
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id)
    {
        throw new NotImplementedException();
    }

    [HttpGet]
    public IActionResult Search(string? query)
    {
        throw new NotImplementedException();
    }

    [HttpPut]
    public IActionResult Update()
    {
        throw new NotImplementedException();
    }
}