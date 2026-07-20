using Messenger.Entities;
namespace Messenger.DTO.Messages
{
    public class MessageResponse
    {
        public Guid Id { get; set; }

        public Guid SenderId { get; set; }

        public string SenderName { get; set; } = "";

        public string Text { get; set; } = "";

        public DateTime CreatedAt { get; set; }

        public bool IsEdited { get; set; }

        public ICollection<Attachment> Attachments { get; set; } = [];
    }
}
