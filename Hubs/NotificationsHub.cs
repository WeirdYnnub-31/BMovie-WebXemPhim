using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace webxemphim.Hubs
{
    public class NotificationsHub : Hub
    {
        private static int _onlineCount = 0;

        public override async Task OnConnectedAsync()
        {
            Interlocked.Increment(ref _onlineCount);
            await Clients.All.SendAsync("OnlineCountUpdated", _onlineCount);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Interlocked.Decrement(ref _onlineCount);
            await Clients.All.SendAsync("OnlineCountUpdated", _onlineCount);
            await base.OnDisconnectedAsync(exception);
        }
    }
}


