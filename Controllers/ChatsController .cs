using Messenger.DTO.Chats;
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

        [HttpGet("by-user/{userId2:guid}")]
        public async Task<IActionResult> GetPrivateChat(Guid userId2)
        {
            try
            {
                var userId1 = GetUserId();
                var chat = await _chatService.GetPrivateChatAsync(userId1, userId2);
                return Ok(chat);
            }
            catch (Exception ex) when (ex.Message.Contains("не найден"))
            {
                return NotFound(new { Message = "Чат не найден" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения личного чата с {ChatId}", userId2);
                return StatusCode(500, new { Message = "Не удалось получить информацию о чате" });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetChat(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var chat = await _chatService.GetChatAsync(id, userId);
                return Ok(chat);
            }
            catch (Exception ex) when (ex.Message.Contains("не найден"))
            {
                return NotFound(new { Message = "Чат не найден" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения чата {ChatId}", id);
                return StatusCode(500, new { Message = "Не удалось получить информацию о чате" });
            }
        }

        [HttpGet("{id:guid}/members")]
        public async Task<IActionResult> GetMembers(Guid id)
        {
            try
            {
                var members = await _chatService.GetMembersAsync(id);
                return Ok(members);
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
                var userId = GetUserId();
                await _chatService.AddMemberAsync(id, request, userId);
                return NoContent();
            }
            catch (Exception ex) when (ex.Message.Contains("не найден"))
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка добавления пользователя в чат {ChatId}", id);
                return StatusCode(500, new { Message = "Не удалось добавить пользователя в чат" });
            }
        }

        [HttpDelete("{id:guid}/members/{targetId:guid}")]
        public async Task<IActionResult> RemoveMember(Guid id, Guid targetId)
        {
            try
            {
                var userId = GetUserId();
                await _chatService.RemoveMemberAsync(id, userId, targetId);
                return NoContent();
            }
            catch (Exception ex) when (ex.Message.Contains("Чат не найден"))
            {
                return NotFound(new { Message = "Чат не найден" });
            }
            catch (Exception ex) when (ex.Message.Contains("не найден"))
            {
                return NotFound(new { Message = "Пользователь не найден в чате" });
            }
            catch (Exception ex) when (ex.Message.Contains("не является владельцем"))
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка удаления пользователя из чата {ChatId}", id);
                return StatusCode(500, new { Message = "Не удалось удалить пользователя из чата" });
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateGroup(Guid id, [FromForm] UpdateGroupRequest request)
        {
            try
            {
                var userId = GetUserId();
                var updated = await _chatService.UpdateGroupAsync(id, request, userId);
                return Ok(updated);
            }
            catch (Exception ex) when (ex.Message.Contains("не найден"))
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления группы {ChatId}", id);
                return StatusCode(500, new { Message = "Не удалось обновить данные группы" });
            }
        }
    }
}