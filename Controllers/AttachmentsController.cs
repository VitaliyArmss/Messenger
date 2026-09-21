using Messenger.Data;
using Messenger.Entities;
using Messenger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Messenger.Controllers
{
    [ApiController]
    [Route("api/attachments")]
    [Authorize]
    public class AttachmentsController : ControllerBase
    {
        private readonly IAttachmentService _attachmentService;
        private readonly ILogger<AttachmentsController> _logger;
        private readonly AppDbContext _dbContext;

        public AttachmentsController(IAttachmentService attachmentService, ILogger<AttachmentsController> logger, AppDbContext dbContext)
        {
            _attachmentService = attachmentService;
            _logger = logger;
            _dbContext = dbContext;
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, [FromQuery] bool? isAvatar = false)
        {
            try
            {
                var result = await _attachmentService.UploadAsync(file, IsAvatar: isAvatar ?? false);
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
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized();

                var userId = Guid.Parse(userIdClaim.Value);
                var (stream, contentType, fileName) = await _attachmentService.DownloadAsync(id, userId);
                return File(stream, contentType, fileName);
            }
            catch (Exception ex) when (ex.Message == "Файл не найден")
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
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

        [HttpGet("chat/{chatId:guid}")]
        public async Task<IActionResult> GetAttachmentsByChat(
            Guid chatId,
            [FromQuery] FileType? type,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 20)
        {
            try
            {
                var result = await _attachmentService.GetAttachmentsByChatAsync(chatId, type, skip, take);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения вложений чата {ChatId}", chatId);
                return StatusCode(500, new { Message = "Не удалось получить вложения" });
            }
        }
    }
}