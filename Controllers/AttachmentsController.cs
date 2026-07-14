using Microsoft.AspNetCore.Mvc;

namespace Messenger.Controllers;

[ApiController]
[Route("api/attachments")]
public class AttachmentsController : ControllerBase
{
    [HttpPost]
    public IActionResult Upload()
    {
        throw new NotImplementedException();
    }

    [HttpGet("{id:guid}")]
    public IActionResult Download(Guid id)
    {
        throw new NotImplementedException();
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        throw new NotImplementedException();
    }
}