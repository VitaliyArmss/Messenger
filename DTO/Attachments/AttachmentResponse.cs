using Messenger.Entities;

namespace Messenger.DTO.Attachments
{
    public class AttachmentResponse
    {
        public Guid Id { get; set; }

        public string FileName { get; set; } = "";

        public string Url { get; set; } = "";

        public long Size { get; set; }

        public string ContentType { get; set; } = "";

        public FileType Type { get; set; }
    }
}
