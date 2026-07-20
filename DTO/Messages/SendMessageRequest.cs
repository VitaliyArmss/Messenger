using Messenger.Entities;
namespace Messenger.DTO.Chats
{
    public class SendMessageRequest
    {
        public string Text { get; set; } = "";
        public ICollection<Attachment> Attachments { get; set; }
    }
}
