using Microsoft.AspNetCore.Mvc;

namespace Messenger.Controllers;

[ApiController]
[Route("api/messages")]
public class MessagesController : ControllerBase
{
    [HttpGet("chat/{chatId:guid}")]
    public IActionResult GetMessages(Guid chatId)
    {
        throw new NotImplementedException();
    }

    [HttpPost]
    public IActionResult SendMessage()
    {
        throw new NotImplementedException();
    }

    [HttpPut("{id:guid}")]
    public IActionResult EditMessage(Guid id)
    {
        throw new NotImplementedException();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult DeleteMessage(Guid id)
    {
        throw new NotImplementedException();
    }
}