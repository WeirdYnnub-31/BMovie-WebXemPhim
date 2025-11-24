using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace webxemphim.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        public async Task JoinRoom(string room)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, room);
            await Clients.Group(room).SendAsync("SystemMessage", $"{GetUser()} đã tham gia phòng {room}");
        }

        public async Task LeaveRoom(string room)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, room);
            await Clients.Group(room).SendAsync("SystemMessage", $"{GetUser()} đã rời phòng {room}");
        }

        public async Task SendMessage(string room, string message)
        {
            var user = GetUser();
            await Clients.Group(room).SendAsync("ReceiveMessage", user, message, DateTimeOffset.UtcNow);
        }

        private string GetUser()
        {
            return Context.User?.Identity?.Name ?? "User";
        }
    }
}


