using webxemphim.Data;

namespace webxemphim.Models
{
    /// <summary>
    /// Model để quản lý sessions đa thiết bị
    /// </summary>
    public class UserSession
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string SessionToken { get; set; } = string.Empty; // Unique token cho mỗi session
        public string DeviceName { get; set; } = string.Empty; // Tên thiết bị
        public string DeviceType { get; set; } = string.Empty; // Desktop, Mobile, Tablet
        public string? UserAgent { get; set; }
        public string? IpAddress { get; set; }
        public string? Location { get; set; } // Có thể lấy từ IP geolocation
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; } // Thời gian hết hạn session
        public bool IsActive { get; set; } = true;
        public bool IsCurrentSession { get; set; } = false; // Session hiện tại
        
        // Navigation properties
        public ApplicationUser? User { get; set; }
    }
}

