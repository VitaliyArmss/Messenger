namespace Messenger.DTO.Chats
{
    public class CreateChatRequest
    {
        public string Name { get; set; } = "";

        public bool IsGroup { get; set; }
    }
}
