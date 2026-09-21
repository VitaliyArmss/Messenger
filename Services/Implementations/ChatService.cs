using Messenger.Data;
using Messenger.DTO.Chats;
using Messenger.DTO.Users;
using Messenger.Entities;
using Messenger.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Net.WebSockets;

namespace Messenger.Services
{
    public class ChatService : IChatService
    {
        private readonly IAttachmentService _attachmentService;
        private readonly IHubContext<ChatHub> _chatHubContext;
        private readonly AppDbContext _db;
        private readonly IMessageService _messageService;
        private readonly IHubContext<NotificationHub> _notificationHubContext;

        public ChatService(AppDbContext db, IMessageService messageService, IHubContext<ChatHub> chatHubContext, IAttachmentService attachmentService, IHubContext<NotificationHub> notificationHubContext)
        {
            _db = db;
            _messageService = messageService;
            _chatHubContext = chatHubContext;
            _attachmentService = attachmentService;
            _notificationHubContext = notificationHubContext;
        }

        /// Добавить пользователя в чат.
        public async Task AddMemberAsync(Guid chatId, AddChatMemberRequest request, Guid currentUserId)
        {
            var chat = await _db.Chats.FirstOrDefaultAsync(x => x.Id == chatId);
            if (chat == null)
                throw new Exception("Чат не найден.");

            // Разрешаем добавление только в групповые чаты
            if (!chat.IsGroup)
                throw new Exception("В личный чат нельзя добавлять участников.");

            // Проверяем, что текущий пользователь является участником чата
            bool isMember = await _db.ChatMembers.AnyAsync(x => x.ChatId == chatId && x.UserId == currentUserId);
            if (!isMember)
                throw new UnauthorizedAccessException("Вы не участник этого чата.");

            var userToAdd = await _db.Users.FindAsync(request.UserId);
            if (userToAdd == null)
                throw new Exception("Пользователь не найден.");

            bool already = await _db.ChatMembers.AnyAsync(x => x.ChatId == chatId && x.UserId == request.UserId);
            if (already)
                return;

            _db.ChatMembers.Add(new ChatMember
            {
                ChatId = chatId,
                UserId = request.UserId,
                JoinedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            // 1. Отправляем системное сообщение
            var actor = await _db.Users.FindAsync(currentUserId);
            string systemMessage = $"{actor?.Name ?? "Кто-то"} добавил {userToAdd.Name} в чат";
            await _messageService.SendSystemAsync(chatId, new SendMessageRequest
            {
                Text = systemMessage,
                IsSystem = true
            });

            // 2. Обновляем список участников через SignalR
            var members = await GetMembersAsync(chatId);
            await _chatHubContext.Clients.Group(chatId.ToString())
                .SendAsync("GroupMembersUpdated", members);

            // Отправляем уведомление об обновлении списка чатов всем участникам чата
            var allMemberIds = await _db.ChatMembers
                .Where(cm => cm.ChatId == chatId)
                .Select(cm => cm.UserId)
                .ToListAsync();
            foreach (var memberId in allMemberIds)
            {
                await _notificationHubContext.Clients.User(memberId.ToString())
                    .SendAsync("ChatsUpdated", new { ChatId = chatId });
            }

            // 3. Обновляем информацию о чате (количество участников, возможно, имя/аватар)
            var updatedChat = await GetChatAsync(chatId, currentUserId);
            await _chatHubContext.Clients.Group(chatId.ToString())
                .SendAsync("GroupUpdated", updatedChat);
        }

        /// Создать чат.
        /// <summary>
        /// Создаёт чат в неактивном состоянии.
        /// Чат будет инициализирован при первом сообщении.
        /// </summary>
        public async Task<ChatResponse> CreateAsync(
            Guid creatorId,
            CreateChatRequest request)
        {
            var requestedMemberIds = request.MemberIds
                .Distinct()
                .ToList();

            var existingUserIds = await _db.Users
                .Where(x => requestedMemberIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();

            var missingUserIds = requestedMemberIds
                .Except(existingUserIds)
                .ToList();

            if (missingUserIds.Any())
                throw new Exception("Один или несколько участников не найдены.");

            var creator = await _db.Users
                .FirstOrDefaultAsync(x => x.Id == creatorId);

            if (creator == null)
                throw new Exception("Создатель не найден.");

            var chat = new Chat
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                IsGroup = request.IsGroup,
                OwnerId = creatorId,
                CreatedAt = DateTime.UtcNow,

                IsInitialised = request.IsGroup ? true : false
            };

            // Добавляем участников
            foreach (var userId in existingUserIds)
            {
                chat.Members.Add(new ChatMember
                {
                    ChatId = chat.Id,
                    UserId = userId,
                    JoinedAt = DateTime.UtcNow
                });
            }

            // На всякий случай гарантируем, что создатель является участником
            if (!chat.Members.Any(x => x.UserId == creatorId))
            {
                chat.Members.Add(new ChatMember
                {
                    ChatId = chat.Id,
                    UserId = creatorId,
                    JoinedAt = DateTime.UtcNow
                });
            }

            _db.Chats.Add(chat);

            await _db.SaveChangesAsync();

            if (chat.IsGroup)
            {
                // Создаём системное сообщение
                await _messageService.SendSystemAsync(
                    chat.Id,
                    new SendMessageRequest
                    {
                        Text = "Чат создан",
                        IsSystem = true
                    }
                );
            }

            User? otherUser = null;

            if (!chat.IsGroup)
            {
                var otherUserId = chat.Members
                    .Select(x => x.UserId)
                    .FirstOrDefault(id => id != creatorId);

                if (otherUserId != Guid.Empty)
                {
                    otherUser = await _db.Users
                        .FirstOrDefaultAsync(x => x.Id == otherUserId);
                }
            }

            var lastMsg = chat.Messages?.OrderByDescending(m => m.CreatedAt).FirstOrDefault();

            var chatResponse = new ChatResponse
            {
                Id = chat.Id,
                Name = chat.IsGroup
                    ? chat.Name
                    : otherUser?.Name ?? "Чат",

                AvatarUrl = chat.IsGroup
                    ? chat.AvatarUrl
                    : otherUser?.AvatarUrl,

                OwnerId = chat.OwnerId,
                IsGroup = chat.IsGroup,
                MembersCount = chat.Members.Count,
                CreatedAt = chat.CreatedAt,

                // Чат пока пустой
                LastMessageText = lastMsg?.Text,
                LastMessageAt = lastMsg?.CreatedAt,
                LastMessageSenderId = lastMsg?.Sender?.Id,
                LastMessageSenderName = lastMsg?.Sender?.Name,
                HasLastMessageFile = lastMsg?.Attachments?.Any() == true,
                UnreadMessagesCount = 0
            };

            if (chat.IsGroup)
            {
                foreach (var userId in existingUserIds)
                {
                    await _notificationHubContext
                        .Clients
                        .User(userId.ToString())
                        .SendAsync(
                            "GroupCreated",
                            chatResponse
                        );
                }
            }

            return chatResponse;
        }

        /// Удалить чат.
        public async Task DeleteAsync(Guid userId, Guid chatId)
        {
            var chat = await _db.Chats
                .Include(c => c.Members) // важно: загружаем участников
                .FirstOrDefaultAsync(x => x.Id == chatId);

            if (chat == null)
                throw new Exception("Чат не найден.");

            if (chat.OwnerId != userId)
                throw new Exception("Пользователь не является владельцем");

            // Сохраняем ID всех участников до удаления
            var memberIds = chat.Members.Select(m => m.UserId).ToList();

            // Удаляем сообщения и сам чат
            await _db.Messages.Where(x => x.ChatId == chat.Id).ExecuteDeleteAsync();
            _db.Chats.Remove(chat);
            await _db.SaveChangesAsync();

            // Уведомляем всех участников об удалении группы
            foreach (var memberId in memberIds)
            {
                await _notificationHubContext.Clients.User(memberId.ToString())
                    .SendAsync("GroupDeleted", new { ChatId = chatId });
            }
        }

        /// <summary>
        /// Получить информацию о конкретном чате с количеством непрочитанных.
        /// </summary>
        public async Task<ChatResponse> GetChatAsync(Guid chatId, Guid currentUserId)
        {
            var chat = await _db.Chats
                .Include(x => x.Members)
                    .ThenInclude(cm => cm.User)
                .Include(x => x.Messages)
                .FirstOrDefaultAsync(x => x.Id == chatId);

            if (chat == null)
                throw new Exception("Чат не найден.");

            // Проверяем, что пользователь является участником чата
            if (!chat.Members.Any(m => m.UserId == currentUserId))
                throw new UnauthorizedAccessException("Вы не являетесь участником этого чата.");

            var lastMsg = chat.Messages?.OrderByDescending(m => m.CreatedAt).FirstOrDefault();

            string displayName = chat.Name;
            string? avatarUrl = chat.AvatarUrl;
            string? username = null;

            if (!chat.IsGroup)
            {
                var otherMember = chat.Members.FirstOrDefault(m => m.UserId != currentUserId);
                if (otherMember != null)
                {
                    displayName = otherMember.User.Name;
                    username = otherMember.User.UserName;
                    avatarUrl = otherMember.User.AvatarUrl;
                }
                else
                {
                    displayName = chat.Name ?? "Чат";
                }
            }

            var unreadCount = await _db.Messages
                .Where(m => m.ChatId == chatId && !m.IsDeleted && !m.IsSystem)
                .Where(m => !_db.MessagesReadStatuses
                    .Any(rs => rs.MessageId == m.Id && rs.UserId == currentUserId))
                .CountAsync();

            return new ChatResponse
            {
                Id = chat.Id,
                Name = displayName,
                UserName = username,
                AvatarUrl = avatarUrl,
                OwnerId = chat.OwnerId,
                IsGroup = chat.IsGroup,
                MembersCount = chat.Members.Count,
                CreatedAt = chat.CreatedAt,
                LastMessageText = lastMsg?.Text,
                LastMessageAt = lastMsg?.CreatedAt,
                LastMessageSenderId = lastMsg?.Sender?.Id,
                LastMessageSenderName = lastMsg?.Sender?.Name,
                HasLastMessageFile = lastMsg?.Attachments?.Any() == true,
                UnreadMessagesCount = unreadCount
            };
        }

        /// Получить все чаты пользователя.
        public async Task<IEnumerable<ChatResponse>> GetChatsAsync(Guid userId)
        {
            var query = from cm in _db.ChatMembers
                        where cm.UserId == userId
                        join c in _db.Chats on cm.ChatId equals c.Id
                        where c.IsInitialised
                        let otherUser = _db.ChatMembers
                            .Where(cm2 => cm2.ChatId == c.Id && cm2.UserId != userId)
                            .Select(cm2 => cm2.User)
                            .FirstOrDefault()
                        let lastMsg = _db.Messages
                            .Where(m => m.ChatId == c.Id && !m.IsDeleted)
                            .OrderByDescending(m => m.CreatedAt)
                            .FirstOrDefault()
                        // Подсчёт непрочитанных сообщений для текущего пользователя в этом чате
                        let unreadCount = _db.Messages
                            .Where(m => m.ChatId == c.Id && !m.IsDeleted && !m.IsSystem)
                            .Where(m => !_db.MessagesReadStatuses
                                .Any(rs => rs.MessageId == m.Id && rs.UserId == userId))
                            .Count()
                        select new ChatResponse
                        {
                            Id = c.Id,
                            UserId = c.IsGroup ? null : otherUser.Id,
                            // Для личных чатов подставляем имя собеседника, для групповых – название чата
                            Name = c.IsGroup ? c.Name : (otherUser != null ? otherUser.Name : "Unknown"),
                            AvatarUrl = c.IsGroup ? c.AvatarUrl : (otherUser != null ? otherUser.AvatarUrl : null),
                            OwnerId = c.OwnerId,
                            IsGroup = c.IsGroup,
                            MembersCount = c.Members.Count,
                            CreatedAt = c.CreatedAt,
                            LastMessageText = lastMsg != null ? lastMsg.Text : null,
                            LastMessageSenderName = lastMsg != null ? lastMsg.Sender.Name : null,
                            LastMessageSenderId = lastMsg != null ? lastMsg.Sender.Id : null,
                            LastMessageAt = lastMsg != null ? lastMsg.CreatedAt : (DateTime?)null,
                            HasLastMessageFile = lastMsg != null ? lastMsg.Attachments.Count > 0 : false,
                            UnreadMessagesCount = unreadCount
                        };

            var chats = await query.ToListAsync();

            return chats
                .OrderByDescending(x => x.LastMessageAt ?? x.CreatedAt)
                .ToList();
        }

        public async Task<IEnumerable<UserResponse>> GetMembersAsync(Guid chatId)
        {
            var chatExists = await _db.Chats
                .AnyAsync(x => x.Id == chatId);

            if (!chatExists)
                throw new Exception("Чат не найден.");

            // Получаем всех участников с их пользовательскими данными
            return await _db.ChatMembers
                .Where(cm => cm.ChatId == chatId)
                .Include(cm => cm.User)
                .Select(cm => new UserResponse
                {
                    Id = cm.User.Id,
                    Name = cm.User.Name,
                    UserName = cm.User.UserName,
                    AvatarUrl = cm.User.AvatarUrl,
                })
                .ToListAsync();
        }

        public async Task<ChatResponse> GetPrivateChatAsync(Guid user1Id, Guid user2Id)
        {
            var chat = await _db.Chats
                .Include(c => c.Members)
                    .ThenInclude(cm => cm.User)
                .Where(c => !c.IsGroup)
                .Where(c => c.Members.Any(m => m.UserId == user1Id))
                .Where(c => c.Members.Any(m => m.UserId == user2Id))
                .FirstOrDefaultAsync();

            if (chat == null)
                throw new Exception("Чат не найден.");

            var otherMember = chat.Members
                .FirstOrDefault(m => m.UserId != user1Id);

            var lastMsg = await _db.Messages
                .Where(m => m.ChatId == chat.Id && !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();

            return new ChatResponse
            {
                Id = chat.Id,
                UserId = otherMember?.UserId,

                // Имя и аватар собеседника
                Name = otherMember?.User?.Name ?? "Чат",
                UserName = otherMember?.User?.Name ?? string.Empty,
                AvatarUrl = otherMember?.User?.AvatarUrl,

                OwnerId = chat.OwnerId,
                IsGroup = chat.IsGroup,
                MembersCount = chat.Members.Count,
                CreatedAt = chat.CreatedAt,

                LastMessageText = lastMsg?.Text,
                LastMessageAt = lastMsg?.CreatedAt,
                LastMessageSenderId = lastMsg?.Sender?.Id,
                LastMessageSenderName = lastMsg?.Sender?.Name,
                HasLastMessageFile = lastMsg?.Attachments?.Count > 0
            };
        }

        /// Удалить пользователя из чата.
        public async Task RemoveMemberAsync(Guid chatId, Guid userId, Guid targetId)
        {
            var chat = await _db.Chats.FirstOrDefaultAsync(x => x.Id == chatId);
            if (chat == null)
                throw new Exception("Чат не найден.");

            var member = await _db.ChatMembers
                .FirstOrDefaultAsync(x => x.ChatId == chatId && x.UserId == targetId);
            if (member == null)
                throw new Exception("Пользователь не найден");

            // Проверка прав: владелец может удалять всех, обычный пользователь – только себя
            if (chat.OwnerId != userId && userId != targetId)
                throw new UnauthorizedAccessException("Только владелец может удалять других участников.");

            _db.ChatMembers.Remove(member);
            await _db.SaveChangesAsync();

            // 1. Отправляем системное сообщение
            var actor = await _db.Users.FindAsync(userId);
            var targetUser = await _db.Users.FindAsync(targetId);
            string systemMessage;
            if (userId == targetId)
                systemMessage = $"{targetUser?.Name ?? "Пользователь"} покинул чат.";
            else
                systemMessage = $"{actor?.Name ?? "Кто-то"} исключил {targetUser?.Name ?? "пользователя"} из чата";

            await _messageService.SendSystemAsync(chatId, new SendMessageRequest
            {
                Text = systemMessage,
                IsSystem = true
            });

            // 2. Отправляем обновлённый список участников через SignalR
            var members = await GetMembersAsync(chatId);
            await _chatHubContext.Clients.Group(chatId.ToString())
                .SendAsync("GroupMembersUpdated", members);

            // Уведомляем пользователя, который вышел или был удалён из группы
            await _notificationHubContext.Clients
                .User(targetId.ToString())
                .SendAsync("ChatsUpdated", new
                {
                    ChatId = chatId
                });

            await _notificationHubContext.Clients
                .User(targetId.ToString())
                .SendAsync("ExcludedFromChat", new
                {
                    ChatId = chatId
                });

            // 3. Обновление чата – только если не самовыход
            if (userId != targetId)
            {
                var updatedChat = await GetChatAsync(chatId, userId);
                await _chatHubContext.Clients.Group(chatId.ToString())
                    .SendAsync("GroupUpdated", updatedChat);
            }

            // Если участников не осталось – удаляем чат
            bool hasMembers = await _db.ChatMembers.AnyAsync(x => x.ChatId == chatId);
            if (!hasMembers)
            {
                await _db.Messages.Where(x => x.ChatId == chat.Id).ExecuteDeleteAsync();
                _db.Chats.Remove(chat);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<ChatResponse> UpdateGroupAsync(Guid chatId, UpdateGroupRequest request, Guid currentUserId)
        {
            var chat = await _db.Chats
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.Id == chatId);
            if (chat == null)
                throw new Exception("Чат не найден.");
            if (!chat.IsGroup)
                throw new Exception("Чат не является групповым.");
            if (chat.OwnerId != currentUserId)
                throw new UnauthorizedAccessException("Только владелец может изменять данные группы.");

            // Если передан файл – загружаем его
            if (request.AvatarFile != null)
            {
                var attachmentResponse = await _attachmentService.UploadAsync(request.AvatarFile, IsAvatar: true, chatId: chatId);
                chat.AvatarUrl = attachmentResponse.Url; // обновляем аватар
            }

            // Обновляем имя, если передано
            if (!string.IsNullOrWhiteSpace(request.Name))
                chat.Name = request.Name;

            await _db.SaveChangesAsync();

            var actor = await _db.Users.FindAsync(currentUserId);

            bool hasAvatar = request.AvatarFile != null;
            bool hasName = !string.IsNullOrWhiteSpace(request.Name);

            string? message = null;

            if (hasAvatar && hasName)
            {
                message = $"{actor?.Name ?? "Кто-то"} обновил данные группы";
            }
            else if (hasAvatar)
            {
                message = $"{actor?.Name ?? "Кто-то"} обновил фото группы";
            }
            else if (hasName)
            {
                message = $"{actor?.Name ?? "Кто-то"} обновил название группы";
            }

            if (!string.IsNullOrEmpty(message))
            {
                await _messageService.SendSystemAsync(chatId, new SendMessageRequest
                {
                    Text = message,
                    IsSystem = true
                });
            }

            var response = await GetChatAsync(chatId, currentUserId);

            await _chatHubContext.Clients
                .Group(chatId.ToString())
                .SendAsync("GroupUpdated", response);

            return response;
        }
    }
}