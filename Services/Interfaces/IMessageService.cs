using Messenger.DTO.Chats;
using Messenger.DTO.Messages;

namespace Messenger.Services
{
    public interface IMessageService
    {
        Task<IEnumerable<MessageResponse>> GetMessagesAsync(Guid chatId);

        Task<MessageResponse> SendAsync(Guid chatId, Guid senderId, SendMessageRequest request);

        Task<MessageResponse> EditAsync(Guid messageId, EditMessageRequest request);

        Task DeleteAsync(Guid messageId);
    }
}
