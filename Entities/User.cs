namespace Messenger.Entities
{
    public class User
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        public string UserName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public string? AvatarUrl { get; set; }

        public DateTime CreatedAt { get; set; }

        public ICollection<ChatMember> Chats { get; set; } = [];
    }
}
