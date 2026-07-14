namespace Messenger.Entities
{
    public class Chat
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = "";

        public bool IsGroup { get; set; }

        public DateTime CreatedAt { get; set; }

        public ICollection<ChatMember> Members { get; set; } = [];

        public ICollection<Message> Messages { get; set; } = [];
    }
}
