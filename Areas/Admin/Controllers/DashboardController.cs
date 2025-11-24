using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Data;

namespace webxemphim.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        public DashboardController(ApplicationDbContext db) { _db = db; }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var totalMovies = await _db.Movies.CountAsync();
            var totalGenres = await _db.Genres.CountAsync();
            var totalRatings = await _db.Ratings.CountAsync();
            var avgRating = await _db.Ratings.Select(r => (double?)r.Score).AverageAsync() ?? 0d;

            var since = DateTime.UtcNow.Date.AddDays(-13);
            var views = await _db.ViewHits
                .Where(v => v.ViewedAt >= since)
                .GroupBy(v => v.ViewedAt.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .OrderBy(g => g.Day)
                .ToListAsync();

            var topMovies = await _db.Movies
                .OrderByDescending(m => m.ViewCount)
                .Take(10)
                .Select(m => new { m.Title, m.ViewCount })
                .ToListAsync();

            ViewBag.TotalMovies = totalMovies;
            ViewBag.TotalGenres = totalGenres;
            ViewBag.TotalRatings = totalRatings;
            ViewBag.AverageRating = Math.Round(avgRating, 2);
            ViewBag.ViewsSeries = views.Select(v => new object[] { v.Day.ToString("yyyy-MM-dd"), v.Count }).ToList();
            ViewBag.TopMovies = topMovies;
            return View();
        }
    }
}
