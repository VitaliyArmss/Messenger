namespace Messenger.DTO.Users
{
    public class UpdateUserRequest
    {
        public string Username { get; set; } = "";

        public string? AvatarUrl { get; set; }
    }
}
