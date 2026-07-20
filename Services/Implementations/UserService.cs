using Messenger.Data;
using Messenger.DTO.Users;
using Messenger.Entities;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _db;

        public UserService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<UserResponse> GetCurrentUserAsync(Guid userId)
        {

            // 4. В будущем получать статус IsOnline из SignalR/Redis.

            var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
                throw new Exception("Пользователь не найден.");

            return new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl,
                IsOnline = false
            };
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
                AvatarUrl = user.AvatarUrl,
                IsOnline = false
            };
        }
        public async Task<IEnumerable<UserResponse>> SearchAsync(string? query, int page = 1, int pagesize = 10)
        {

            IQueryable<User> users = _db.Users;

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.Trim();

                users = users.Where(x =>
                    x.Name.Contains(query) ||
                    x.Email.Contains(query));
            }

            return await users
                .OrderBy(x => x.Name)
                .Skip((page - 1) * pagesize)
                .Take(pagesize)
                .Select(x => new UserResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    AvatarUrl = x.AvatarUrl,
                    IsOnline = false
                })
                .ToListAsync();
        }

        public async Task<UserResponse> UpdateAsync(Guid userId, UpdateUserRequest request)
        {
            // 5. В будущем добавить обновление аватара через AttachmentService.

            var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
                throw new Exception("Пользователь не найден.");

            if (!string.IsNullOrWhiteSpace(request.UserName))
            {
                bool exists = await _db.Users.AnyAsync(x =>
                    x.UserName == request.UserName &&
                    x.Id != userId);

                if (exists)
                    throw new Exception("Данный username уже занят.");

                user.UserName = request.UserName;
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                user.Name = request.Name;
            }

            user.AvatarUrl = request.AvatarUrl;

            await _db.SaveChangesAsync();

            return new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl,
                IsOnline = false
            };
        }
    }
}