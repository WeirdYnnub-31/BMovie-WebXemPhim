using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using webxemphim.Data;
using webxemphim.Hubs;
using webxemphim.Models;

namespace webxemphim.Services
{
    public class WatchPartyService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHubContext<WatchPartyHub> _hubContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<WatchPartyService> _logger;

        public WatchPartyService(
            ApplicationDbContext db,
            IHubContext<WatchPartyHub> hubContext,
            IServiceProvider serviceProvider,
            ILogger<WatchPartyService> logger)
        {
            _db = db;
            _hubContext = hubContext;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Tạo watch party mới
        /// </summary>
        public async Task<WatchParty?> CreateWatchPartyAsync(string hostId, int movieId, string roomName, int maxParticipants = 10)
        {
            var roomId = GenerateRoomId();

            var watchParty = new WatchParty
            {
                RoomId = roomId,
                HostId = hostId,
                MovieId = movieId,
                RoomName = roomName,
                MaxParticipants = maxParticipants,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.WatchParties.Add(watchParty);

            // Thêm host làm participant
            var hostParticipant = new WatchPartyParticipant
            {
                WatchPartyId = watchParty.Id,
                UserId = hostId,
                JoinedAt = DateTime.UtcNow,
                IsConnected = true
            };
            _db.WatchPartyParticipants.Add(hostParticipant);

            await _db.SaveChangesAsync();

            _logger.LogInformation("Created watch party {RoomId} by user {HostId}", roomId, hostId);
            return watchParty;
        }

        /// <summary>
        /// Tham gia watch party
        /// </summary>
        public async Task<bool> JoinWatchPartyAsync(string roomId, string userId)
        {
            var watchParty = await _db.WatchParties
                .Include(wp => wp.Participants)
                .FirstOrDefaultAsync(wp => wp.RoomId == roomId && wp.IsActive);

            if (watchParty == null) return false;

            // Kiểm tra số lượng participants
            if (watchParty.Participants.Count >= watchParty.MaxParticipants)
            {
                return false;
            }

            // Kiểm tra đã tham gia chưa
            var existingParticipant = watchParty.Participants.FirstOrDefault(p => p.UserId == userId);
            if (existingParticipant != null)
            {
                existingParticipant.IsConnected = true;
                await _db.SaveChangesAsync();
                return true;
            }

            // Thêm participant mới
            var participant = new WatchPartyParticipant
            {
                WatchPartyId = watchParty.Id,
                UserId = userId,
                JoinedAt = DateTime.UtcNow,
                IsConnected = true
            };

            _db.WatchPartyParticipants.Add(participant);
            await _db.SaveChangesAsync();

            // Gửi thông báo cho các thành viên khác
            await _hubContext.Clients.Group($"watchparty-{roomId}")
                .SendAsync("UserJoined", userId);

            _logger.LogInformation("User {UserId} joined watch party {RoomId}", userId, roomId);
            return true;
        }

        /// <summary>
        /// Rời watch party
        /// </summary>
        public async Task<bool> LeaveWatchPartyAsync(string roomId, string userId)
        {
            var watchParty = await _db.WatchParties
                .Include(wp => wp.Participants)
                .FirstOrDefaultAsync(wp => wp.RoomId == roomId);

            if (watchParty == null) return false;

            var participant = watchParty.Participants.FirstOrDefault(p => p.UserId == userId);
            if (participant != null)
            {
                participant.IsConnected = false;
                await _db.SaveChangesAsync();

                // Gửi thông báo cho các thành viên khác
                await _hubContext.Clients.Group($"watchparty-{roomId}")
                    .SendAsync("UserLeft", userId);

                // Nếu host rời và không còn ai, kết thúc party
                if (watchParty.HostId == userId && !watchParty.Participants.Any(p => p.IsConnected && p.UserId != userId))
                {
                    await EndWatchPartyAsync(roomId, userId);
                }
            }

            return true;
        }

        /// <summary>
        /// Kết thúc watch party
        /// </summary>
        public async Task<bool> EndWatchPartyAsync(string roomId, string userId)
        {
            var watchParty = await _db.WatchParties
                .FirstOrDefaultAsync(wp => wp.RoomId == roomId && wp.HostId == userId);

            if (watchParty == null) return false;

            watchParty.IsActive = false;
            watchParty.EndedAt = DateTime.UtcNow;

            // Đánh dấu tất cả participants là disconnected
            var participants = await _db.WatchPartyParticipants
                .Where(p => p.WatchPartyId == watchParty.Id)
                .ToListAsync();

            foreach (var participant in participants)
            {
                participant.IsConnected = false;
            }

            await _db.SaveChangesAsync();

            // Gửi thông báo kết thúc
            await _hubContext.Clients.Group($"watchparty-{roomId}")
                .SendAsync("WatchPartyEnded");

            _logger.LogInformation("Watch party {RoomId} ended by {UserId}", roomId, userId);
            return true;
        }

        /// <summary>
        /// Cập nhật trạng thái phát video (play/pause/time)
        /// </summary>
        public async Task<bool> UpdatePlaybackStateAsync(string roomId, string userId, double currentTime, bool isPlaying)
        {
            var watchParty = await _db.WatchParties
                .FirstOrDefaultAsync(wp => wp.RoomId == roomId && wp.IsActive);

            if (watchParty == null || watchParty.HostId != userId) return false;

            watchParty.CurrentTime = currentTime;
            watchParty.IsPlaying = isPlaying;

            await _db.SaveChangesAsync();

            // Broadcast state change đến tất cả participants (trừ host)
            await _hubContext.Clients.GroupExcept($"watchparty-{roomId}", new[] { userId })
                .SendAsync("PlaybackStateChanged", new { currentTime, isPlaying });

            return true;
        }

        /// <summary>
        /// Gửi message trong watch party
        /// </summary>
        public async Task<bool> SendMessageAsync(string roomId, string userId, string message)
        {
            var watchParty = await _db.WatchParties
                .FirstOrDefaultAsync(wp => wp.RoomId == roomId && wp.IsActive);

            if (watchParty == null) return false;

            // Kiểm tra user có trong party không
            var participant = await _db.WatchPartyParticipants
                .FirstOrDefaultAsync(p => p.WatchPartyId == watchParty.Id && p.UserId == userId && p.IsConnected);

            if (participant == null) return false;

            var chatMessage = new WatchPartyMessage
            {
                WatchPartyId = watchParty.Id,
                UserId = userId,
                Message = message,
                CreatedAt = DateTime.UtcNow
            };

            _db.WatchPartyMessages.Add(chatMessage);
            await _db.SaveChangesAsync();

            // Broadcast message
            await _hubContext.Clients.Group($"watchparty-{roomId}")
                .SendAsync("ReceiveMessage", new
                {
                    chatMessage.Id,
                    UserId = userId,
                    Message = message,
                    CreatedAt = chatMessage.CreatedAt
                });

            return true;
        }

        /// <summary>
        /// Lấy thông tin watch party
        /// </summary>
        public async Task<WatchParty?> GetWatchPartyAsync(string roomId)
        {
            return await _db.WatchParties
                .Include(wp => wp.Movie)
                .Include(wp => wp.Host)
                .Include(wp => wp.Participants)
                    .ThenInclude(p => p.User)
                .Include(wp => wp.Messages.OrderByDescending(m => m.CreatedAt).Take(50))
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(wp => wp.RoomId == roomId && wp.IsActive);
        }

        /// <summary>
        /// Mời user tham gia watch party
        /// </summary>
        public async Task<bool> InviteUserAsync(string roomId, string hostId, string inviteeId)
        {
            var watchParty = await _db.WatchParties
                .Include(wp => wp.Movie)
                .FirstOrDefaultAsync(wp => wp.RoomId == roomId && wp.HostId == hostId && wp.IsActive);

            if (watchParty == null) return false;

            var host = await _db.Users.FindAsync(hostId);
            var hostName = host?.UserName ?? "Người dùng";

            // Gửi thông báo mời (sử dụng service provider để tránh circular dependency)
            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
            await notificationService.NotifyWatchPartyInviteAsync(
                inviteeId,
                hostName,
                watchParty.RoomName,
                roomId,
                watchParty.MovieId
            );

            return true;
        }

        /// <summary>
        /// Lấy danh sách messages của watch party
        /// </summary>
        public async Task<List<WatchPartyMessage>> GetMessagesAsync(string roomId, int limit = 50)
        {
            var watchParty = await _db.WatchParties
                .FirstOrDefaultAsync(wp => wp.RoomId == roomId);

            if (watchParty == null) return new List<WatchPartyMessage>();

            return await _db.WatchPartyMessages
                .Include(m => m.User)
                .Where(m => m.WatchPartyId == watchParty.Id)
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        private string GenerateRoomId()
        {
            return Guid.NewGuid().ToString("N")[..8].ToUpper();
        }
    }
}

