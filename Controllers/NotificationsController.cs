using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using webxemphim.Services;

namespace webxemphim.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly NotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            NotificationService notificationService,
            ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> Index(int page = 1, bool? unreadOnly = null)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var (notifications, unreadCount) = await _notificationService.GetUserNotificationsAsync(
                userId,
                page,
                20,
                unreadOnly
            );

            ViewBag.UnreadCount = unreadCount;
            ViewBag.CurrentPage = page;
            ViewBag.UnreadOnly = unreadOnly;

            return View(notifications);
        }

        [HttpPost("notifications/{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _notificationService.MarkAsReadAsync(id, userId);
            if (!success)
            {
                return NotFound();
            }

            return Ok(new { success = true });
        }

        [HttpPost("notifications/mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var count = await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(new { success = true, count });
        }

        [HttpDelete("notifications/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _notificationService.DeleteNotificationAsync(id, userId);
            if (!success)
            {
                return NotFound();
            }

            return Ok(new { success = true });
        }

        [HttpGet("api/notifications/unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var (_, unreadCount) = await _notificationService.GetUserNotificationsAsync(userId, 1, 1, true);
            return Ok(new { unreadCount });
        }
    }
}

