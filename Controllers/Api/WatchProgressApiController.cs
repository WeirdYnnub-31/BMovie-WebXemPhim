using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using webxemphim.Services;

namespace webxemphim.Controllers.Api
{
    [ApiController]
    [Route("api/watch-progress")]
    [Authorize]
    public class WatchProgressApiController : ControllerBase
    {
        private readonly WatchProgressService _watchProgressService;

        public WatchProgressApiController(WatchProgressService watchProgressService)
        {
            _watchProgressService = watchProgressService;
        }

        /// <summary>
        /// Lưu vị trí xem hiện tại
        /// </summary>
        [HttpPost("save")]
        public async Task<IActionResult> SaveProgress([FromBody] SaveProgressRequest request)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            await _watchProgressService.SaveProgressAsync(
                userId, 
                request.MovieId, 
                request.CurrentTime, 
                request.Duration, 
                request.EpisodeNumber);

            return Ok(new { success = true, message = "Progress saved" });
        }

        /// <summary>
        /// Lấy vị trí xem cuối cùng
        /// </summary>
        [HttpGet("{movieId}")]
        public async Task<IActionResult> GetProgress(int movieId, [FromQuery] int? episodeNumber = null)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var progress = await _watchProgressService.GetProgressInfoAsync(userId, movieId, episodeNumber);
            
            if (progress == null)
            {
                return Ok(new { hasProgress = false });
            }

            return Ok(new 
            { 
                hasProgress = true,
                currentTime = progress.CurrentTime,
                duration = progress.Duration,
                percentage = progress.Percentage,
                lastWatched = progress.LastWatched
            });
        }

        /// <summary>
        /// Xóa progress (xem lại từ đầu)
        /// </summary>
        [HttpDelete("{movieId}")]
        public async Task<IActionResult> ClearProgress(int movieId, [FromQuery] int? episodeNumber = null)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            await _watchProgressService.ClearProgressAsync(userId, movieId, episodeNumber);
            return Ok(new { success = true, message = "Progress cleared" });
        }

        /// <summary>
        /// Lấy danh sách "Continue Watching"
        /// </summary>
        [HttpGet("continue-watching")]
        public async Task<IActionResult> GetContinueWatching([FromQuery] int limit = 20)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var items = await _watchProgressService.GetContinueWatchingAsync(userId, limit);
            return Ok(items);
        }
    }

    public class SaveProgressRequest
    {
        public int MovieId { get; set; }
        public double CurrentTime { get; set; }
        public double? Duration { get; set; }
        public int? EpisodeNumber { get; set; }
    }
}

