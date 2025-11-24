using webxemphim.Data;

namespace webxemphim.Models
{
    public class Feedback
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public FeedbackType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? MovieId { get; set; } // Nếu báo lỗi phim
        public string? MovieSourceUrl { get; set; } // URL nguồn video có vấn đề
        public FeedbackStatus Status { get; set; } = FeedbackStatus.Pending;
        public string? AdminResponse { get; set; }
        public string? AdminId { get; set; } // Admin xử lý
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
        public int Priority { get; set; } = 1; // 1: Low, 2: Medium, 3: High, 4: Critical

        // Navigation properties
        public ApplicationUser? User { get; set; }
        public Movie? Movie { get; set; }
        public ApplicationUser? Admin { get; set; }
    }

    public enum FeedbackType
    {
        MovieError,         // Phim không phát được
        VideoQuality,       // Chất lượng video kém
        SubtitleError,      // Lỗi phụ đề
        AudioError,         // Lỗi âm thanh
        Suggestion,         // Góp ý cải tiến
        BugReport,          // Báo lỗi website
        FeatureRequest,     // Yêu cầu tính năng mới
        Other               // Khác
    }

    public enum FeedbackStatus
    {
        Pending,            // Chờ xử lý
        InProgress,         // Đang xử lý
        Resolved,           // Đã xử lý
        Rejected,           // Từ chối
        Duplicate           // Trùng lặp
    }
}

