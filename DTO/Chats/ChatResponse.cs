namespace Messenger.DTO.Chats
{
    public class ChatResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = "";

        public bool IsGroup { get; set; }

        public int MembersCount { get; set; }

        public DateTime CreatedAt { get; set; }

        public string LastMessageText { get; set; }

        public DateTime? LastMessageAt { get; set; }
    }
}
