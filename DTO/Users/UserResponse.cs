namespace Messenger.DTO.Users
{
    public class UserResponse
    {
        public Guid Id { get; set; }

        public string Username { get; set; } = "";

        public string? AvatarUrl { get; set; }

        public bool IsOnline { get; set; }
    }
}
