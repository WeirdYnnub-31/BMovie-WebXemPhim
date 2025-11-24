using webxemphim.Data;

namespace webxemphim.Models
{
    public class ApiKey
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty; // API key value
        public string Name { get; set; } = string.Empty; // Tên/key description
        public string? UserId { get; set; } // User tạo key (nếu có)
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; } // Ngày hết hạn (null = không hết hạn)
        public int RateLimit { get; set; } = 1000; // Số request mỗi giờ
        public string? AllowedIps { get; set; } // IP được phép (comma-separated, null = tất cả)
        public List<string> AllowedEndpoints { get; set; } = new(); // Endpoints được phép truy cập
        public long RequestCount { get; set; } = 0; // Tổng số request đã thực hiện
        public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ApplicationUser? User { get; set; }
    }
}

