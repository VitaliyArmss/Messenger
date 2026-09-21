using System.Net.Mail;

namespace Messenger.Entities
{
    public class Message
    {
        public Guid Id { get; set; }

        public Guid ChatId { get; set; }

        public Chat Chat { get; set; } = null!;

        public Guid? SenderId { get; set; }

        public User Sender { get; set; } = null!;

        public string Text { get; set; } = "";

        public DateTime CreatedAt { get; set; }

        public bool IsEdited { get; set; }

        public bool IsDeleted { get; set; }

        public bool IsSystem { get; set; }
        public ICollection<Attachment> Attachments { get; set; } = [];
    }
}
