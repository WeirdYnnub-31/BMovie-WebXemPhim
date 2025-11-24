using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using webxemphim.Data;
using webxemphim.Hubs;
using webxemphim.Models;

namespace webxemphim.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHubContext<NotificationsHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ApplicationDbContext db,
            IHubContext<NotificationsHub> hubContext,
            ILogger<NotificationService> logger)
        {
            _db = db;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Tạo thông báo mới
        /// </summary>
        public async Task<int> CreateNotificationAsync(
            string userId,
            string title,
            string message,
            NotificationType type,
            string? link = null,
            int? movieId = null,
            int? commentId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Link = link,
                MovieId = movieId,
                CommentId = commentId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();

            // Gửi real-time notification qua SignalR
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", new
            {
                notification.Id,
                notification.Title,
                notification.Message,
                notification.Type,
                notification.Link,
                notification.MovieId,
                notification.CommentId,
                notification.CreatedAt
            });

            _logger.LogInformation("Created notification {NotificationId} for user {UserId}", notification.Id, userId);
            return notification.Id;
        }

        /// <summary>
        /// Đánh dấu thông báo là đã đọc
        /// </summary>
        public async Task<bool> MarkAsReadAsync(int notificationId, string userId)
        {
            var notification = await _db.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null) return false;

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return true;
        }

        /// <summary>
        /// Đánh dấu tất cả thông báo là đã đọc
        /// </summary>
        public async Task<int> MarkAllAsReadAsync(string userId)
        {
            var unreadNotifications = await _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return unreadNotifications.Count;
        }

        /// <summary>
        /// Lấy danh sách thông báo của user
        /// </summary>
        public async Task<(List<Notification> Notifications, int UnreadCount)> GetUserNotificationsAsync(
            string userId,
            int page = 1,
            int pageSize = 20,
            bool? unreadOnly = null)
        {
            var query = _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .AsQueryable();

            if (unreadOnly == true)
            {
                query = query.Where(n => !n.IsRead);
            }

            var totalCount = await query.CountAsync();
            var notifications = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var unreadCount = await _db.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return (notifications, unreadCount);
        }

        /// <summary>
        /// Xóa thông báo
        /// </summary>
        public async Task<bool> DeleteNotificationAsync(int notificationId, string userId)
        {
            var notification = await _db.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null) return false;

            _db.Notifications.Remove(notification);
            await _db.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Thông báo phim mới phù hợp sở thích
        /// </summary>
        public async Task NotifyNewMovieAsync(string userId, int movieId, string movieTitle)
        {
            await CreateNotificationAsync(
                userId,
                "Phim mới phù hợp với bạn",
                $"Phim '{movieTitle}' có thể bạn sẽ thích",
                NotificationType.MovieNew,
                link: $"/Movies/Detail/{movieId}",
                movieId: movieId
            );
        }

        /// <summary>
        /// Thông báo phim sắp ra mắt
        /// </summary>
        public async Task NotifyUpcomingMovieAsync(string userId, int movieId, string movieTitle, DateTime releaseDate)
        {
            await CreateNotificationAsync(
                userId,
                "Phim sắp ra mắt",
                $"Phim '{movieTitle}' sẽ ra mắt vào {releaseDate:dd/MM/yyyy}",
                NotificationType.MovieUpcoming,
                link: $"/Movies/Detail/{movieId}",
                movieId: movieId
            );
        }

        /// <summary>
        /// Thông báo có người reply comment
        /// </summary>
        public async Task NotifyCommentReplyAsync(string userId, int commentId, string replyerName, int movieId)
        {
            await CreateNotificationAsync(
                userId,
                "Có người trả lời bình luận của bạn",
                $"{replyerName} đã trả lời bình luận của bạn",
                NotificationType.CommentReply,
                link: $"/Movies/Detail/{movieId}#comment-{commentId}",
                movieId: movieId,
                commentId: commentId
            );
        }

        /// <summary>
        /// Thông báo lời mời watch party
        /// </summary>
        public async Task NotifyWatchPartyInviteAsync(string userId, string hostName, string roomName, string roomId, int movieId)
        {
            await CreateNotificationAsync(
                userId,
                "Lời mời xem phim cùng nhau",
                $"{hostName} mời bạn tham gia xem phim: {roomName}",
                NotificationType.WatchPartyInvite,
                link: $"/WatchParty/{roomId}",
                movieId: movieId
            );
        }

        /// <summary>
        /// Thông báo đạt thành tích mới
        /// </summary>
        public async Task NotifyAchievementAsync(string userId, string achievementName, int rewardCoins = 0)
        {
            var message = rewardCoins > 0
                ? $"Chúc mừng! Bạn đã đạt thành tích '{achievementName}' và nhận {rewardCoins} coin!"
                : $"Chúc mừng! Bạn đã đạt thành tích '{achievementName}'!";

            await CreateNotificationAsync(
                userId,
                "Thành tích mới",
                message,
                NotificationType.Achievement,
                link: "/Profile/Achievements"
            );
        }
    }
}

