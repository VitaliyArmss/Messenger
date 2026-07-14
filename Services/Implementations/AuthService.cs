using Messenger.DTO;

namespace Messenger.Services
{
    public class AuthService : IAuthService
    {
        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // TODO:
            // Проверить существование пользователя
            // Захешировать пароль
            // Создать User
            // Сохранить в БД
            // Сгенерировать JWT

            return await Task.FromResult(new AuthResponse
            {
                AccessToken = Guid.NewGuid().ToString(),
                RefreshToken = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            // TODO:
            // Найти пользователя
            // Проверить пароль
            // Сгенерировать JWT

            return await Task.FromResult(new AuthResponse
            {
                AccessToken = Guid.NewGuid().ToString(),
                RefreshToken = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });
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