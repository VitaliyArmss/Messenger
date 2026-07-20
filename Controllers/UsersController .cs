// Controllers/UsersController.cs
using Messenger.DTO.Users;
using Messenger.Entities;
using Messenger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Messenger.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = GetUserId();
                var user = await _userService.GetCurrentUserAsync(userId);
                return Ok(user);
            }
            catch (Exception ex) when (ex.Message.Contains("не найден"))
            {
                return NotFound(new { Message = "Пользователь не найден" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения текущего пользователя");
                return StatusCode(500, new { Message = "Не удалось получить данные пользователя" });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var user = await _userService.GetByIdAsync(id);
                if (user == null)
                    return NotFound(new { Message = "Пользователь не найден" });
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения пользователя {UserId}", id);
                return StatusCode(500, new { Message = "Не удалось получить данные пользователя" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] string? query,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var users = await _userService.SearchAsync(query, page, pageSize);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка поиска пользователей");
                return StatusCode(500, new { Message = "Не удалось выполнить поиск" });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateUserRequest request)
        {
            try
            {
                var userId = GetUserId();
                var user = await _userService.UpdateAsync(userId, request);
                return Ok(user);
            }
            catch (Exception ex) when (ex.Message.Contains("не найден"))
            {
                return NotFound(new { Message = "Пользователь не найден" });
            }
            catch (Exception ex) when (ex.Message.Contains("уже занят"))
            {
                return Conflict(new { Message = "Данный username уже занят" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления пользователя");
                return StatusCode(500, new { Message = "Не удалось обновить данные пользователя" });
            }
        }
    }
}