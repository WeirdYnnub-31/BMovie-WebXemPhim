using webxemphim.Data;

namespace webxemphim.Models
{
    public class Achievement
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public AchievementType Type { get; set; }
        public int Requirement { get; set; } // Số lượng cần đạt (ví dụ: xem 100 phim)
        public int RewardCoins { get; set; } = 0; // Coin thưởng khi đạt được
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public List<UserAchievement> UserAchievements { get; set; } = new();
    }

    public class UserAchievement
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int AchievementId { get; set; }
        public int Progress { get; set; } = 0; // Tiến độ hiện tại
        public bool IsUnlocked { get; set; } = false;
        public DateTime? UnlockedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ApplicationUser? User { get; set; }
        public Achievement? Achievement { get; set; }
    }

    public enum AchievementType
    {
        MoviesWatched,      // Xem N phim
        ReviewsWritten,     // Viết N review
        CommentsWritten,    // Viết N comment
        DaysActive,         // Hoạt động N ngày liên tiếp
        MoviesRated,        // Đánh giá N phim
        WatchPartyHosted,   // Tạo N watch party
        FriendsFollowed,    // Follow N bạn
        CoinsEarned,        // Kiếm N coin
        PerfectRating,      // Đánh giá 5 sao cho N phim
        MarathonWatcher     // Xem phim liên tục N giờ
    }
}

