using Messenger.Data;
using Messenger.DTO.Chats;
using Messenger.Entities;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Services
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _db;

        public ChatService(AppDbContext db)
        {
            _db = db;
        }

        /// Получить все чаты пользователя.
        public async Task<IEnumerable<ChatResponse>> GetChatsAsync(Guid userId)
        {
            var query = from cm in _db.ChatMembers
                        where cm.UserId == userId
                        join c in _db.Chats on cm.ChatId equals c.Id
                        let lastMsg = _db.Messages
                            .Where(m => m.ChatId == c.Id && !m.IsDeleted)
                            .OrderByDescending(m => m.CreatedAt)
                            .FirstOrDefault()
                        select new ChatResponse
                        {
                            Id = c.Id,
                            Name = c.Name,
                            IsGroup = c.IsGroup,
                            MembersCount = c.Members.Count,
                            CreatedAt = c.CreatedAt,
                            LastMessageText = lastMsg != null ? lastMsg.Text : null,
                            LastMessageAt = lastMsg != null ? lastMsg.CreatedAt : (DateTime?)null
                        };

            var chats = await query.ToListAsync();

            // Сортировка по последнему сообщению (null - в конец)
            return chats
                .OrderByDescending(x => x.LastMessageAt ?? x.CreatedAt)
                .ToList();
        }

        /// Получить информацию о чате.
        public async Task<ChatResponse> GetChatAsync(Guid chatId)
        {
            var chat = await _db.Chats
                .Include(x => x.Members)
                .FirstOrDefaultAsync(x => x.Id == chatId);

            var lastMsg = chat?.Messages?.OrderByDescending(m => m.CreatedAt).FirstOrDefault();

            if (chat == null)
                throw new Exception("Чат не найден.");

            return new ChatResponse
            {
                Id = chat.Id,
                Name = chat.Name,
                IsGroup = chat.IsGroup,
                MembersCount = chat.Members.Count,
                CreatedAt = chat.CreatedAt,
                LastMessageText = lastMsg.Text,
                LastMessageAt = lastMsg.CreatedAt
            };
        }

        /// Создать чат.
        public async Task<ChatResponse> CreateAsync(Guid creatorId, CreateChatRequest request)
        {
            // 5. Позже отправлять уведомления через SignalR.

            var creator = await _db.Users.FirstOrDefaultAsync(x => x.Id == creatorId);

            if (creator == null)
                throw new Exception("Создатель не найден.");

            var chat = new Chat
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                IsGroup = request.IsGroup,
                OwnerId = creatorId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Chats.Add(chat);

            _db.ChatMembers.Add(new ChatMember
            {
                ChatId = chat.Id,
                UserId = creatorId,
                JoinedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return new ChatResponse
            {
                Id = chat.Id,
                Name = chat.Name,
                IsGroup = chat.IsGroup,
                MembersCount = 1,
                CreatedAt = chat.CreatedAt
            };
        }

        /// Удалить чат.
        public async Task DeleteAsync(Guid userId, Guid chatId)
        {
            var chat = await _db.Chats
                .FirstOrDefaultAsync(x => x.Id == chatId);

            if (chat == null)
                throw new Exception("Чат не найден.");

            if (chat.OwnerId != userId)
            {
                throw new Exception("Пользователь не является владельцем");
            }

            await _db.Messages.Where(x => x.ChatId == chat.Id).ExecuteDeleteAsync();
            
            _db.Chats.Remove(chat);

            await _db.SaveChangesAsync();
        }

        /// Добавить пользователя в чат.
        public async Task AddMemberAsync(Guid chatId, AddChatMemberRequest request)
        {
            // 5. Позже отправлять событие через SignalR.

            bool chatExists = await _db.Chats.AnyAsync(x => x.Id == chatId);

            if (!chatExists)
                throw new Exception("Чат не найден.");

            bool userExists = await _db.Users.AnyAsync(x => x.Id == request.UserId);

            if (!userExists)
                throw new Exception("Пользователь не найден.");

            bool already = await _db.ChatMembers.AnyAsync(x =>
                x.ChatId == chatId &&
                x.UserId == request.UserId);

            if (already)
                return;

            _db.ChatMembers.Add(new ChatMember
            {
                ChatId = chatId,
                UserId = request.UserId,
                JoinedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        /// Удалить пользователя из чата.
        public async Task RemoveMemberAsync(Guid chatId, Guid userId)
        {
            // 5. Позже уведомить остальных участников.

            var member = await _db.ChatMembers
                .FirstOrDefaultAsync(x =>
                    x.ChatId == chatId &&
                    x.UserId == userId);

            if (member == null)
                return;

            _db.ChatMembers.Remove(member);

            await _db.SaveChangesAsync();

            bool hasMembers = await _db.ChatMembers
                .AnyAsync(x => x.ChatId == chatId);

            if (!hasMembers)
            {
                var chat = await _db.Chats
                    .FirstOrDefaultAsync(x => x.Id == chatId);

                if (chat != null)
                {
                    await _db.Messages.Where(x => x.ChatId == chat.Id).ExecuteDeleteAsync();
                    _db.Chats.Remove(chat);
                    await _db.SaveChangesAsync();
                }
            }
        }
    }
}