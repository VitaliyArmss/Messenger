using Messenger;
using Messenger.Data;
using Messenger.DTO.Attachments;
using Messenger.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.AccessControl;

namespace Messenger.Services
{
    public class AttachmentService : IAttachmentService
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _environment;
        private readonly FileWorker _fileWorker;

        public AttachmentService(
            AppDbContext db,
            IWebHostEnvironment environment,
            FileWorker fileWorker)
        {
            _db = db;
            _environment = environment;
            _fileWorker = fileWorker;
        }

        /// <summary>
        /// Загрузка файла.
        /// </summary>
        public async Task<AttachmentResponse> UploadAsync(IFormFile file)
        {
            // Позже:
            // - S3 / MinIO
            // - Azure Blob
            // - Антивирус
            // - Генерация preview

            if (file == null || file.Length == 0)
                throw new Exception("Файл отсутствует.");

            string uploadsPath = Path.Combine(
                _environment.ContentRootPath,
                "Uploads");

            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            string extension = Path.GetExtension(file.FileName);

            string fileName = $"{Guid.NewGuid()}{extension}";

            string fullPath = Path.Combine(
                uploadsPath,
                fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var attachment = new Attachment
            {
                Id = Guid.NewGuid(),

                // TODO:
                // После появления сообщений
                // сюда передавать MessageId.
                MessageId = null, //заглушка

                Message = null, //заглушка

                FileName = file.FileName,

                Url = "/Uploads/" + fileName,

                ContentType = file.ContentType,

                Size = file.Length
            };

            _db.Attachments.Add(attachment);

            await _db.SaveChangesAsync();

            await _fileWorker.UploadFileAsync(file);
            
            return new AttachmentResponse
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                Url = attachment.Url,
                Size = attachment.Size,
                ContentType = attachment.ContentType
            };
        }

        public async Task<(Stream Stream, string ContentType, string FileName)> DownloadAsync(Guid id)
        {
            // 1. Ищем запись в БД
            var attachment = await _db.Attachments
                .FirstOrDefaultAsync(x => x.Id == id);

            if (attachment == null)
                throw new Exception("Файл не найден.");

            return await _fileWorker.DownloadFileAsync(attachment.FileName);
        }

        /// Удаление вложения.
        public async Task DeleteAsync(Guid attachmentId)
        {
            // План реализации:
            // 1. Найти Attachment.
            // 2. Проверить права.
            // 3. Удалить файл с диска.
            // 4. Удалить запись из БД.
            // 5. Позже добавить SoftDelete.
            
            var attachment = await _db.Attachments.FirstOrDefaultAsync(x => x.Id == attachmentId);

            if (attachment == null)
                return;

            string path = Path.Combine(
                _environment.ContentRootPath,
                attachment.Url.TrimStart('/'));

            //добавить S3
            if (File.Exists(path))
                File.Delete(path);

            _db.Attachments.Remove(attachment);

            await _db.SaveChangesAsync();
            
            await Task.CompletedTask;
        }
    }
}