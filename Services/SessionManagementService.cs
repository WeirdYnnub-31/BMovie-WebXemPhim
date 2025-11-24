using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using webxemphim.Data;
using webxemphim.Models;

namespace webxemphim.Services
{
    /// <summary>
    /// Service để quản lý sessions đa thiết bị
    /// </summary>
    public class SessionManagementService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SessionManagementService> _logger;

        public SessionManagementService(ApplicationDbContext db, ILogger<SessionManagementService> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Tạo session mới hoặc cập nhật session hiện tại
        /// </summary>
        public async Task<UserSession> CreateOrUpdateSessionAsync(
            string userId, 
            string deviceName, 
            string deviceType, 
            string? userAgent = null, 
            string? ipAddress = null,
            int expirationHours = 24 * 7) // Mặc định 7 ngày
        {
            // Tìm session hiện tại của device này
            var existingSession = await _db.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == userId && 
                                         s.DeviceName == deviceName && 
                                         s.IsActive);

            if (existingSession != null)
            {
                // Cập nhật session hiện tại
                existingSession.LastActivityAt = DateTime.UtcNow;
                existingSession.ExpiresAt = DateTime.UtcNow.AddHours(expirationHours);
                existingSession.UserAgent = userAgent;
                existingSession.IpAddress = ipAddress;
                await _db.SaveChangesAsync();
                return existingSession;
            }

            // Tạo session mới
            var sessionToken = GenerateSessionToken();
            var session = new UserSession
            {
                UserId = userId,
                SessionToken = sessionToken,
                DeviceName = deviceName,
                DeviceType = deviceType,
                UserAgent = userAgent,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(expirationHours),
                IsActive = true
            };

            _db.UserSessions.Add(session);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Created new session for user {UserId} on device {DeviceName}", userId, deviceName);
            return session;
        }

        /// <summary>
        /// Lấy tất cả sessions của user
        /// </summary>
        public async Task<List<UserSession>> GetUserSessionsAsync(string userId)
        {
            return await _db.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderByDescending(s => s.LastActivityAt)
                .ToListAsync();
        }

        /// <summary>
        /// Đánh dấu session hiện tại
        /// </summary>
        public async Task MarkCurrentSessionAsync(string userId, string sessionToken)
        {
            // Bỏ đánh dấu tất cả sessions khác
            var allSessions = await _db.UserSessions
                .Where(s => s.UserId == userId)
                .ToListAsync();

            foreach (var session in allSessions)
            {
                session.IsCurrentSession = session.SessionToken == sessionToken;
            }

            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Xóa session (logout từ xa)
        /// </summary>
        public async Task<bool> RevokeSessionAsync(string userId, string sessionToken)
        {
            var session = await _db.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.SessionToken == sessionToken);

            if (session == null) return false;

            session.IsActive = false;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Revoked session {SessionToken} for user {UserId}", sessionToken, userId);
            return true;
        }

        /// <summary>
        /// Xóa tất cả sessions khác (giữ lại session hiện tại)
        /// </summary>
        public async Task<int> RevokeOtherSessionsAsync(string userId, string currentSessionToken)
        {
            var otherSessions = await _db.UserSessions
                .Where(s => s.UserId == userId && 
                           s.SessionToken != currentSessionToken && 
                           s.IsActive)
                .ToListAsync();

            foreach (var session in otherSessions)
            {
                session.IsActive = false;
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Revoked {Count} other sessions for user {UserId}", otherSessions.Count, userId);
            return otherSessions.Count;
        }

        /// <summary>
        /// Xóa sessions đã hết hạn
        /// </summary>
        public async Task<int> CleanupExpiredSessionsAsync()
        {
            var expiredSessions = await _db.UserSessions
                .Where(s => s.IsActive && s.ExpiresAt.HasValue && s.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            foreach (var session in expiredSessions)
            {
                session.IsActive = false;
            }

            await _db.SaveChangesAsync();
            return expiredSessions.Count;
        }

        /// <summary>
        /// Cập nhật last activity
        /// </summary>
        public async Task UpdateActivityAsync(string userId, string? sessionToken = null)
        {
            var query = _db.UserSessions.Where(s => s.UserId == userId && s.IsActive);

            if (!string.IsNullOrEmpty(sessionToken))
            {
                query = query.Where(s => s.SessionToken == sessionToken);
            }

            var sessions = await query.ToListAsync();
            foreach (var session in sessions)
            {
                session.LastActivityAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Lấy thông tin device từ User-Agent
        /// </summary>
        public (string DeviceType, string DeviceName) ParseUserAgent(string? userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return ("Unknown", "Unknown Device");

            userAgent = userAgent.ToLower();

            if (userAgent.Contains("mobile") || userAgent.Contains("android") || userAgent.Contains("iphone"))
            {
                if (userAgent.Contains("tablet") || userAgent.Contains("ipad"))
                    return ("Tablet", "Tablet");
                return ("Mobile", "Mobile Device");
            }

            if (userAgent.Contains("tablet") || userAgent.Contains("ipad"))
                return ("Tablet", "Tablet");

            return ("Desktop", "Desktop");
        }

        private string GenerateSessionToken()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }
    }
}

