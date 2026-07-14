using Messenger.DTO.Users;

namespace Messenger.Services
{
    public interface IUserService
    {
        Task<UserResponse> GetCurrentUserAsync(Guid userId);

        Task<UserResponse?> GetByIdAsync(Guid id);

        Task<IEnumerable<UserResponse>> SearchAsync(string? query);

        Task<UserResponse> UpdateAsync(Guid userId, UpdateUserRequest request);
    }
}
