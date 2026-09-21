namespace Messenger.DTO.Users
{
    public class UpdateUserRequest
    {
        public string Name { get; set; } = "";

        public string UserName { get; set; } = "";

        public IFormFile? AvatarFile { get; set; }
    }
}
