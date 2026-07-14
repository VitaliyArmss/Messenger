namespace Messenger.DTO.Messages
{
    public class MessageResponse
    {
        public Guid Id { get; set; }

        public Guid SenderId { get; set; }

        public string SenderUsername { get; set; } = "";

        public string Text { get; set; } = "";

        public DateTime CreatedAt { get; set; }

        public bool IsEdited { get; set; }
    }
}
