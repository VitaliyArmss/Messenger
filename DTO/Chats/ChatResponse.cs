using Messenger.Entities;

namespace Messenger.DTO.Chats
{
    public class ChatResponse
    {
        public Guid Id { get; set; }

        public Guid? UserId { get; set; }

        public string Name { get; set; } = "";

        public string? UserName { get; set; }

        public bool IsGroup { get; set; }

        public Guid? OwnerId { get; set; }

        public int MembersCount { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? LastMessageText { get; set; }

        public Guid? LastMessageSenderId { get; set; }

        public string? LastMessageSenderName { get; set; }

        public DateTime? LastMessageAt { get; set; }

        public bool? HasLastMessageFile { get; set; }

        public string? AvatarUrl { get; set; }

        public int UnreadMessagesCount { get; set; }
    }
}