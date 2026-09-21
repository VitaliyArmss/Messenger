namespace Messenger.Entities
{
    public enum FileType
    {
        File, Media, Sound
    }
    public class Attachment
    {
        public Guid Id { get; set; }

        public Guid? MessageId { get; set; }

        public Guid? ChatId { get; set; }

        public string Url { get; set; } = "";

        public string FileName { get; set; } = "";

        public string ContentType { get; set; } = "";

        public long Size { get; set; }

        public bool IsAvatar { get; set; } = false;

        public FileType Type { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
