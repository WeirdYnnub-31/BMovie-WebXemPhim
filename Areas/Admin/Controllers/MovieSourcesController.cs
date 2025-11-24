using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Data;
using webxemphim.Models;

namespace webxemphim.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class MovieSourcesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<MovieSourcesController> _logger;
        public MovieSourcesController(ApplicationDbContext db, ILogger<MovieSourcesController> logger) { _db = db; _logger = logger; }

        public async Task<IActionResult> Index(int movieId, string? language = null, string? quality = null, string? sort = null)
        {
            var movie = await _db.Movies.FindAsync(movieId);
            if (movie == null) return NotFound();
            ViewBag.Movie = movie;
            var query = _db.MovieSources.AsNoTracking().Where(s=>s.MovieId==movieId);
            ViewBag.Languages = await query.Select(s=>s.Language).Distinct().OrderBy(s=>s).ToListAsync();
            ViewBag.Qualities = await query.Select(s=>s.Quality).Distinct().OrderByDescending(s=>s).ToListAsync();
            if (!string.IsNullOrWhiteSpace(language)) query = query.Where(s=>s.Language==language);
            if (!string.IsNullOrWhiteSpace(quality)) query = query.Where(s=>s.Quality==quality);
            sort = sort ?? "server";
            ViewBag.FilterLanguage = language; ViewBag.FilterQuality = quality; ViewBag.Sort = sort;
            query = sort switch
            {
                "quality" => query.OrderByDescending(s=>s.Quality),
                "lang" => query.OrderBy(s=>s.Language),
                _ => query.OrderBy(s=>s.ServerName)
            };
            var items = await query.OrderByDescending(s=>s.IsDefault).ToListAsync();
            return View(items);
        }

        public async Task<IActionResult> Create(int movieId)
        {
            var movie = await _db.Movies.FindAsync(movieId);
            if (movie == null) return NotFound();
            ViewBag.Movie = movie;
            return View(new MovieSource{ MovieId = movieId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovieSource model)
        {
            if (!IsValidUrl(model.Url))
            {
                ModelState.AddModelError(nameof(MovieSource.Url), "URL phải là http(s) và đuôi .m3u8, .mpd, hoặc chứa /embed/, iframe, player.vimeo.com, youtube.com/embed");
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Movie = await _db.Movies.FindAsync(model.MovieId);
                return View(model);
            }
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                if (model.IsDefault)
                {
                    var existingDefaults = _db.MovieSources.Where(s=>s.MovieId==model.MovieId && s.IsDefault);
                    foreach (var s in existingDefaults) s.IsDefault = false;
                }
                _db.MovieSources.Add(model);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Failed to create MovieSource for MovieId {MovieId}", model.MovieId);
                ModelState.AddModelError(string.Empty, "Lỗi lưu nguồn.");
                ViewBag.Movie = await _db.Movies.FindAsync(model.MovieId);
                return View(model);
            }
            return RedirectToAction(nameof(Index), new { movieId = model.MovieId });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var src = await _db.MovieSources.FindAsync(id);
            if (src == null) return NotFound();
            ViewBag.Movie = await _db.Movies.FindAsync(src.MovieId);
            return View(src);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MovieSource model)
        {
            if (id != model.Id) return BadRequest();
            if (!IsValidUrl(model.Url))
            {
                ModelState.AddModelError(nameof(MovieSource.Url), "URL phải là http(s) và đuôi .m3u8, .mpd, hoặc chứa /embed/, iframe, player.vimeo.com, youtube.com/embed");
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Movie = await _db.Movies.FindAsync(model.MovieId);
                return View(model);
            }
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                if (model.IsDefault)
                {
                    var existingDefaults = _db.MovieSources.Where(s=>s.MovieId==model.MovieId && s.Id!=model.Id && s.IsDefault);
                    foreach (var s in existingDefaults) s.IsDefault = false;
                }
                _db.Update(model);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Failed to edit MovieSource {Id}", id);
                ModelState.AddModelError(string.Empty, "Lỗi cập nhật nguồn.");
                ViewBag.Movie = await _db.Movies.FindAsync(model.MovieId);
                return View(model);
            }
            return RedirectToAction(nameof(Index), new { movieId = model.MovieId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var src = await _db.MovieSources.FindAsync(id);
            if (src == null) return NotFound();
            var movieId = src.MovieId;
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.MovieSources.Remove(src);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Failed to delete MovieSource {Id}", id);
            }
            return RedirectToAction(nameof(Index), new { movieId });
        }

        private static bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var u)) return false;
            if (u.Scheme != Uri.UriSchemeHttp && u.Scheme != Uri.UriSchemeHttps) return false;
            // Hỗ trợ m3u8, mpd, và các embed links (youtube, vimeo, iframe, etc.)
            var lowerUrl = url.ToLowerInvariant();
            return lowerUrl.EndsWith(".m3u8") || 
                   lowerUrl.EndsWith(".mpd") || 
                   lowerUrl.Contains("/embed/") || 
                   lowerUrl.Contains("iframe") ||
                   lowerUrl.Contains("player.vimeo.com") ||
                   lowerUrl.Contains("youtube.com/embed");
        }
    }
}


