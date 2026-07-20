using Messenger.Entities;

namespace Messenger.Services;

public interface IJwtService
{
    JwtResult GenerateToken(User user);

    string GenerateRefreshToken();
}