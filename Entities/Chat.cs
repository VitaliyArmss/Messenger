namespace Messenger.Entities
{
    public class Chat
    {
        public Guid Id { get; set; }

        public string? Name { get; set; } = "";

        public bool IsGroup { get; set; }

        public bool IsInitialised { get; set; } = false;

        public Guid? OwnerId { get; set; }

        public User Owner { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public ICollection<ChatMember> Members { get; set; } = [];

        public ICollection<Message> Messages { get; set; } = [];

        public string? AvatarUrl { get; set; }
    }
}