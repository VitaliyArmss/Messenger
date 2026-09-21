using Messenger.DTO.Chats;
using Messenger.DTO.Users;

namespace Messenger.Services
{
    public interface IChatService
    {
        Task<IEnumerable<ChatResponse>> GetChatsAsync(Guid userId);
        Task<ChatResponse> GetChatAsync(Guid chatId, Guid currentUserId);

        Task<ChatResponse> GetPrivateChatAsync(Guid userId1, Guid userId2);

        Task<IEnumerable<UserResponse>> GetMembersAsync(Guid chatId);

        Task<ChatResponse> CreateAsync(Guid creatorId, CreateChatRequest request);

        Task DeleteAsync(Guid userId, Guid chatId);

        Task AddMemberAsync(Guid chatId, AddChatMemberRequest request, Guid currentUserId);

        Task RemoveMemberAsync(Guid chatId, Guid userId, Guid targetId);

        Task<ChatResponse> UpdateGroupAsync(Guid chatId, UpdateGroupRequest request, Guid currentUserId);
    }
}
