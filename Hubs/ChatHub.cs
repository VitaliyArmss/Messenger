using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Messenger.Hubs
{
    [Authorize] // Требуем авторизацию через JWT
    public class ChatHub : Hub
    {
        // Присоединиться к группе чата
        public async Task JoinGroup(string chatId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
        }

        // Покинуть группу чата
        public async Task LeaveGroup(string chatId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId);
        }

        // Отправка сообщения через хаб
        public async Task SendMessage(string chatId, object message)
        {
            await Clients.Group(chatId).SendAsync("ReceiveMessage", message);
        }
    }
}