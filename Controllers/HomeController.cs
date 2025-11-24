using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using webxemphim.Models;
using webxemphim.Services;
using webxemphim.Data;
using Microsoft.EntityFrameworkCore;

namespace webxemphim.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly RecommendationService _recommendationService;
        private readonly TMDbService _tmdb;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, RecommendationService recommendationService, ApplicationDbContext db, TMDbService tmdb)
        {
            _logger = logger;
            _recommendationService = recommendationService;
            _db = db;
            _tmdb = tmdb;
        }

        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)] // Cache 5 phút
        public async Task<IActionResult> Index()
        {
            var userId = User.Identity?.IsAuthenticated == true ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;
            
            // Get real data from database
            var featuredMovies = await _db.Movies
                .OrderByDescending(m => m.ViewCount)
                .Take(8)
                .ToListAsync();
            
            var recentMovies = await _db.Movies
                .OrderByDescending(m => m.CreatedAt)
                .Take(12)
                .ToListAsync();
            
            var trendingMovies = await _recommendationService.GetTrendingMoviesAsync(8);
            var newReleases = await _recommendationService.GetNewReleasesAsync(8);
            var recommendations = await _recommendationService.GetRecommendationsAsync(userId ?? "", 8);
            
            // Continue Watching (chỉ cho user đã đăng nhập)
            // Tạm thời comment để tránh lỗi khi chưa chạy migration
            List<webxemphim.Services.ContinueWatchingItem>? continueWatching = null;
            // TODO: Uncomment sau khi chạy migration
            // if (!string.IsNullOrEmpty(userId))
            // {
            //     var watchProgressService = HttpContext.RequestServices.GetRequiredService<WatchProgressService>();
            //     continueWatching = await watchProgressService.GetContinueWatchingAsync(userId, 12);
            // }
            
            ViewBag.Featured = featuredMovies;
            ViewBag.Recent = recentMovies;
            ViewBag.Trending = trendingMovies;
            ViewBag.NewReleases = newReleases;
            ViewBag.Recommendations = recommendations;
            ViewBag.ContinueWatching = continueWatching;

            // Marvel collection from TMDb (not persisted): map to temporary Movie models
            try
            {
                var marvel = await _tmdb.SearchMoviesParsedAsync("Marvel");
                var marvelMovies = marvel.Take(12).Select(m => new Movie
                {
                    Title = m.Title ?? m.Name ?? "Marvel",
                    PosterUrl = _tmdb.GetImageUrl(m.PosterPath ?? "", "w500"),
                    Slug = $"phim-demo-{m.Id}",
                    Imdb = m.VoteAverage,
                    Year = !string.IsNullOrEmpty(m.ReleaseDate) && DateTime.TryParse(m.ReleaseDate, out var d) ? (int?)d.Year : null,
                    Description = m.Overview
                }).ToList();
                ViewBag.Marvel = marvelMovies;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TMDb Marvel fetch failed");
                ViewBag.Marvel = new List<Movie>();
            }
            
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
