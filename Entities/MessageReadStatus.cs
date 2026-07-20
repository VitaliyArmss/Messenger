using System.ComponentModel.DataAnnotations.Schema;

namespace Messenger.Entities
{
    public class MessageReadStatus
    {
        public Guid MessageId { get; set; }

        public Guid UserId { get; set; }

        public DateTime ReadAt { get; set; }
    }
}