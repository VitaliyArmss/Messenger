namespace Messenger.Entities
{
    public class ChatMember
    {
        public Guid UserId { get; set; }

        public User User { get; set; } = null!;

        public Guid ChatId { get; set; }

        public Chat Chat { get; set; } = null!;

        public DateTime JoinedAt { get; set; }
    }
}
