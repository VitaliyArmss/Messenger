using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Messenger.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public async Task SendNotificationToUser(string userId, object message)
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", message);
        }
    }
}