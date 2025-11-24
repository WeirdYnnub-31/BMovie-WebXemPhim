using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Data;
using webxemphim.Services;

namespace webxemphim.Controllers.Api
{
    [ApiController]
    [Route("api/admin/sync")] 
    public class AdminSyncController : ControllerBase
    {
        private readonly BmovieSyncBackgroundService _svc;
        private readonly IConfiguration _cfg;
        private readonly ApplicationDbContext _db;
        
        public AdminSyncController(BmovieSyncBackgroundService svc, IConfiguration cfg, ApplicationDbContext db)
        {
            _svc = svc;
            _cfg = cfg;
            _db = db;
        }

        [HttpPost("bmovie")] 
        [AllowAnonymous]
        public async Task<IActionResult> RunBmovieSync()
        {
            var apiKey = _cfg["SyncApi:ApiKey"];
            var headerKey = Request.Headers["X-Admin-ApiKey"].FirstOrDefault();
            var isApiKeyValid = !string.IsNullOrWhiteSpace(apiKey) && apiKey == headerKey;
            var isAdmin = User?.Identity?.IsAuthenticated == true && User.IsInRole("Admin");
            if (!isApiKeyValid && !isAdmin) return Unauthorized();

            await _svc.SyncOnce(HttpContext.RequestAborted);
            return Ok(new { success = true });
        }

        [HttpPost("delete-all-movies")]
        [AllowAnonymous]
        public async Task<IActionResult> DeleteAllMovies()
        {
            var apiKey = _cfg["SyncApi:ApiKey"];
            var headerKey = Request.Headers["X-Admin-ApiKey"].FirstOrDefault();
            var isApiKeyValid = !string.IsNullOrWhiteSpace(apiKey) && apiKey == headerKey;
            var isAdmin = User?.Identity?.IsAuthenticated == true && User.IsInRole("Admin");
            if (!isApiKeyValid && !isAdmin) return Unauthorized();

            try
            {
                // Lấy danh sách tất cả Movie IDs trước
                var movieIds = await _db.Movies.Select(m => m.Id).ToListAsync();
                
                if (!movieIds.Any())
                {
                    return Ok(new { success = true, message = "Không có phim nào trong database để xóa.", deletedCount = 0 });
                }

                var movieCount = movieIds.Count;

                // Xóa dữ liệu liên quan trước (các bảng không có cascade delete)
                // 1. Xóa MovieGenres (bảng trung gian)
                var movieGenres = await _db.MovieGenres.Where(mg => movieIds.Contains(mg.MovieId)).ToListAsync();
                if (movieGenres.Any())
                {
                    _db.MovieGenres.RemoveRange(movieGenres);
                    await _db.SaveChangesAsync();
                }

                // 2. Xóa ViewHits
                var viewHits = await _db.ViewHits.Where(vh => movieIds.Contains(vh.MovieId)).ToListAsync();
                if (viewHits.Any())
                {
                    _db.ViewHits.RemoveRange(viewHits);
                    await _db.SaveChangesAsync();
                }

                // 3. Xóa UserInventoryItems liên quan đến phim
                var userInventoryItems = await _db.UserInventoryItems.Where(ui => ui.MovieId.HasValue && movieIds.Contains(ui.MovieId.Value)).ToListAsync();
                if (userInventoryItems.Any())
                {
                    _db.UserInventoryItems.RemoveRange(userInventoryItems);
                    await _db.SaveChangesAsync();
                }

                // 4. Xóa Notifications liên quan đến phim (có Restrict, cần xóa thủ công)
                var notifications = await _db.Notifications.Where(n => n.MovieId.HasValue && movieIds.Contains(n.MovieId.Value)).ToListAsync();
                if (notifications.Any())
                {
                    _db.Notifications.RemoveRange(notifications);
                    await _db.SaveChangesAsync();
                }

                // Xóa tất cả Movies (các bảng có cascade sẽ tự động xóa)
                var allMovies = await _db.Movies.ToListAsync();
                _db.Movies.RemoveRange(allMovies);
                await _db.SaveChangesAsync();

                return Ok(new { 
                    success = true, 
                    message = $"Đã xóa thành công {movieCount} phim và tất cả dữ liệu liên quan.", 
                    deletedCount = movieCount 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi khi xóa phim: {ex.Message}" });
            }
        }
    }
}


