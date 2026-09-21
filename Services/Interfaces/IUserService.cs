using Messenger.DTO.Users;

namespace Messenger.Services
{
    public interface IUserService
    {
        Task<UserResponse> GetCurrentUserAsync(Guid userId);

        Task<UserResponse?> GetByIdAsync(Guid id);

        Task<IEnumerable<UserResponse>> GetContactsAsync(Guid userId);

        Task<IEnumerable<UserResponse>> SearchAsync(string? query, int page, int pagesize, Guid currentUserId);

        Task<UserResponse> UpdateAsync(Guid userId, UpdateUserRequest request);
    }
}