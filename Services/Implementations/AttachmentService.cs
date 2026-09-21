using Messenger.Data;
using Messenger.DTO.Attachments;
using Messenger.Entities;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Services
{
    public class AttachmentService : IAttachmentService
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _environment;
        private readonly FileWorker _fileWorker;

        public AttachmentService(
            AppDbContext db,
            IWebHostEnvironment environment,
            FileWorker fileWorker,
            IConfiguration configuration)
        {
            _db = db;
            _environment = environment;
            _fileWorker = fileWorker;
            _configuration = configuration;
        }

        /// Удаление вложения.
        public async Task DeleteAsync(Guid attachmentId)
        {
            var attachment = await _db.Attachments.FirstOrDefaultAsync(x => x.Id == attachmentId);

            if (attachment == null)
                return;

            string path = Path.Combine(
                _environment.ContentRootPath,
                attachment.Url.TrimStart('/'));

            //if (File.Exists(path))
            //    File.Delete(path);

            _db.Attachments.Remove(attachment);

            await _db.SaveChangesAsync();

            await Task.CompletedTask;
        }

        public async Task<(Stream Stream, string ContentType, string FileName)> DownloadAsync(Guid id, Guid userId)
        {
            // 1. Находим вложение
            var attachment = await _db.Attachments
                .FirstOrDefaultAsync(a => a.Id == id);
            if (attachment == null)
                throw new Exception("Файл не найден");

            // 2. Проверка доступа
            bool hasAccess = false;

            if (attachment.ChatId.HasValue)
            {
                // Если есть ChatId – проверяем, является ли пользователь участником чата
                hasAccess = await _db.ChatMembers
                    .AnyAsync(cm => cm.ChatId == attachment.ChatId.Value && cm.UserId == userId);
            }
            else
            {
                // Если ChatId отсутствует – разрешаем доступ только если это аватар
                hasAccess = attachment.IsAvatar;
            }

            if (!hasAccess)
                throw new UnauthorizedAccessException("У вас нет доступа к этому файлу");

            // 3. Скачивание
            return await _fileWorker.DownloadFileAsync(attachment.FileName);
        }

        public async Task<IEnumerable<AttachmentResponse>> GetAttachmentsByChatAsync(
                    Guid chatId,
                    FileType? type,
                    int skip,
                    int take)
        {
            var query = from a in _db.Attachments
                        join m in _db.Messages on a.MessageId equals m.Id
                        where m.ChatId == chatId
                        select a;

            if (type.HasValue)
                query = query.Where(a => a.Type == type.Value);

            var attachments = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Select(a => new AttachmentResponse
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    Url = a.Url,
                    Size = a.Size,
                    ContentType = a.ContentType,
                    Type = a.Type
                })
                .ToListAsync();

            return attachments;
        }

        /// <summary>
        /// Загрузка файла.
        /// </summary>
        public async Task<AttachmentResponse> UploadAsync(IFormFile file, bool IsAvatar = false, Guid? chatId = null)
        {
            if (file == null || file.Length == 0)
                throw new Exception("Файл отсутствует.");

            string extension = Path.GetExtension(file.FileName);
            string fileName = $"{Guid.NewGuid()}{extension}";

            // 1. Загружаем файл в Minio
            await _fileWorker.UploadFileAsync(file, fileName);

            // 2. Создаём запись в БД (пока без Url, чтобы получить Id)
            var attachment = new Attachment
            {
                Id = Guid.NewGuid(),
                MessageId = null,
                ChatId = chatId,
                IsAvatar = IsAvatar,
                FileName = fileName,
                ContentType = file.ContentType,
                Size = file.Length,
                CreatedAt = DateTime.UtcNow,
                Type = DetermineFileType(file.ContentType)
            };

            _db.Attachments.Add(attachment);
            await _db.SaveChangesAsync(); // теперь у нас есть Id

            // 3. Формируем URL к API
            var baseUrl = _configuration["BaseUrl"]; // например, "https://localhost:7039"
            var attachmentUrl = $"{baseUrl}/api/attachments/{attachment.Id}";

            // 4. Обновляем запись в БД с URL
            attachment.Url = attachmentUrl;
            await _db.SaveChangesAsync();

            return new AttachmentResponse
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                Url = attachment.Url,
                Size = attachment.Size,
                ContentType = attachment.ContentType,
                Type = attachment.Type
            };
        }

        private FileType DetermineFileType(string contentType)
        {
            if (contentType.StartsWith("image/") || contentType.StartsWith("video/"))
                return FileType.Media;
            if (contentType.StartsWith("audio/"))
                return FileType.Sound;
            return FileType.File;
        }
    }
}