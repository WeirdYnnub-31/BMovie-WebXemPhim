using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Data;
using webxemphim.Models;
using webxemphim.Services;

namespace webxemphim.Controllers
{
    [Authorize]
    public class RatingController : Controller
    {
        private readonly RatingService _ratingService;
        private readonly CoinService _coinService;
        private readonly AchievementService _achievementService;
        private readonly ApplicationDbContext _db;

        public RatingController(
            RatingService ratingService,
            CoinService coinService,
            AchievementService achievementService,
            ApplicationDbContext db)
        {
            _ratingService = ratingService;
            _coinService = coinService;
            _achievementService = achievementService;
            _db = db;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rate(int movieId, int score)
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để đánh giá phim." });
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin người dùng." });
            }

            // Kiểm tra xem đã đánh giá chưa (trước khi rate)
            var existingRating = await _db.Ratings
                .FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == movieId);
            var isFirstRating = existingRating == null;
            
            var success = await _ratingService.RateMovieAsync(userId, movieId, score);
            
            if (success)
            {
                // Thưởng coin và cập nhật achievement (chỉ lần đầu đánh giá)
                if (isFirstRating)
                {
                    var movie = await _db.Movies.FindAsync(movieId);
                    if (movie != null)
                    {
                        // Cập nhật achievement
                        await _achievementService.CheckAndUpdateAchievementsAsync(userId, AchievementType.MoviesRated);
                    }
                }
                
                var averageRating = await _ratingService.GetMovieAverageRatingAsync(movieId);
                var totalRatings = await _ratingService.GetMovieTotalRatingsAsync(movieId);
                
                return Json(new 
                { 
                    success = true, 
                    averageRating = averageRating?.ToString("F1"),
                    totalRatings = totalRatings,
                    message = "Đánh giá thành công!" 
                });
            }

            return Json(new { success = false, message = "Có lỗi xảy ra khi đánh giá phim." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int movieId)
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." });
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin người dùng." });
            }

            var success = await _ratingService.RemoveRatingAsync(userId, movieId);
            
            if (success)
            {
                var averageRating = await _ratingService.GetMovieAverageRatingAsync(movieId);
                var totalRatings = await _ratingService.GetMovieTotalRatingsAsync(movieId);
                
                return Json(new 
                { 
                    success = true, 
                    averageRating = averageRating?.ToString("F1"),
                    totalRatings = totalRatings,
                    message = "Đã xóa đánh giá!" 
                });
            }

            return Json(new { success = false, message = "Có lỗi xảy ra khi xóa đánh giá." });
        }

        [HttpGet]
        public async Task<IActionResult> GetUserRating(int movieId)
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return Json(new { rating = 0 });
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { rating = 0 });
            }

            var rating = await _ratingService.GetUserRatingAsync(userId, movieId);
            return Json(new { rating = rating ?? 0 });
        }
    }
}
