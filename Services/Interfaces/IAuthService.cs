using Messenger.DTO;

namespace Messenger.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);

        Task<AuthResponse> LoginAsync(LoginRequest request);

        Task<AuthResponse> RefreshAsync(RefreshRequest request);

        Task LogoutAsync(Guid userId);
    }
}
