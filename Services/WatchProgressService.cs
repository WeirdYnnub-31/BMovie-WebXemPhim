using Microsoft.EntityFrameworkCore;
using webxemphim.Data;
using webxemphim.Models;

namespace webxemphim.Services
{
    /// <summary>
    /// Service để quản lý watch progress và resume watching
    /// </summary>
    public class WatchProgressService
    {
        private readonly ApplicationDbContext _db;

        public WatchProgressService(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Lưu vị trí xem hiện tại
        /// </summary>
        public async Task SaveProgressAsync(string? userId, int movieId, double currentTime, double? duration = null, int? episodeNumber = null)
        {
            if (string.IsNullOrEmpty(userId)) return;

            // Tìm view hit gần nhất của user cho movie này
            var latestView = await _db.ViewHits
                .Where(v => v.UserId == userId && v.MovieId == movieId && 
                           (episodeNumber == null || v.EpisodeNumber == episodeNumber))
                .OrderByDescending(v => v.ViewedAt)
                .FirstOrDefaultAsync();

            if (latestView != null)
            {
                // Cập nhật progress vào view hit gần nhất
                latestView.WatchProgress = currentTime;
                latestView.Duration = duration;
                latestView.EpisodeNumber = episodeNumber;
                latestView.ViewedAt = DateTime.UtcNow;
            }
            else
            {
                // Tạo view hit mới với progress
                var viewHit = new ViewHit
                {
                    MovieId = movieId,
                    UserId = userId,
                    WatchProgress = currentTime,
                    Duration = duration,
                    EpisodeNumber = episodeNumber,
                    ViewedAt = DateTime.UtcNow
                };
                _db.ViewHits.Add(viewHit);
            }

            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Lấy vị trí xem cuối cùng để resume
        /// </summary>
        public async Task<double?> GetLastProgressAsync(string? userId, int movieId, int? episodeNumber = null)
        {
            if (string.IsNullOrEmpty(userId)) return null;

            var viewHit = await _db.ViewHits
                .Where(v => v.UserId == userId && v.MovieId == movieId && 
                           (episodeNumber == null || v.EpisodeNumber == episodeNumber) &&
                           v.WatchProgress.HasValue)
                .OrderByDescending(v => v.ViewedAt)
                .FirstOrDefaultAsync();

            return viewHit?.WatchProgress;
        }

        /// <summary>
        /// Lấy thông tin progress đầy đủ (progress, duration, percentage)
        /// </summary>
        public async Task<WatchProgressInfo?> GetProgressInfoAsync(string? userId, int movieId, int? episodeNumber = null)
        {
            if (string.IsNullOrEmpty(userId)) return null;

            var viewHit = await _db.ViewHits
                .Where(v => v.UserId == userId && v.MovieId == movieId && 
                           (episodeNumber == null || v.EpisodeNumber == episodeNumber) &&
                           v.WatchProgress.HasValue)
                .OrderByDescending(v => v.ViewedAt)
                .FirstOrDefaultAsync();

            if (viewHit == null || !viewHit.WatchProgress.HasValue) return null;

            var progress = viewHit.WatchProgress.Value;
            var duration = viewHit.Duration ?? 0;
            var percentage = duration > 0 ? (progress / duration) * 100 : 0;

            return new WatchProgressInfo
            {
                CurrentTime = progress,
                Duration = duration,
                Percentage = percentage,
                LastWatched = viewHit.ViewedAt
            };
        }

        /// <summary>
        /// Xóa progress (khi user muốn xem lại từ đầu)
        /// </summary>
        public async Task ClearProgressAsync(string? userId, int movieId, int? episodeNumber = null)
        {
            if (string.IsNullOrEmpty(userId)) return;

            var viewHits = await _db.ViewHits
                .Where(v => v.UserId == userId && v.MovieId == movieId && 
                           (episodeNumber == null || v.EpisodeNumber == episodeNumber))
                .ToListAsync();

            foreach (var viewHit in viewHits)
            {
                viewHit.WatchProgress = null;
                viewHit.Duration = null;
            }

            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Lấy danh sách "Continue Watching" - các phim đang xem dở
        /// </summary>
        public async Task<List<ContinueWatchingItem>> GetContinueWatchingAsync(string? userId, int limit = 20)
        {
            if (string.IsNullOrEmpty(userId)) return new List<ContinueWatchingItem>();

            // Lấy danh sách view hits với điều kiện
            var viewHits = await _db.ViewHits
                .Include(v => v.Movie)
                .Where(v => v.UserId == userId && 
                           v.WatchProgress.HasValue && 
                           v.Duration.HasValue &&
                           v.WatchProgress < v.Duration * 0.9) // Chỉ lấy phim chưa xem xong (>90%)
                .ToListAsync();

            // Group và select trong memory để tránh lỗi LINQ translation
            var items = viewHits
                .GroupBy(v => new { v.MovieId, v.EpisodeNumber })
                .Select(g => g.OrderByDescending(v => v.ViewedAt).First())
                .OrderByDescending(v => v.ViewedAt)
                .Take(limit)
                .Select(v => new ContinueWatchingItem
                {
                    MovieId = v.MovieId,
                    MovieTitle = v.Movie?.Title ?? "Unknown",
                    MovieSlug = v.Movie?.Slug,
                    PosterUrl = v.Movie?.PosterUrl,
                    EpisodeNumber = v.EpisodeNumber,
                    CurrentTime = v.WatchProgress ?? 0,
                    Duration = v.Duration ?? 0,
                    Percentage = (v.Duration.HasValue && v.Duration.Value > 0) ? ((v.WatchProgress ?? 0) / v.Duration.Value * 100) : 0,
                    LastWatched = v.ViewedAt
                })
                .ToList();

            return items;
        }
    }

    public class WatchProgressInfo
    {
        public double CurrentTime { get; set; }
        public double Duration { get; set; }
        public double Percentage { get; set; }
        public DateTime LastWatched { get; set; }
    }

    public class ContinueWatchingItem
    {
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string? MovieSlug { get; set; }
        public string? PosterUrl { get; set; }
        public int? EpisodeNumber { get; set; }
        public double CurrentTime { get; set; }
        public double Duration { get; set; }
        public double Percentage { get; set; }
        public DateTime LastWatched { get; set; }
    }
}

