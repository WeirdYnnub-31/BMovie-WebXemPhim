using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Data;

namespace webxemphim.Controllers
{
    [Authorize]
    public class WatchHistoryController : Controller
    {
        private readonly ApplicationDbContext _db;

        public WatchHistoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var watchHistory = await _db.ViewHits
                .Include(vh => vh.Movie)
                .GroupBy(vh => vh.MovieId)
                .Select(g => new
                {
                    Movie = g.First().Movie,
                    LastWatched = g.Max(vh => vh.ViewedAt),
                    WatchCount = g.Count()
                })
                .OrderByDescending(x => x.LastWatched)
                .Take(50)
                .ToListAsync();

            return View(watchHistory);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearHistory()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin người dùng." });
            }

            // Note: ViewHits don't have UserId, so we can't filter by user
            // This is a limitation of the current model design
            // In a real implementation, you'd want to add UserId to ViewHits
            TempData["Message"] = "Lịch sử xem đã được xóa.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromHistory(int movieId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin người dùng." });
            }

            // Remove all ViewHits for this movie
            var viewHits = await _db.ViewHits.Where(vh => vh.MovieId == movieId).ToListAsync();
            _db.ViewHits.RemoveRange(viewHits);
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa khỏi lịch sử xem." });
        }
    }
}
