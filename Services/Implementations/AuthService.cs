using Messenger.Data;
using Messenger.DTO;
using Messenger.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly IJwtService _jwtService;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AuthService(AppDbContext db, IJwtService jwtService)
        {
            _db = db;
            _jwtService = jwtService;
        }
        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // TODO:
            // Проверить существование пользователя
            bool exist = await _db.Users.AnyAsync(u => u.Email == request.Email);
            if (exist)
            {
                throw new Exception("Пользователь уже существует");
            }

            // Создать User
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow
            };

            // Захешировать пароль
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            // Сохранить в БД
            _db.Users.Add(user);
            await _db.SaveChangesAsync();


            // Сгенерировать JWT
            JwtResult accessToken = _jwtService.GenerateToken(user);

            return new AuthResponse
            {
                AccessToken = accessToken.Token,
                RefreshToken = Guid.NewGuid().ToString(), // пока заглушка
                ExpiresAt = accessToken.ExpiresAt
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            // TODO:
            // Найти пользователя
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                throw new Exception("Пользователь не найден");
            }

            // Проверить пароль
            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);

            if (result == PasswordVerificationResult.Failed)
            {
                throw new Exception("Неверный пароль");
            }

            // Сгенерировать JWT
            JwtResult accessToken = _jwtService.GenerateToken(user);

            return new AuthResponse
            {
                AccessToken = accessToken.Token,
                RefreshToken = Guid.NewGuid().ToString(), // пока заглушка
                ExpiresAt = accessToken.ExpiresAt
            };

        }

        public async Task<AuthResponse> RefreshAsync(RefreshRequest request)
        {
            // TODO:
            // Проверить Refresh Token
            // Выдать новую пару токенов

            return await Task.FromResult(new AuthResponse
            {
                AccessToken = Guid.NewGuid().ToString(),
                RefreshToken = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });
        }

        public async Task LogoutAsync(Guid userId)
        {
            // TODO:
            // Удалить Refresh Token из БД
            // или добавить JWT в blacklist

            await Task.CompletedTask;
        }
    }
}