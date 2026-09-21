using Messenger.DTO.Attachments;
using Messenger.Entities;

namespace Messenger.Services
{
    public interface IAttachmentService
    {
        Task<AttachmentResponse> UploadAsync(IFormFile file, bool IsAvatar = false, Guid? chatId = null);

        Task<(Stream Stream, string ContentType, string FileName)> DownloadAsync(Guid id, Guid userId);

        Task DeleteAsync(Guid attachmentId);

        Task<IEnumerable<AttachmentResponse>> GetAttachmentsByChatAsync(
        Guid chatId,
        FileType? typ,
        int skip,
        int take);
    }
}
