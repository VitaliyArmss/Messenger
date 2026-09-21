using Messenger.DTO;
using Messenger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Messenger.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);
                return Ok(result);
            }
            catch (Exception ex) when (ex.Message.Contains("уже существует"))
            {
                _logger.LogWarning("Попытка регистрации с существующим email: {Email}", request.Email);
                return Conflict(new { Message = "Пользователь с таким email уже существует" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка регистрации пользователя {Email}", request.Email);
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                return Ok(result);
            }
            catch (Exception ex) when (ex.Message.Contains("не найден"))
            {
                _logger.LogWarning("Попытка входа с несуществующим email: {Email}", request.Email);
                return NotFound(new { Message = "Неверный email или пароль" });
            }
            catch (Exception ex) when (ex.Message.Contains("Неверный пароль"))
            {
                _logger.LogWarning("Неудачная попытка входа для {Email}", request.Email);
                return Unauthorized(new { Message = "Неверный email или пароль" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при входе для {Email}", request.Email);
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            try
            {
                var result = await _authService.RefreshAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления токена");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { Message = "Не удалось определить пользователя" });

                var userId = Guid.Parse(userIdClaim.Value);
                await _authService.LogoutAsync(userId);
                return Ok(new { Message = "Вы успешно вышли из системы" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выходе из системы");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("check-email")]
        public async Task<IActionResult> CheckEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "Email is required" });

            var isUnique = await _authService.IsEmailUniqueAsync(email);
            return Ok(new { isUnique });
        }

        [HttpGet("check-username")]
        public async Task<IActionResult> CheckUsername([FromQuery] string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest(new { message = "Username is required" });

            var isUnique = await _authService.IsUsernameUniqueAsync(username);
            return Ok(new { isUnique });
        }
    }
}