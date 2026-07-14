namespace Messenger.Entities
{
    public class Attachment
    {
        public Guid Id { get; set; }

        public Guid MessageId { get; set; }

        public Message Message { get; set; } = null!;

        public string Url { get; set; } = "";

        public string FileName { get; set; } = "";

        public string ContentType { get; set; } = "";

        public long Size { get; set; }
    }
}
