using Messenger.DTO.Attachments;

namespace Messenger.Services
{
    public interface IAttachmentService
    {
        Task<AttachmentResponse> UploadAsync(IFormFile file);

        Task<(Stream Stream, string ContentType, string FileName)> DownloadAsync(Guid id);

        Task DeleteAsync(Guid attachmentId);
    }
}
