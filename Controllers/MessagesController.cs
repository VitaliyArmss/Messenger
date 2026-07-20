// Controllers/MessagesController.cs
using Messenger.DTO.Chats;
using Messenger.DTO.Messages;
using Messenger.Entities;
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
        public async Task<IActionResult> GetMessages(Guid chatId)
        {
            try
            {
                var messages = await _messageService.GetMessagesAsync(chatId);
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
    }
}