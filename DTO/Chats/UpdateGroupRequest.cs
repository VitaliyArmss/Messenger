namespace Messenger.DTO.Chats
{
    public class UpdateGroupRequest
    {
        public string? Name { get; set; }
        public IFormFile? AvatarFile { get; set; }
    }
}