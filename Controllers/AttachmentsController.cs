// Controllers/AttachmentsController.cs
using Messenger.DTO.Attachments;
using Messenger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Messenger.Controllers
{
    [ApiController]
    [Route("api/attachments")]
    //[Authorize]
    public class AttachmentsController : ControllerBase
    {
        private readonly IAttachmentService _attachmentService;
        private readonly ILogger<AttachmentsController> _logger;

        public AttachmentsController(IAttachmentService attachmentService, ILogger<AttachmentsController> logger)
        {
            _attachmentService = attachmentService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            try
            {
                var result = await _attachmentService.UploadAsync(file);
                return CreatedAtAction(nameof(Download), new { id = result.Id }, result);
            }
            catch (Exception ex) when (ex.Message.Contains("отсутствует"))
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки файла");
                return StatusCode(500, new { Message = "Не удалось загрузить файл" });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Download(Guid id)
        {
            try
            {
                var (stream, contentType, fileName) = await _attachmentService.DownloadAsync(id);
                return File(stream, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка скачивания файла {FileId}", id);
                return StatusCode(500, new { Message = "Не удалось скачать файл" });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _attachmentService.DeleteAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка удаления файла {FileId}", id);
                return StatusCode(500, new { Message = "Не удалось удалить файл" });
            }
        }
    }
}