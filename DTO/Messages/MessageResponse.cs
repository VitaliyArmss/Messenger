using Messenger.DTO.Users;
using Messenger.Entities;
namespace Messenger.DTO.Messages
{
    public class MessageResponse
    {
        public Guid Id { get; set; }

        public Guid? SenderId { get; set; }

        public string? SenderName { get; set; } = "";

        public string? SenderAvatarUrl { get; set; } = null;

        public string Text { get; set; } = "";

        public DateTime CreatedAt { get; set; }

        public bool IsEdited { get; set; }

        public bool IsSystem { get; set; }

        public List<UserReadInfo> ReadBy { get; set; } = new();

        public ICollection<Attachment> Attachments { get; set; } = [];
    }
}
