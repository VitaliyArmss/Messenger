using Messenger.Entities;

namespace Messenger.DTO.Chats
{
    public class CreateChatRequest
    {
        public string? Name { get; set; } = "";

        public string? AvatarUrl { get; set; } = null;

        public bool IsGroup { get; set; } = false;

        public List<Guid> MemberIds { get; set; } = null;
    }
}