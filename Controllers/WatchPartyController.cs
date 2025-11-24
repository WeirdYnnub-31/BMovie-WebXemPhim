using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Data;
using webxemphim.Models;
using webxemphim.Services;

namespace webxemphim.Controllers
{
    [Authorize]
    public class WatchPartyController : Controller
    {
        private readonly WatchPartyService _watchPartyService;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<WatchPartyController> _logger;

        public WatchPartyController(
            WatchPartyService watchPartyService,
            ApplicationDbContext db,
            ILogger<WatchPartyController> logger)
        {
            _watchPartyService = watchPartyService;
            _db = db;
            _logger = logger;
        }

        [HttpGet("watchparty/create/{movieId}")]
        public async Task<IActionResult> Create(int movieId, string? roomName = null)
        {
            var movie = await _db.Movies.FindAsync(movieId);
            if (movie == null)
            {
                return NotFound();
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var defaultRoomName = roomName ?? $"Xem {movie.Title} cùng nhau";
            var watchParty = await _watchPartyService.CreateWatchPartyAsync(userId, movieId, defaultRoomName);

            if (watchParty == null)
            {
                TempData["Error"] = "Không thể tạo watch party.";
                return RedirectToAction("Detail", "Movies", new { slug = movie.Slug });
            }

            return RedirectToAction("Room", new { roomId = watchParty.RoomId });
        }

        [HttpGet("watchparty/{roomId}")]
        public async Task<IActionResult> Room(string roomId)
        {
            var watchParty = await _watchPartyService.GetWatchPartyAsync(roomId);
            if (watchParty == null || !watchParty.IsActive)
            {
                return NotFound();
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Join party if not already joined
            await _watchPartyService.JoinWatchPartyAsync(roomId, userId);

            ViewBag.RoomId = roomId;
            ViewBag.IsHost = watchParty.HostId == userId;
            ViewBag.Movie = watchParty.Movie;
            ViewBag.Participants = watchParty.Participants.Where(p => p.IsConnected).ToList();
            ViewBag.Messages = watchParty.Messages.OrderBy(m => m.CreatedAt).Take(50).ToList();

            return View(watchParty);
        }

        [HttpPost("watchparty/{roomId}/leave")]
        public async Task<IActionResult> Leave(string roomId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            await _watchPartyService.LeaveWatchPartyAsync(roomId, userId);
            return RedirectToAction("Index", "Movies");
        }

        [HttpPost("watchparty/{roomId}/end")]
        public async Task<IActionResult> End(string roomId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _watchPartyService.EndWatchPartyAsync(roomId, userId);
            if (!success)
            {
                TempData["Error"] = "Chỉ host mới có thể kết thúc watch party.";
            }

            return RedirectToAction("Index", "Movies");
        }

        [HttpPost("watchparty/{roomId}/invite")]
        public async Task<IActionResult> Invite(string roomId, string inviteeId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _watchPartyService.InviteUserAsync(roomId, userId, inviteeId);
            if (!success)
            {
                return BadRequest(new { error = "Không thể gửi lời mời." });
            }

            return Ok(new { success = true, message = "Đã gửi lời mời." });
        }

        [HttpGet("watchparty")]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var watchParties = await _db.WatchParties
                .Include(wp => wp.Movie)
                .Include(wp => wp.Host)
                .Include(wp => wp.Participants)
                .Where(wp => wp.IsActive && (wp.HostId == userId || wp.Participants.Any(p => p.UserId == userId && p.IsConnected)))
                .OrderByDescending(wp => wp.CreatedAt)
                .Take(20)
                .ToListAsync();

            return View(watchParties);
        }
    }
}

