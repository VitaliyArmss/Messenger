using Messenger.Data;
using Messenger.DTO.Users;
using Messenger.Entities;
using Messenger.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Services
{
    public class UserService : IUserService
    {
        private readonly IAttachmentService _attachmentService;
        private readonly IHubContext<ChatHub> _chatHubContext;
        private readonly AppDbContext _db;
        private readonly IHubContext<NotificationHub> _notificationHubContext;

        public UserService(AppDbContext db, IHubContext<ChatHub> chatHubContext, IAttachmentService attachmentService, IHubContext<NotificationHub> notificationHubContext)
        {
            _db = db;
            _chatHubContext = chatHubContext;
            _attachmentService = attachmentService;
            _notificationHubContext = notificationHubContext;
        }

        public async Task<UserResponse?> GetByIdAsync(Guid id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);

            if (user == null)
                return null;

            return new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                UserName = user.UserName,
                AvatarUrl = user.AvatarUrl,
                IsOnline = false
            };
        }

        public async Task<IEnumerable<UserResponse>> GetContactsAsync(Guid userId)
        {
            // 1. Загружаем личные чаты с участниками и их пользователями
            var privateChats = await _db.Chats
                .Where(c => !c.IsGroup && c.Members.Any(m => m.UserId == userId))
                .Include(c => c.Members)
                    .ThenInclude(cm => cm.User)
                .ToListAsync();

            // 2. Для каждого чата находим собеседника
            var contacts = new List<UserResponse>();
            foreach (var chat in privateChats)
            {
                var otherMember = chat.Members.FirstOrDefault(m => m.UserId != userId);
                if (otherMember != null)
                {
                    var user = otherMember.User;
                    contacts.Add(new UserResponse
                    {
                        Id = user.Id,
                        Name = user.Name,
                        UserName = user.UserName,
                        AvatarUrl = user.AvatarUrl,
                        IsOnline = false // позже можно доработать с SignalR
                    });
                }
            }

            // 3. Убираем дубликаты (на случай, если есть несколько личных чатов с одним и тем же пользователем)
            return contacts.DistinctBy(u => u.Id).ToList();
        }

        public async Task<UserResponse> GetCurrentUserAsync(Guid userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
                throw new Exception("Пользователь не найден.");

            return new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                UserName = user.UserName,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                IsOnline = false
            };
        }

        public async Task<IEnumerable<UserResponse>> SearchAsync(
            string? query,
            int page,
            int pagesize,
            Guid currentUserId)
        {
            IQueryable<User> users = _db.Users;

            users = users.Where(u => u.Id != currentUserId);

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.Trim().ToLower();
                users = users.Where(x =>
                    x.Name.ToLower().Contains(query) ||
                    x.UserName.ToLower().Contains(query));
            }

            return await users
                .OrderBy(x => x.Name)
                .Skip((page - 1) * pagesize)
                .Take(pagesize)
                .Select(x => new UserResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    UserName = x.UserName,
                    AvatarUrl = x.AvatarUrl,
                    IsOnline = false
                })
                .ToListAsync();
        }

        public async Task<UserResponse> UpdateAsync(Guid userId, UpdateUserRequest request)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) throw new Exception("Пользователь не найден.");

            // Если передан файл – загружаем его
            string? avatarUrl = null;
            if (request.AvatarFile != null)
            {
                var attachmentResponse = await _attachmentService.UploadAsync(request.AvatarFile, IsAvatar: true);
                avatarUrl = attachmentResponse.Url;
            }

            if (!string.IsNullOrWhiteSpace(request.UserName))
            {
                bool exists = await _db.Users.AnyAsync(x => x.UserName == request.UserName && x.Id != userId);
                if (exists) throw new Exception("Данный username уже занят.");
                user.UserName = request.UserName;
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
                user.Name = request.Name;

            if (avatarUrl != null)
                user.AvatarUrl = avatarUrl;

            await _db.SaveChangesAsync();

            var updatedUser = new
            {
                UserId = user.Id,
                Name = user.Name,
                UserName = user.UserName,
                AvatarUrl = user.AvatarUrl
            };

            // Отправка обновлений всем чатам, где участвует пользователь
            var recipientIds = await _db.ChatMembers
            .Where(cm =>
                _db.ChatMembers.Any(x =>
                    x.UserId == user.Id &&
                    x.ChatId == cm.ChatId
                )
            )
            .Select(cm => cm.UserId)
            .Distinct()
            .ToListAsync();

            foreach (var recipientId in recipientIds)
            {
                await _notificationHubContext
                    .Clients
                    .User(recipientId.ToString())
                    .SendAsync("UserUpdated", updatedUser);
            }

            return new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                UserName = user.UserName,
                AvatarUrl = user.AvatarUrl,
                IsOnline = false
            };
        }
    }
}