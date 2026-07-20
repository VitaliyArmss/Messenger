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
                Name = request.Name,
                UserName = request.UserName,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow
            };

            // Захешировать пароль
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            // Сохранить в БД
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var refreshToken = await CreateRefreshTokenAsync(user);

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
                throw new UnauthorizedAccessException("Пользователь не найден");
            }

            // Проверить пароль
            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);

            if (result == PasswordVerificationResult.Failed)
            {
                throw new UnauthorizedAccessException("Неверный пароль");
            }

            var refreshToken = await CreateRefreshTokenAsync(user);

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
            var storedToken = await _db.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (storedToken == null)
                throw new UnauthorizedAccessException("Неверный refresh-токен");

            if (storedToken.IsRevoked)
                throw new UnauthorizedAccessException("Refresh-токен отозван");

            if (storedToken.ExpiresAt < DateTime.UtcNow)
                throw new UnauthorizedAccessException("Refresh-токен истёк");

            var user = storedToken.User;

             _db.RefreshTokens.Remove(storedToken);

            var newRefreshToken = await CreateRefreshTokenAsync(user);

            var accessToken = _jwtService.GenerateToken(user);

            return new AuthResponse
            {
                AccessToken = accessToken.Token,
                RefreshToken = newRefreshToken.Token,
                ExpiresAt = accessToken.ExpiresAt
            };
        }

        public async Task LogoutAsync(Guid userId)
        {
            var tokens = await _db.RefreshTokens
                        .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                        .ToListAsync();

            _db.RefreshTokens.RemoveRange(tokens);
            await _db.SaveChangesAsync();
        }

        private async Task<RefreshToken> CreateRefreshTokenAsync(User user)
        {
            var token = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = _jwtService.GenerateRefreshToken(),
                UserId = user.Id,
                User = user,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            _db.RefreshTokens.Add(token);
            await _db.SaveChangesAsync();
            return token;
        }
    }
}