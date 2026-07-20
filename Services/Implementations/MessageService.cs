using Messenger.Data;
using Messenger.DTO.Chats;
using Messenger.DTO.Messages;
using Messenger.Entities;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Services
{
    public class MessageService : IMessageService
    {
        private readonly AppDbContext _db;

        public MessageService(AppDbContext db)
        {
            _db = db;
        }

        /// Получить сообщения чата.
        public async Task<IEnumerable<MessageResponse>> GetMessagesAsync(Guid chatId)
        {

            // 5. Позже добавить пагинацию.

            bool chatExists = await _db.Chats.AnyAsync(x => x.Id == chatId);

            if (!chatExists)
                throw new Exception("Чат не найден.");

            return await _db.Messages
                .Include(x => x.Sender)
                .Where(x => x.ChatId == chatId)
                .OrderBy(x => x.CreatedAt)
                .Select(x => new MessageResponse
                {
                    Id = x.Id,
                    SenderId = x.SenderId,
                    SenderName = x.Sender.Name,
                    Text = x.Text,
                    CreatedAt = x.CreatedAt,
                    IsEdited = x.IsEdited,
                    Attachments = x.Attachments
                })
                .ToListAsync();
        }

        /// Отправить сообщение.
        public async Task<MessageResponse> SendAsync(
            Guid chatId,
            Guid senderId,
            SendMessageRequest request)
        {
            // 6. Позже сохранить вложения.
            // 7. Позже отправить SignalR-событие.

            var chat = await _db.Chats
                .FirstOrDefaultAsync(x => x.Id == chatId);

            if (chat == null)
                throw new Exception("Чат не найден.");

            var sender = await _db.Users
                .FirstOrDefaultAsync(x => x.Id == senderId);

            if (sender == null)
                throw new Exception("Пользователь не найден.");

            bool member = await _db.ChatMembers.AnyAsync(x =>
                x.ChatId == chatId &&
                x.UserId == senderId);

            if (!member)
                throw new Exception("Пользователь не состоит в чате.");

            var message = new Message
            {
                Id = Guid.NewGuid(),
                ChatId = chatId,
                SenderId = senderId,
                Text = request.Text,
                CreatedAt = DateTime.UtcNow,
                IsEdited = false,
                IsDeleted = false,
                Attachments = request.Attachments
            };

            _db.Messages.Add(message);

            await _db.SaveChangesAsync();

            return new MessageResponse
            {
                Id = message.Id,
                SenderId = sender.Id,
                SenderName = sender.Name,
                Text = message.Text,
                CreatedAt = message.CreatedAt,
                IsEdited = false,
                Attachments = message.Attachments
            };
        }

        /// <summary>
        /// Изменить сообщение.
        /// </summary>
        public async Task<MessageResponse> EditAsync(
            Guid messageId,
            EditMessageRequest request)
        {
            // План реализации:
            // 1. Найти сообщение.
            // 2. Проверить права владельца.
            // 3. Изменить текст.
            // 4. Пометить как изменённое.
            // 5. Позже хранить историю изменений.
            // 6. Позже уведомлять участников.

            var message = await _db.Messages
                .Include(x => x.Sender)
                .FirstOrDefaultAsync(x => x.Id == messageId);

            if (message == null)
                throw new Exception("Сообщение не найдено.");

            message.Text = request.Text;
            message.IsEdited = true;

            await _db.SaveChangesAsync();

            return new MessageResponse
            {
                Id = message.Id,
                SenderId = message.SenderId,
                SenderName = message.Sender.Name,
                Text = message.Text,
                CreatedAt = message.CreatedAt,
                IsEdited = true
            };
        }

        /// <summary>
        /// Удалить сообщение.
        /// </summary>
        public async Task DeleteAsync(Guid messageId)
        {
            // План реализации:
            // 1. Найти сообщение.
            // 2. Позже проверить владельца.
            // 3. Сейчас используется soft delete.
            // 4. Позже очищать вложения.
            // 5. Позже уведомлять клиентов.

            var message = await _db.Messages
                .FirstOrDefaultAsync(x => x.Id == messageId);

            if (message == null)
                return;

            message.IsDeleted = true;

            // При желании можно заменить на:
            // _db.Messages.Remove(message);

            await _db.SaveChangesAsync();
        }
    }
}