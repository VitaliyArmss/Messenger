namespace Messenger.DTO.Users
{
    public class UpdateUserRequest
    {
        public string Name { get; set; } = "";

		public string UserName { get; set; } = "";

		public string? AvatarUrl { get; set; }
    }
}
