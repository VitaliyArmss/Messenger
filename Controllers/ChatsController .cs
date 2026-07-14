using Microsoft.AspNetCore.Mvc;

namespace Messenger.Controllers;

[ApiController]
[Route("api/chats")]
public class ChatsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetChats()
    {
        throw new NotImplementedException();
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetChat(Guid id)
    {
        throw new NotImplementedException();
    }

    [HttpPost]
    public IActionResult CreateChat()
    {
        throw new NotImplementedException();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult DeleteChat(Guid id)
    {
        throw new NotImplementedException();
    }

    [HttpPost("{id:guid}/members")]
    public IActionResult AddMember(Guid id)
    {
        throw new NotImplementedException();
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public IActionResult RemoveMember(Guid id, Guid userId)
    {
        throw new NotImplementedException();
    }
}