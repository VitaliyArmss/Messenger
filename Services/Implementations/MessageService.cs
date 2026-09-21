using Messenger.Data;
using Messenger.DTO.Attachments;
using Messenger.DTO.Chats;
using Messenger.DTO.Messages;
using Messenger.DTO.Users;
using Messenger.Entities;
using Messenger.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Services
{
    public class MessageService : IMessageService
    {
        private readonly IHubContext<ChatHub> _chatHubContext;
        private readonly AppDbContext _db;
        private readonly IHubContext<NotificationHub> _notificationHubContext;

        public MessageService(AppDbContext db,
                          IHubContext<ChatHub> chatHubContext,
                          IHubContext<NotificationHub> notificationHubContext)
        {
            _db = db;
            _chatHubContext = chatHubContext;
            _notificationHubContext = notificationHubContext;
        }

        /// <summary>
        /// Удалить сообщение.
        /// </summary>
        public async Task DeleteAsync(Guid messageId)
        {
            var message = await _db.Messages
                .FirstOrDefaultAsync(x => x.Id == messageId);

            if (message == null)
                return;

            message.IsDeleted = true;

            await _db.SaveChangesAsync();

            // Отправляем событие в группу чата
            await _chatHubContext.Clients.Group(message.ChatId.ToString())
                .SendAsync("MessageDeleted", new { MessageId = messageId, ChatId = message.ChatId });
        }

        /// <summary>
        /// Изменить сообщение.
        /// </summary>
        public async Task<MessageResponse> EditAsync(
            Guid messageId,
            EditMessageRequest request)
        {
            // 2. Проверить права владельца.

            var message = await _db.Messages
                .Include(x => x.Sender)
                .FirstOrDefaultAsync(x => x.Id == messageId);

            if (message == null)
                throw new Exception("Сообщение не найдено.");

            message.Text = request.Text;
            message.IsEdited = true;

            await _db.SaveChangesAsync();

            var response = new MessageResponse
            {
                Id = message.Id,
                SenderId = message.SenderId,
                SenderName = message.Sender.Name,
                Text = message.Text,
                CreatedAt = message.CreatedAt,
                IsEdited = true
            };

            // Отправляем событие в группу чата
            await _chatHubContext.Clients.Group(message.ChatId.ToString())
                .SendAsync("MessageEdited", response);

            return response;
        }

        public async Task<Guid?> GetFirstUnreadMessageIdAsync(Guid chatId, Guid userId)
        {
            // Проверяем, что пользователь участник чата
            bool isMember = await _db.ChatMembers.AnyAsync(cm => cm.ChatId == chatId && cm.UserId == userId);
            if (!isMember)
                throw new UnauthorizedAccessException("Вы не участник этого чата");

            // Находим самое старое непрочитанное сообщение (не системное, не удалённое)
            var unreadMessage = await _db.Messages
                .Where(m => m.ChatId == chatId && !m.IsDeleted && !m.IsSystem)
                .Where(m => !_db.MessagesReadStatuses
                    .Any(rs => rs.MessageId == m.Id && rs.UserId == userId))
                .OrderBy(m => m.CreatedAt)
                .Select(m => new { m.Id, m.CreatedAt })
                .FirstOrDefaultAsync();

            return unreadMessage?.Id;
        }

        public async Task<IEnumerable<MessageResponse>> GetMessagesAroundAsync(Guid chatId, Guid messageId, int count)
        {
            // 1. Находим целевое сообщение
            var targetMessage = await _db.Messages
            .Include(m => m.Sender)
            .Include(m => m.Attachments)
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ChatId == chatId && !m.IsDeleted);
            if (targetMessage == null)
                throw new Exception("Сообщение не найдено.");

            // 2. Получаем общее количество сообщений в чате и количество более старых
            var totalCount = await _db.Messages
                .CountAsync(m => m.ChatId == chatId && !m.IsDeleted);
            var olderCount = await _db.Messages
                .CountAsync(m => m.ChatId == chatId && m.CreatedAt < targetMessage.CreatedAt && !m.IsDeleted);

            // 3. Вычисляем желаемое количество до и после (включая само сообщение)
            int half = count / 2;
            int before = half;
            int after = count - half - 1; // вычитаем само сообщение

            // 4. Корректируем с учётом фактического количества
            if (olderCount < before)
            {
                after += (before - olderCount);
                before = olderCount;
            }
            var newerCount = totalCount - olderCount - 1; // сообщения после
            if (after > newerCount)
            {
                before += (after - newerCount);
                after = newerCount;
            }
            // Ограничиваем минимальное количество (если сообщений меньше count, то возьмём сколько есть)
            if (before < 0) before = 0;
            if (after < 0) after = 0;

            // 5. Загружаем старые сообщения (в хронологическом порядке)
            var olderMessages = await _db.Messages
                .Where(m => m.ChatId == chatId && m.CreatedAt < targetMessage.CreatedAt && !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .Take(before)
                .Include(m => m.Sender)
                .Include(m => m.Attachments)
                .ToListAsync();
            olderMessages.Reverse(); // теперь старые сверху

            // 6. Загружаем новые сообщения (в хронологическом порядке)
            var newerMessages = await _db.Messages
                .Where(m => m.ChatId == chatId && m.CreatedAt > targetMessage.CreatedAt && !m.IsDeleted)
                .OrderBy(m => m.CreatedAt)
                .Take(after)
                .Include(m => m.Sender)
                .Include(m => m.Attachments)
                .ToListAsync();

            // 7. Собираем все сообщения в правильном порядке
            var allMessages = olderMessages
                .Concat(new[] { targetMessage })
                .Concat(newerMessages)
                .ToList();

            // 8. Подгружаем статусы прочтения (как в GetMessagesAsync)
            var messageIds = allMessages.Select(m => m.Id).ToList();
            var readStatusesWithUsers = await _db.MessagesReadStatuses
                .Where(rs => messageIds.Contains(rs.MessageId))
                .Join(_db.Users,
                      rs => rs.UserId,
                      u => u.Id,
                      (rs, u) => new { rs.MessageId, User = u, rs.ReadAt })
                .ToListAsync();

            var readByMap = readStatusesWithUsers
                .GroupBy(x => x.MessageId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new UserReadInfo
                    {
                        Id = x.User.Id,
                        Name = x.User.Name,
                        AvatarUrl = x.User.AvatarUrl,
                        ReadAt = x.ReadAt
                    }).ToList()
                );

            // 9. Формируем ответ
            return allMessages.Select(m => new MessageResponse
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = m.Sender?.Name,
                SenderAvatarUrl = m.Sender?.AvatarUrl,
                Text = m.Text,
                CreatedAt = m.CreatedAt,
                IsEdited = m.IsEdited,
                IsSystem = m.IsSystem,
                Attachments = m.Attachments,
                ReadBy = readByMap.GetValueOrDefault(m.Id) ?? new List<UserReadInfo>()
            }).ToList();
        }

        /// Получить сообщения чата.
        public async Task<IEnumerable<MessageResponse>> GetMessagesAsync(Guid chatId, int skip = 0, int take = 50)
        {
            // 1. Загружаем сообщения
            var messages = await _db.Messages
                .Where(m => m.ChatId == chatId && !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Include(m => m.Sender)
                .Include(m => m.Attachments)
                .ToListAsync();

            // 2. Получаем ID сообщений
            var messageIds = messages.Select(m => m.Id).ToList();

            // 3. Загружаем статусы прочтения с информацией о пользователях через Join
            var readStatusesWithUsers = await _db.MessagesReadStatuses
                .Where(rs => messageIds.Contains(rs.MessageId))
                .Join(_db.Users,
                      rs => rs.UserId,
                      u => u.Id,
                      (rs, u) => new { rs.MessageId, User = u, rs.ReadAt })
                .ToListAsync();

            // 4. Группируем по MessageId, создаём словарь
            var readByMap = readStatusesWithUsers
                .GroupBy(x => x.MessageId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new UserReadInfo
                    {
                        Id = x.User.Id,
                        Name = x.User.Name,
                        AvatarUrl = x.User.AvatarUrl,
                        ReadAt = x.ReadAt
                    }).ToList()
                );

            // 5. Формируем ответ
            return messages.Select(m => new MessageResponse
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = m.Sender?.Name,
                SenderAvatarUrl = m.Sender?.AvatarUrl,
                Text = m.Text,
                CreatedAt = m.CreatedAt,
                IsEdited = m.IsEdited,
                IsSystem = m.IsSystem,
                Attachments = m.Attachments,
                ReadBy = readByMap.GetValueOrDefault(m.Id) ?? new List<UserReadInfo>()
            }).ToList();
        }

        public async Task<IEnumerable<MessageResponse>> GetMessagesByDirectionAsync(
            Guid chatId,
            Guid anchorMessageId,
            string direction,
            int count)
        {
            // Проверяем существование чата и опорного сообщения
            var chat = await _db.Chats.AnyAsync(x => x.Id == chatId);
            if (!chat)
                throw new Exception("Чат не найден.");

            var anchor = await _db.Messages
                .FirstOrDefaultAsync(m => m.Id == anchorMessageId && m.ChatId == chatId && !m.IsDeleted);
            if (anchor == null)
                throw new Exception("Опорное сообщение не найдено.");

            IQueryable<Message> query = _db.Messages
                .Where(m => m.ChatId == chatId && !m.IsDeleted && m.Id != anchorMessageId);

            if (direction.Equals("older", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(m => m.CreatedAt < anchor.CreatedAt)
                             .OrderByDescending(m => m.CreatedAt)
                             .Take(count);
                // Для хронологического порядка нужно развернуть
                var messages = await query
                    .Include(m => m.Sender)
                    .Include(m => m.Attachments)
                    .ToListAsync();
                messages.Reverse(); // теперь старые сверху
                return await BuildMessageResponsesAsync(messages);
            }
            else if (direction.Equals("newer", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(m => m.CreatedAt > anchor.CreatedAt)
                             .OrderBy(m => m.CreatedAt)
                             .Take(count);
                var messages = await query
                    .Include(m => m.Sender)
                    .Include(m => m.Attachments)
                    .ToListAsync();
                return await BuildMessageResponsesAsync(messages);
            }
            else
            {
                throw new ArgumentException("Направление должно быть 'older' или 'newer'.");
            }
        }

        public async Task<int> MarkMessagesAsReadAsync(Guid chatId, Guid userId, List<Guid> messageIds)
        {
            if (messageIds == null || messageIds.Count == 0)
                throw new ArgumentException("Список сообщений не может быть пустым");

            var isMember = await _db.ChatMembers
                .AnyAsync(cm => cm.ChatId == chatId && cm.UserId == userId);
            if (!isMember)
                throw new UnauthorizedAccessException("Вы не участник этого чата");

            var unreadMessages = await _db.Messages
                .Where(m => m.ChatId == chatId && !m.IsDeleted && messageIds.Contains(m.Id))
                .Where(m => !_db.MessagesReadStatuses
                    .Any(rs => rs.MessageId == m.Id && rs.UserId == userId))
                .ToListAsync();

            if (unreadMessages.Count == 0)
            {
                var currentUnread = await _db.Messages
                    .Where(m => m.ChatId == chatId && !m.IsDeleted && !m.IsSystem)
                    .Where(m => !_db.MessagesReadStatuses
                        .Any(rs => rs.MessageId == m.Id && rs.UserId == userId))
                    .CountAsync();
                return currentUnread;
            }

            foreach (var msg in unreadMessages)
            {
                _db.MessagesReadStatuses.Add(new MessageReadStatus
                {
                    MessageId = msg.Id,
                    UserId = userId,
                    ReadAt = DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();

            // Получаем информацию о пользователе для отправки
            var user = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => new UserReadInfo
                {
                    Id = u.Id,
                    Name = u.Name,
                    AvatarUrl = u.AvatarUrl,
                    ReadAt = DateTime.UtcNow
                })
                .FirstOrDefaultAsync();

            var readMessageIds = unreadMessages.Select(m => m.Id).ToList();

            // Отправляем событие MessageRead всем участникам чата через ChatHub
            await _chatHubContext.Clients.Group(chatId.ToString())
                .SendAsync("MessageRead", new
                {
                    ChatId = chatId,
                    Reader = user,
                    MessageIds = readMessageIds
                });

            // Отправляем уведомление обновления счётчика текущему пользователю
            var remainingUnread = await _db.Messages
                .Where(m => m.ChatId == chatId && !m.IsDeleted && !m.IsSystem)
                .Where(m => !_db.MessagesReadStatuses
                    .Any(rs => rs.MessageId == m.Id && rs.UserId == userId))
                .CountAsync();

            await _notificationHubContext.Clients.User(userId.ToString())
                .SendAsync("UpdateUnreadCount", new
                {
                    ChatId = chatId,
                    UnreadCount = remainingUnread
                });

            return remainingUnread;
        }

        /// Отправить сообщение.
        public async Task<MessageResponse> SendAsync(
            Guid chatId,
            Guid senderId,
            SendMessageRequest request)
        {
            // Проверяем чат
            var chat = await _db.Chats
                .FirstOrDefaultAsync(x => x.Id == chatId);

            if (chat == null)
                throw new Exception("Чат не найден.");

            // Проверяем отправителя
            var sender = await _db.Users
                .FirstOrDefaultAsync(x => x.Id == senderId);

            if (sender == null)
                throw new Exception("Пользователь не найден.");

            // Проверяем, что пользователь состоит в чате
            var isMember = await _db.ChatMembers
                .AnyAsync(x =>
                    x.ChatId == chatId &&
                    x.UserId == senderId);

            if (!isMember)
                throw new UnauthorizedAccessException(
                    "Пользователь не состоит в чате.");

            // Создаём сообщение
            var message = new Message
            {
                Id = Guid.NewGuid(),
                ChatId = chatId,
                SenderId = senderId,
                Text = request.Text,
                CreatedAt = DateTime.UtcNow,
                IsEdited = false,
                IsSystem = false,
                IsDeleted = false,
                Attachments = new List<Attachment>()
            };

            _db.Messages.Add(message);

            await _db.SaveChangesAsync();

            // Получаем участников
            var memberIds = await _db.ChatMembers
                .Where(cm => cm.ChatId == chatId)
                .Select(cm => cm.UserId)
                .ToListAsync();

            // Привязываем вложения
            List<Attachment> attachments = new();

            if (request.Attachments != null &&
                request.Attachments.Any())
            {
                var attachmentIds = request.Attachments
                    .Select(a => a.Id)
                    .ToList();

                attachments = await _db.Attachments
                    .Where(a =>
                        attachmentIds.Contains(a.Id) &&
                        a.MessageId == null)
                    .ToListAsync();

                if (attachments.Count != request.Attachments.Count)
                {
                    throw new Exception(
                        "Одно или несколько вложений не найдены или уже привязаны."
                    );
                }

                foreach (var attachment in attachments)
                {
                    attachment.MessageId = message.Id;
                    attachment.ChatId = message.ChatId;
                }

                await _db.SaveChangesAsync();

                // Обновляем navigation property,
                // чтобы BuildChatResponseForUserAsync видел вложения
                message.Attachments = attachments;
            }

            // Если чат ещё не инициализирован —
            // инициализируем его после создания первого сообщения
            if (!chat.IsInitialised)
            {
                // Активируем чат
                chat.IsInitialised = true;

                await _db.SaveChangesAsync();

                // Отправляем GroupCreated всем участникам.
                // Для каждого пользователя формируется свой ChatResponse,
                // что важно для личных чатов.
                foreach (var memberId in memberIds)
                {
                    var chatResponse =
                        await BuildChatResponseForUserAsync(
                            chat,
                            memberId,
                            message
                        );

                    await _notificationHubContext
                        .Clients
                        .User(memberId.ToString())
                        .SendAsync(
                            "GroupCreated",
                            chatResponse
                        );
                }
            }

            // Формируем ответ сообщения
            var response = new MessageResponse
            {
                Id = message.Id,
                SenderId = sender.Id,
                SenderName = sender.Name,
                SenderAvatarUrl = sender.AvatarUrl,
                Text = message.Text,
                CreatedAt = message.CreatedAt,
                IsEdited = false,
                IsSystem = false,
                Attachments = attachments,

                ReadBy = new List<UserReadInfo>
        {
            new UserReadInfo
            {
                Id = sender.Id,
                Name = sender.Name,
                AvatarUrl = sender.AvatarUrl,
                ReadAt = DateTime.UtcNow
            }
        }
            };

            // Отправляем пользовательское сообщение через SignalR
            await _chatHubContext
                .Clients
                .Group(chatId.ToString())
                .SendAsync(
                    "ReceiveMessage",
                    response
                );

            // Уведомляем участников и обновляем счётчик непрочитанных
            foreach (var memberId in memberIds)
            {
                int unreadCount;

                if (memberId == senderId)
                {
                    unreadCount = 0;
                }
                else
                {
                    unreadCount = await _db.Messages
                        .Where(m =>
                            m.ChatId == chatId &&
                            !m.IsDeleted &&
                            !m.IsSystem)
                        .Where(m =>
                            !_db.MessagesReadStatuses.Any(rs =>
                                rs.MessageId == m.Id &&
                                rs.UserId == memberId))
                        .CountAsync();
                }

                await _notificationHubContext
                    .Clients
                    .User(memberId.ToString())
                    .SendAsync(
                        "ReceiveNotification",
                        new
                        {
                            ChatId = chatId,
                            LastMessageText = response.Text,
                            LastMessageAt = response.CreatedAt,
                            SenderName = response.SenderName,
                            UnreadCount = unreadCount
                        }
                    );
            }

            // Отправляем события о добавленных вложениях
            foreach (var attachment in attachments)
            {
                var attachmentResponse = new AttachmentResponse
                {
                    Id = attachment.Id,
                    FileName = attachment.FileName,
                    Url = attachment.Url,
                    Size = attachment.Size,
                    ContentType = attachment.ContentType,
                    Type = attachment.Type
                };

                await _chatHubContext
                    .Clients
                    .Group(chatId.ToString())
                    .SendAsync(
                        "AttachmentAdded",
                        new
                        {
                            ChatId = chatId,
                            Attachment = attachmentResponse
                        }
                    );
            }

            return response;
        }

        public async Task<MessageResponse> SendSystemAsync(
                    Guid chatId,
                    SendMessageRequest request)
        {
            var chat = await _db.Chats
                .FirstOrDefaultAsync(x => x.Id == chatId);

            if (chat == null)
                throw new Exception("Чат не найден.");

            var message = new Message
            {
                Id = Guid.NewGuid(),
                ChatId = chatId,
                SenderId = null,
                Text = request.Text,
                CreatedAt = DateTime.UtcNow,
                IsEdited = false,
                IsSystem = true,
                IsDeleted = false,
                Attachments = request.Attachments
            };

            _db.Messages.Add(message);

            await _db.SaveChangesAsync();

            var response = new MessageResponse
            {
                Id = message.Id,
                Text = message.Text,
                CreatedAt = message.CreatedAt,
                IsEdited = false,
                IsSystem = message.IsSystem,
                Attachments = message.Attachments
            };

            await _chatHubContext.Clients.Group(chatId.ToString()).SendAsync("ReceiveMessage", response);

            // Отправка уведомления через NotificationHub всем участникам чата (кроме отправителя)
            var memberIds = await _db.ChatMembers
                .Where(cm => cm.ChatId == chatId)
                .Select(cm => cm.UserId.ToString())
                .ToListAsync();

            foreach (var userId in memberIds)
            {
                await _notificationHubContext.Clients.User(userId)
                    .SendAsync("ReceiveNotification", new
                    {
                        ChatId = chatId,
                        LastMessageText = response.Text,
                        LastMessageAt = response.CreatedAt
                    });
            }

            return response;
        }

        private async Task<ChatResponse> BuildChatResponseForUserAsync(
            Chat chat,
            Guid userId,
            Message lastMessage)
        {
            var members = await _db.ChatMembers
                .Where(x => x.ChatId == chat.Id)
                .Include(x => x.User)
                .ToListAsync();

            string? name = chat.Name;
            string? avatarUrl = chat.AvatarUrl;
            string? username = null;

            if (!chat.IsGroup)
            {
                var otherMember = members
                    .FirstOrDefault(x => x.UserId != userId);

                if (otherMember != null)
                {
                    name = otherMember.User.Name;
                    avatarUrl = otherMember.User.AvatarUrl;
                    username = otherMember.User.UserName;
                }
            }

            return new ChatResponse
            {
                Id = chat.Id,
                Name = name ?? "Чат",
                UserName = username,
                AvatarUrl = avatarUrl,

                OwnerId = chat.OwnerId,
                IsGroup = chat.IsGroup,

                MembersCount = members.Count,

                CreatedAt = chat.CreatedAt,

                LastMessageText = lastMessage.Text,
                LastMessageSenderId = lastMessage.SenderId,
                LastMessageSenderName = lastMessage.Sender?.Name,

                LastMessageAt = lastMessage.CreatedAt,

                HasLastMessageFile =
                    lastMessage.Attachments?.Any() == true,

                UnreadMessagesCount = 0
            };
        }

        // Вспомогательный метод для построения ответов
        private async Task<IEnumerable<MessageResponse>> BuildMessageResponsesAsync(List<Message> messages)
        {
            if (!messages.Any()) return Enumerable.Empty<MessageResponse>();

            var messageIds = messages.Select(m => m.Id).ToList();
            var readStatusesWithUsers = await _db.MessagesReadStatuses
                .Where(rs => messageIds.Contains(rs.MessageId))
                .Join(_db.Users,
                      rs => rs.UserId,
                      u => u.Id,
                      (rs, u) => new { rs.MessageId, User = u, rs.ReadAt })
                .ToListAsync();

            var readByMap = readStatusesWithUsers
                .GroupBy(x => x.MessageId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new UserReadInfo
                    {
                        Id = x.User.Id,
                        Name = x.User.Name,
                        AvatarUrl = x.User.AvatarUrl,
                        ReadAt = x.ReadAt
                    }).ToList()
                );

            return messages.Select(m => new MessageResponse
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = m.Sender?.Name,
                SenderAvatarUrl = m.Sender?.AvatarUrl,
                Text = m.Text,
                CreatedAt = m.CreatedAt,
                IsEdited = m.IsEdited,
                IsSystem = m.IsSystem,
                Attachments = m.Attachments,
                ReadBy = readByMap.GetValueOrDefault(m.Id) ?? new List<UserReadInfo>()
            }).ToList();
        }
    }
}