using Messenger.DTO.Chats;
using Messenger.DTO.Messages;
using Messenger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Messenger.Controllers
{
    [ApiController]
    [Route("api/messages")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(IMessageService messageService, ILogger<MessagesController> logger)
        {
            _messageService = messageService;
            _logger = logger;
        }

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        [HttpGet("chat/{chatId:guid}")]
        public async Task<IActionResult> GetMessages(Guid chatId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
        {
            try
            {
                var messages = await _messageService.GetMessagesAsync(chatId, skip, take);
                return Ok(messages);
            }
            catch (Exception ex) when (ex.Message.Contains("не найден"))
            {
                return NotFound(new { Message = "Чат не найден" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения сообщений чата {ChatId}", chatId);
                return StatusCode(500, new { Message = "Не удалось получить сообщения" });
            }
        }

        // Controllers/MessagesController.cs
        [HttpGet("chat/{chatId:guid}/around/{messageId:guid}")]
        public async Task<IActionResult> GetMessagesAround(Guid chatId, Guid messageId, [FromQuery] int count = 50)
        {
            try
            {
                var messages = await _messageService.GetMessagesAroundAsync(chatId, messageId, count);
                return Ok(messages);
            }
            catch (Exception ex) when (ex.Message.Contains("не найдено"))
            {
                Console.WriteLine(ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения сообщений вокруг {MessageId}", messageId);
                return StatusCode(500, new { Message = "Не удалось получить сообщения" });
            }
        }

        // Controllers/MessagesController.cs
        [HttpGet("chat/{chatId:guid}/direction/{anchorId:guid}")]
        public async Task<IActionResult> GetMessagesByDirection(
            Guid chatId,
            Guid anchorId,
            [FromQuery] string direction,
            [FromQuery] int count = 50)
        {
            try
            {
                if (!(direction.Equals("older", StringComparison.OrdinalIgnoreCase) ||
                      direction.Equals("newer", StringComparison.OrdinalIgnoreCase)))
                {
                    return BadRequest(new { Message = "Направление должно быть 'older' или 'newer'." });
                }

                var messages = await _messageService.GetMessagesByDirectionAsync(chatId, anchorId, direction, count);
                return Ok(messages);
            }
            catch (Exception ex) when (ex.Message.Contains("не найден") || ex.Message.Contains("не найдено"))
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения сообщений по направлению. ChatId: {ChatId}, Anchor: {AnchorId}", chatId, anchorId);
                return StatusCode(500, new { Message = "Не удалось получить сообщения" });
            }
        }

        // Controllers/MessagesController.cs
        [HttpGet("chat/{chatId:guid}/first-unread")]
        public async Task<IActionResult> GetFirstUnread(Guid chatId)
        {
            try
            {
                var userId = GetUserId();
                var messageId = await _messageService.GetFirstUnreadMessageIdAsync(chatId, userId);
                return Ok(new { messageId });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения первого непрочитанного в чате {ChatId}", chatId);
                return StatusCode(500, new { Message = "Не удалось получить первое непрочитанное сообщение" });
            }
        }

        [HttpPost("chat/{chatId:guid}")]
        public async Task<IActionResult> SendMessage(Guid chatId, [FromBody] SendMessageRequest request)
        {
            try
            {
                var senderId = GetUserId();
                var message = await _messageService.SendAsync(chatId, senderId, request);
                return CreatedAtAction(nameof(GetMessages), new { chatId }, message);
            }
            catch (Exception ex) when (ex.Message.Contains("не найден"))
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex) when (ex.Message.Contains("не состоит в чате"))
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки сообщения в чат {ChatId}", chatId);
                return StatusCode(500, new { Message = "Не удалось отправить сообщение" });
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> EditMessage(Guid id, [FromBody] EditMessageRequest request)
        {
            try
            {
                var message = await _messageService.EditAsync(id, request);
                return Ok(message);
            }
            catch (Exception ex) when (ex.Message.Contains("не найдено"))
            {
                return NotFound(new { Message = "Сообщение не найдено" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка редактирования сообщения {MessageId}", id);
                return StatusCode(500, new { Message = "Не удалось отредактировать сообщение" });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteMessage(Guid id)
        {
            try
            {
                await _messageService.DeleteAsync(id);
                return NoContent();
            }
            catch (Exception ex) when (ex.Message.Contains("не найдено"))
            {
                return NotFound(new { Message = "Сообщение не найдено" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка удаления сообщения {MessageId}", id);
                return StatusCode(500, new { Message = "Не удалось удалить сообщение" });
            }
        }

        [HttpPost("chat/{chatId:guid}/mark-read-batch")]
        public async Task<IActionResult> MarkBatchAsRead(Guid chatId, [FromBody] List<Guid> messageIds)
        {
            try
            {
                var userId = GetUserId();
                var unreadCount = await _messageService.MarkMessagesAsReadAsync(chatId, userId, messageIds);
                return Ok(new { unreadCount });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка пакетной отметки сообщений в чате {ChatId}", chatId);
                return StatusCode(500, new { Message = "Не удалось отметить сообщения" });
            }
        }
    }
}