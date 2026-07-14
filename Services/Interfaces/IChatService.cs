using Messenger.DTO.Chats;

namespace Messenger.Services
{
    public interface IChatService
    {
        Task<IEnumerable<ChatResponse>> GetChatsAsync(Guid userId);

        Task<ChatResponse> GetChatAsync(Guid chatId);

        Task<ChatResponse> CreateAsync(Guid creatorId, CreateChatRequest request);

        Task DeleteAsync(Guid chatId);

        Task AddMemberAsync(Guid chatId, AddChatMemberRequest request);

        Task RemoveMemberAsync(Guid chatId, Guid userId);
    }
}
