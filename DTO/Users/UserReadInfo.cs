namespace Messenger.DTO.Users
{
    public class UserReadInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string? AvatarUrl { get; set; }
        public DateTime ReadAt { get; set; }
    }
}