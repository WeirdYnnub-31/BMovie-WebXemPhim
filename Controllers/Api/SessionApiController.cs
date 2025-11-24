using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using webxemphim.Services;

namespace webxemphim.Controllers.Api
{
    [ApiController]
    [Route("api/sessions")]
    [Authorize]
    public class SessionApiController : ControllerBase
    {
        private readonly SessionManagementService _sessionService;

        public SessionApiController(SessionManagementService sessionService)
        {
            _sessionService = sessionService;
        }

        /// <summary>
        /// Lấy tất cả sessions của user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSessions()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var sessions = await _sessionService.GetUserSessionsAsync(userId);
            var result = sessions.Select(s => new
            {
                s.Id,
                s.DeviceName,
                s.DeviceType,
                s.Location,
                s.IpAddress,
                s.CreatedAt,
                s.LastActivityAt,
                s.IsCurrentSession,
                s.IsActive
            });

            return Ok(result);
        }

        /// <summary>
        /// Tạo hoặc cập nhật session
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var userAgent = Request.Headers["User-Agent"].ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (deviceType, deviceName) = _sessionService.ParseUserAgent(userAgent);
            if (!string.IsNullOrEmpty(request.DeviceName))
            {
                deviceName = request.DeviceName;
            }
            if (!string.IsNullOrEmpty(request.DeviceType))
            {
                deviceType = request.DeviceType;
            }

            var session = await _sessionService.CreateOrUpdateSessionAsync(
                userId, 
                deviceName, 
                deviceType, 
                userAgent, 
                ipAddress,
                request.ExpirationHours ?? 168); // Mặc định 7 ngày

            await _sessionService.MarkCurrentSessionAsync(userId, session.SessionToken);

            return Ok(new 
            { 
                sessionToken = session.SessionToken,
                deviceName = session.DeviceName,
                deviceType = session.DeviceType,
                createdAt = session.CreatedAt
            });
        }

        /// <summary>
        /// Xóa session (logout từ xa)
        /// </summary>
        [HttpDelete("{sessionToken}")]
        public async Task<IActionResult> RevokeSession(string sessionToken)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var success = await _sessionService.RevokeSessionAsync(userId, sessionToken);
            if (!success)
            {
                return NotFound(new { error = "Session not found" });
            }

            return Ok(new { success = true, message = "Session revoked" });
        }

        /// <summary>
        /// Xóa tất cả sessions khác
        /// </summary>
        [HttpPost("revoke-others")]
        public async Task<IActionResult> RevokeOtherSessions([FromBody] RevokeOthersRequest request)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var count = await _sessionService.RevokeOtherSessionsAsync(userId, request.CurrentSessionToken);
            return Ok(new { success = true, revokedCount = count });
        }

        /// <summary>
        /// Cập nhật activity
        /// </summary>
        [HttpPost("activity")]
        public async Task<IActionResult> UpdateActivity([FromBody] UpdateActivityRequest? request = null)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            await _sessionService.UpdateActivityAsync(userId, request?.SessionToken);
            return Ok(new { success = true });
        }
    }

    public class CreateSessionRequest
    {
        public string? DeviceName { get; set; }
        public string? DeviceType { get; set; }
        public int? ExpirationHours { get; set; }
    }

    public class RevokeOthersRequest
    {
        public string CurrentSessionToken { get; set; } = string.Empty;
    }

    public class UpdateActivityRequest
    {
        public string? SessionToken { get; set; }
    }
}

