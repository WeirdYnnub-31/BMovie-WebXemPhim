using Microsoft.EntityFrameworkCore;
using webxemphim.Data;
using webxemphim.Models;

namespace webxemphim.Services
{
    /// <summary>
    /// Service để ghi log và audit các hoạt động của hệ thống
    /// </summary>
    public class AuditLogService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(ApplicationDbContext db, ILogger<AuditLogService> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Ghi log hoạt động
        /// </summary>
        public async Task LogAsync(string action, string? userId = null, string? entityType = null, int? entityId = null, string? details = null, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    Action = action,
                    UserId = userId,
                    EntityType = entityType,
                    EntityId = entityId,
                    Details = details,
                    IpAddress = ipAddress,
                    CreatedAt = DateTime.UtcNow
                };

                _db.AuditLogs.Add(auditLog);
                await _db.SaveChangesAsync();

                // Cũng ghi vào application logger
                _logger.LogInformation("Audit: {Action} by {UserId} on {EntityType}/{EntityId}", action, userId, entityType, entityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit log");
            }
        }

        /// <summary>
        /// Ghi log đăng nhập
        /// </summary>
        public async Task LogLoginAsync(string userId, bool success, string? ipAddress = null, string? reason = null)
        {
            await LogAsync(
                success ? "LOGIN_SUCCESS" : "LOGIN_FAILED",
                userId,
                "User",
                null,
                reason,
                ipAddress
            );
        }

        /// <summary>
        /// Ghi log CRUD operations
        /// </summary>
        public async Task LogCrudAsync(string action, string entityType, int entityId, string? userId = null, string? details = null, string? ipAddress = null)
        {
            await LogAsync(action, userId, entityType, entityId, details, ipAddress);
        }

        /// <summary>
        /// Lấy audit logs với filter
        /// </summary>
        public async Task<List<AuditLog>> GetLogsAsync(string? userId = null, string? action = null, string? entityType = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50)
        {
            var query = _db.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(l => l.UserId == userId);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(l => l.Action == action);

            if (!string.IsNullOrEmpty(entityType))
                query = query.Where(l => l.EntityType == entityType);

            if (from.HasValue)
                query = query.Where(l => l.CreatedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(l => l.CreatedAt <= to.Value);

            return await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }

    /// <summary>
    /// Model cho Audit Log
    /// </summary>
    public class AuditLog
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty; // LOGIN_SUCCESS, LOGIN_FAILED, CREATE, UPDATE, DELETE, etc.
        public string? UserId { get; set; }
        public string? EntityType { get; set; } // Movie, User, Comment, etc.
        public int? EntityId { get; set; }
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

