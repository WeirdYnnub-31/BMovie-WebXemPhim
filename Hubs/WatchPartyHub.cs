using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace webxemphim.Hubs
{
    [Authorize]
    public class WatchPartyHub : Hub
    {
        public async Task JoinWatchParty(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"watchparty-{roomId}");
            await Clients.Group($"watchparty-{roomId}").SendAsync("UserJoined", Context.User?.Identity?.Name);
        }

        public async Task LeaveWatchParty(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"watchparty-{roomId}");
            await Clients.Group($"watchparty-{roomId}").SendAsync("UserLeft", Context.User?.Identity?.Name);
        }

        public async Task UpdatePlaybackState(string roomId, double currentTime, bool isPlaying)
        {
            // Chỉ host mới được cập nhật playback state
            // State sẽ được broadcast bởi WatchPartyService
            await Clients.GroupExcept($"watchparty-{roomId}", Context.ConnectionId)
                .SendAsync("PlaybackStateChanged", new { currentTime, isPlaying });
        }

        public async Task SendMessage(string roomId, string message)
        {
            var username = Context.User?.Identity?.Name ?? "User";
            await Clients.Group($"watchparty-{roomId}").SendAsync("ReceiveMessage", new
            {
                UserId = Context.UserIdentifier,
                Username = username,
                Message = message,
                CreatedAt = DateTime.UtcNow
            });
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Có thể thêm logic để xử lý khi user disconnect
            await base.OnDisconnectedAsync(exception);
        }
    }
}

