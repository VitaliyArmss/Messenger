// Controllers/ChatsController.cs
using Messenger.DTO.Chats;
using Messenger.Entities;
using Messenger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Messenger.Controllers
{
    [ApiController]
    [Route("api/chats")]
    [Authorize]
    public class ChatsController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatsController> _logger;

        public ChatsController(IChatService chatService, ILogger<ChatsController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        [HttpGet]
        public async Task<IActionResult> GetChats()
        {
            try
            {
                var userId = GetUserId();
                var chats = await _chatService.GetChatsAsync(userId);
                return Ok(chats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения чатов");
                return StatusCode(500, new { Message = "Не удалось получить список чатов" });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetChat(Guid id)
        {
            try
            {
                var chat = await _chatService.GetChatAsync(id);
                return Ok(chat);
            }
            catch (Exception ex) when (ex.Message.Contains("не найден"))
            {
                return NotFound(new { Message = "Чат не найден" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения чата {ChatId}", id);
                return StatusCode(500, new { Message = "Не удалось получить информацию о чате" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateChat([FromBody] CreateChatRequest request)
        {
            try
            {
                var userId = GetUserId();
                var chat = await _chatService.CreateAsync(userId, request);
                return CreatedAtAction(nameof(GetChat), new { id = chat.Id }, chat);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка создания чата");
                return StatusCode(500, new { Message = "Не удалось создать чат" });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteChat(Guid id)
        {
            try
            {
                var userId = GetUserId();
                await _chatService.DeleteAsync(userId, id);
                return NoContent();
            }
            catch (Exception ex) when (ex.Message.Contains("не найден"))
            {
                return NotFound(new { Message = "Чат не найден" });
            }
            catch (Exception ex) when (ex.Message.Contains("не является владельцем"))
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка удаления чата {ChatId}", id);
                return StatusCode(500, new { Message = "Не удалось удалить чат" });
            }
        }

        [HttpPost("{id:guid}/members")]
        public async Task<IActionResult> AddMember(Guid id, [FromBody] AddChatMemberRequest request)
        {
            try
            {
                await _chatService.AddMemberAsync(id, request);
                return NoContent();
            }
            catch (Exception ex) when (ex.Message.Contains("не найден"))
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex) when (ex.Message.Contains("уже состоит"))
            {
                return Conflict(new { Message = "Пользователь уже состоит в чате" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка добавления пользователя в чат {ChatId}", id);
                return StatusCode(500, new { Message = "Не удалось добавить пользователя в чат" });
            }
        }

        [HttpDelete("{id:guid}/members/{userId:guid}")]
        public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
        {
            try
            {
                await _chatService.RemoveMemberAsync(id, userId);
                return NoContent();
            }
            catch (Exception ex) when (ex.Message.Contains("не найден"))
            {
                return NotFound(new { Message = "Пользователь не найден в чате" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка удаления пользователя из чата {ChatId}", id);
                return StatusCode(500, new { Message = "Не удалось удалить пользователя из чата" });
            }
        }
    }
}