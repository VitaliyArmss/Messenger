using Messenger.DTO.Chats;
using Messenger.DTO.Messages;

namespace Messenger.Services
{
    public interface IMessageService
    {
        Task<IEnumerable<MessageResponse>> GetMessagesAsync(Guid chatId, int skip = 0, int take = 50);

        Task<IEnumerable<MessageResponse>> GetMessagesAroundAsync(Guid chatId, Guid messageId, int count);

        Task<IEnumerable<MessageResponse>> GetMessagesByDirectionAsync(Guid chatId, Guid anchorMessageId, string direction, int count);

        Task<Guid?> GetFirstUnreadMessageIdAsync(Guid chatId, Guid userId);

        Task<MessageResponse> SendAsync(Guid chatId, Guid senderId, SendMessageRequest request);

        Task<MessageResponse> SendSystemAsync(Guid chatId, SendMessageRequest request);

        Task<MessageResponse> EditAsync(Guid messageId, EditMessageRequest request);

        Task DeleteAsync(Guid messageId);

        Task<int> MarkMessagesAsReadAsync(Guid chatId, Guid userId, List<Guid> messageIds);
    }
}
