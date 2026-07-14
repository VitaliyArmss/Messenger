using Messenger.DTO.Attachments;

namespace Messenger.Services
{
    public interface IAttachmentService
    {
        Task<AttachmentResponse> UploadAsync(IFormFile file);

        Task DeleteAsync(Guid attachmentId);
    }
}
