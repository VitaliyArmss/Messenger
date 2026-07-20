namespace Messenger.DTO.Users
{
    public class UserResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = "";

        public string? AvatarUrl { get; set; }

        public bool IsOnline { get; set; }
    }
}
