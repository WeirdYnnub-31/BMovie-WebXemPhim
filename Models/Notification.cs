using webxemphim.Data;

namespace webxemphim.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; } = false;
        public string? Link { get; set; } // URL để điều hướng khi click
        public int? MovieId { get; set; } // ID phim liên quan (nếu có)
        public int? CommentId { get; set; } // ID comment liên quan (nếu có)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }

        // Navigation properties
        public ApplicationUser? User { get; set; }
        public Movie? Movie { get; set; }
        public Comment? Comment { get; set; }
    }

    public enum NotificationType
    {
        MovieNew,           // Phim mới phù hợp sở thích
        MovieUpcoming,      // Phim sắp ra mắt
        CommentReply,       // Có người reply comment
        CommentLike,        // Có người like comment
        FriendActivity,     // Hoạt động của bạn bè
        Achievement,        // Đạt thành tích mới
        System,             // Thông báo hệ thống
        WatchPartyInvite    // Lời mời watch party
    }
}

