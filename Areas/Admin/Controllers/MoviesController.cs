using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Data;
using webxemphim.Models;
using System.IO;
using webxemphim.Services;
using System.Text.Json;
using System.Linq;

namespace webxemphim.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly TMDbService _tmdb;
        private readonly RophimScraperService _rophim;
        private const long MaxVideoFileSize = 2L * 1024 * 1024 * 1024; // 2GB
        public MoviesController(ApplicationDbContext db, IWebHostEnvironment env, TMDbService tmdb, RophimScraperService rophim) { _db = db; _env = env; _tmdb = tmdb; _rophim = rophim; }

        private string? NormalizeTrailerUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            url = url.Trim();
            try
            {
                // YouTube watch -> embed
                if (url.Contains("youtube.com/watch?v="))
                {
                    var uri = new Uri(url);
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    var v = query.Get("v");
                    if (!string.IsNullOrWhiteSpace(v))
                        return $"https://www.youtube.com/embed/{v}";
                }
                // youtu.be short -> embed
                if (url.Contains("youtu.be/"))
                {
                    var vid = url.Split('/').LastOrDefault();
                    if (!string.IsNullOrWhiteSpace(vid))
                        return $"https://www.youtube.com/embed/{vid}";
                }
                // Vimeo normal -> player
                if (url.Contains("vimeo.com/") && !url.Contains("player.vimeo.com"))
                {
                    var vid = url.Split('/').LastOrDefault();
                    if (!string.IsNullOrWhiteSpace(vid))
                        return $"https://player.vimeo.com/video/{vid}";
                }
            }
            catch { }
            return url;
        }

        private static bool IsValidSourceUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (url.StartsWith("/uploads/videos/", StringComparison.OrdinalIgnoreCase)) return true;
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

        public async Task<IActionResult> Index(string? q, int? genreId, int? year, double? imdbMin, string? sort, int page = 1, int pageSize = 12)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 12;
            var queryable = _db.Movies.AsNoTracking().Include(m=>m.MovieGenres).ThenInclude(mg=>mg.Genre).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q)) queryable = queryable.Where(m=>m.Title.Contains(q));
            if (genreId.HasValue) queryable = queryable.Where(m=>m.MovieGenres.Any(g=>g.GenreId==genreId));
            if (year.HasValue) queryable = queryable.Where(m=>m.Year==year);
            if (imdbMin.HasValue) queryable = queryable.Where(m=>(m.Imdb ?? 0) >= imdbMin);
            sort = sort ?? "id_desc";
            queryable = sort switch
            {
                "title_asc" => queryable.OrderBy(m=>m.Title),
                "title_desc" => queryable.OrderByDescending(m=>m.Title),
                "year_asc" => queryable.OrderBy(m=>m.Year),
                "year_desc" => queryable.OrderByDescending(m=>m.Year),
                "imdb_asc" => queryable.OrderBy(m=>m.Imdb),
                "imdb_desc" => queryable.OrderByDescending(m=>m.Imdb),
                _ => queryable.OrderByDescending(m=>m.Id)
            };
            ViewBag.GenreList = await _db.Genres.AsNoTracking().OrderBy(g=>g.Name).ToListAsync();
            ViewBag.Q = q; ViewBag.FilterGenreId = genreId; ViewBag.FilterYear = year; ViewBag.FilterImdb = imdbMin; ViewBag.Sort = sort;
            var total = await queryable.CountAsync();
            var items = await queryable.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportFromTmdb(int tmdbId)
        {
            try
            {
                if(tmdbId <= 0)
                {
                    TempData["Message"] = "Bạn chưa nhập TMDb ID hoặc ID không hợp lệ.";
                    return RedirectToAction(nameof(Index));
                }

                var dto = await _tmdb.GetMovieDetailsByIdAsync(tmdbId);
                if (dto == null)
                {
                    TempData["Message"] = "Không tìm thấy phim theo TMDb ID hoặc API trả về rỗng. Vui lòng kiểm tra lại ID hoặc Access Token.";
                    return RedirectToAction(nameof(Index));
                }

                // Kiểm tra trùng theo TMDbId trước (ưu tiên), nếu không có TMDbId thì mới kiểm tra title
                var existingMovie = await _db.Movies.FirstOrDefaultAsync(m => m.TMDbId == dto.Id);
                if (existingMovie == null && !string.IsNullOrWhiteSpace(dto.Title))
                {
                    existingMovie = await _db.Movies.FirstOrDefaultAsync(m => m.Title == dto.Title && m.TMDbId == null);
                }
                if (existingMovie != null)
                {
                    TempData["Message"] = $"Phim '{dto.Title}' đã tồn tại trong hệ thống.";
                    return RedirectToAction(nameof(Index));
                }

                // Lấy trailer từ TMDb
                var trailerUrl = await _tmdb.GetMovieYoutubeTrailerEmbedAsync(dto.Id);

                // Cập nhật runtime và country từ dto
                var duration = dto.Runtime.HasValue && dto.Runtime.Value > 0 ? dto.Runtime.Value : 120;
                var country = "Unknown";
                if (dto.ProductionCountries != null && dto.ProductionCountries.Any())
                {
                    var countries = dto.ProductionCountries.Select(c => c.Name ?? c.IsoCode).Where(c => !string.IsNullOrWhiteSpace(c));
                    country = string.Join(", ", countries);
                }

                // Thêm cast và director từ dto (đã được lấy từ credits API)
                var castString = dto.Cast != null && dto.Cast.Any() 
                    ? string.Join(", ", dto.Cast.Take(10)) 
                    : null;
                var director = dto.Director;

                // Tạo phim mới
                var movie = new Movie
                {
                    TMDbId = dto.Id,
                    Title = dto.Title ?? dto.Name ?? string.Empty,
                    Slug = GenerateSlug(dto.Title ?? dto.Name ?? string.Empty),
                    Year = DateTime.TryParse(dto.ReleaseDate, out var d) ? d.Year : DateTime.Now.Year,
                    ReleaseDate = DateTime.TryParse(dto.ReleaseDate, out var rd) ? rd : null,
                    Imdb = dto.VoteAverage,
                    PosterUrl = _tmdb.GetImageUrl(dto.PosterPath ?? string.Empty, "w500"),
                    Description = dto.Overview ?? "Không có mô tả",
                    TrailerUrl = trailerUrl, // Thêm trailer URL
                    Country = country,
                    DurationMinutes = duration,
                    Cast = castString,
                    Director = director,
                    AgeRating = "PG-13",
                    IsSeries = false,
                    ViewCount = 0,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Movies.Add(movie);
                try {
                    await _db.SaveChangesAsync();
                } catch(Exception ex) {
                    TempData["Message"] = $"Lỗi lưu phim vào database: {ex.Message}";
                    return RedirectToAction(nameof(Index));
                }

                // Thêm genres nếu có (tự động tạo genre nếu chưa có)
                if (dto.Genres?.Any() == true)
                {
                    foreach (var genreDto in dto.Genres)
                    {
                        if (string.IsNullOrWhiteSpace(genreDto.Name)) continue;

                        var genre = await _db.Genres.FirstOrDefaultAsync(g => g.Name == genreDto.Name);
                        if (genre == null)
                        {
                            // Tự động tạo genre mới nếu chưa có
                            genre = new Genre
                            {
                                Name = genreDto.Name,
                                Slug = genreDto.Name.ToLower()
                                    .Replace(" ", "-")
                                    .Replace("'", "")
                                    .Replace(",", "")
                            };
                            _db.Genres.Add(genre);
                            await _db.SaveChangesAsync(); // Save to get ID
                        }
                        movie.MovieGenres.Add(new MovieGenre { MovieId = movie.Id, GenreId = genre.Id });
                    }
                    await _db.SaveChangesAsync();
                }

                await _db.SaveChangesAsync();

                TempData["Message"] = $"Đã import thành công phim '{movie.Title}' từ TMDb!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Lỗi khi import phim: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private string GenerateSlug(string title)
        {
            if (string.IsNullOrEmpty(title)) return "movie";
            return title.ToLower()
                .Replace(" ", "-")
                .Replace(":", "")
                .Replace("'", "")
                .Replace("\"", "")
                .Replace("?", "")
                .Replace("!", "")
                .Replace(",", "")
                .Replace(".", "");
        }

        public IActionResult Create()
        {
            ViewBag.Genres = _db.Genres.AsNoTracking().OrderBy(g=>g.Name).ToList();
            return View(new Movie());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Movie model, IFormFile? posterFile, IFormFile? newSourceFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Genres = _db.Genres.AsNoTracking().OrderBy(g=>g.Name).ToList();
                return View(model);
            }
            model.TrailerUrl = NormalizeTrailerUrl(model.TrailerUrl);

            string? uploadedSourceUrl = null;
            if (newSourceFile != null && newSourceFile.Length > 0)
            {
                if (newSourceFile.Length > MaxVideoFileSize)
                {
                    ModelState.AddModelError("NewSourceFile", "File video quá lớn (tối đa 2GB).");
                }
                else
                {
                    try
                    {
                        uploadedSourceUrl = await SaveVideoFileAsync(newSourceFile);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("NewSourceFile", $"Không thể lưu file video: {ex.Message}");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Genres = _db.Genres.AsNoTracking().OrderBy(g=>g.Name).ToList();
                return View(model);
            }

            if (posterFile != null && posterFile.Length > 0)
            {
                var uploads = Path.Combine(_env.WebRootPath, "uploads", "posters");
                Directory.CreateDirectory(uploads);
                var fileName = $"poster_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Path.GetFileName(posterFile.FileName)}";
                var path = Path.Combine(uploads, fileName);
                using (var stream = System.IO.File.Create(path))
                {
                    await posterFile.CopyToAsync(stream);
                }
                model.PosterUrl = $"/uploads/posters/{fileName}";
            }
            _db.Movies.Add(model);
            await _db.SaveChangesAsync();
            var selected = Request.Form["GenreIds"].Select(int.Parse).ToList();
            if (selected.Any())
            {
                _db.MovieGenres.AddRange(selected.Select(gid => new MovieGenre { MovieId = model.Id, GenreId = gid }));
                await _db.SaveChangesAsync();
            }

            var sourcesToAdd = new List<MovieSource>();
            var server = Request.Form["NewSourceServer"].FirstOrDefault() ?? "Server 1";
            var quality = Request.Form["NewSourceQuality"].FirstOrDefault() ?? "1080p";
            var lang = Request.Form["NewSourceLanguage"].FirstOrDefault() ?? "Vietsub";
            int? episode = null; if (int.TryParse(Request.Form["NewSourceEpisode"].FirstOrDefault(), out var epVal)) episode = Math.Max(1, epVal);

            var externalUrl = Request.Form["NewSourceUrl"].FirstOrDefault();
            var externalDefault = string.Equals(Request.Form["NewSourceDefault"].FirstOrDefault(), "on", StringComparison.OrdinalIgnoreCase);
            if (IsValidSourceUrl(externalUrl))
            {
                sourcesToAdd.Add(new MovieSource
                {
                    MovieId = model.Id,
                    ServerName = server,
                    Quality = quality,
                    Language = lang,
                    Url = externalUrl!,
                    EpisodeNumber = episode,
                    IsDefault = externalDefault
                });
            }

            if (!string.IsNullOrWhiteSpace(uploadedSourceUrl))
            {
                var localDefault = string.Equals(Request.Form["NewSourceLocalDefault"].FirstOrDefault(), "on", StringComparison.OrdinalIgnoreCase);
                sourcesToAdd.Add(new MovieSource
                {
                    MovieId = model.Id,
                    ServerName = string.IsNullOrWhiteSpace(server) ? "Local Upload" : server,
                    Quality = quality,
                    Language = lang,
                    Url = uploadedSourceUrl,
                    EpisodeNumber = episode,
                    IsDefault = localDefault || !sourcesToAdd.Any(s => s.IsDefault)
                });
            }

            if (sourcesToAdd.Any())
            {
                if (sourcesToAdd.Count(s => s.IsDefault) > 1)
                {
                    var defaultSet = false;
                    foreach (var src in sourcesToAdd)
                    {
                        if (src.IsDefault)
                        {
                            if (!defaultSet) defaultSet = true;
                            else src.IsDefault = false;
                        }
                    }
                }
                if (sourcesToAdd.Any(s => s.IsDefault))
                {
                    var defaults = _db.MovieSources.Where(s => s.MovieId == model.Id && s.IsDefault);
                    foreach (var s in defaults) s.IsDefault = false;
                }
                _db.MovieSources.AddRange(sourcesToAdd);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var movie = await _db.Movies.Include(m=>m.MovieGenres).FirstOrDefaultAsync(m=>m.Id==id);
            if (movie == null) return NotFound();
            ViewBag.Genres = await _db.Genres.AsNoTracking().OrderBy(g=>g.Name).ToListAsync();
            ViewBag.SelectedGenreIds = movie.MovieGenres.Select(x=>x.GenreId).ToList();
            return View(movie);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Movie model, IFormFile? posterFile, IFormFile? newSourceFile)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid)
            {
                ViewBag.Genres = await _db.Genres.AsNoTracking().OrderBy(g=>g.Name).ToListAsync();
                return View(model);
            }
            var existing = await _db.Movies.Include(m=>m.MovieGenres).FirstOrDefaultAsync(m=>m.Id==id);
            if (existing == null) return NotFound();
            model.TrailerUrl = NormalizeTrailerUrl(model.TrailerUrl);

            string? uploadedSourceUrl = null;
            if (newSourceFile != null && newSourceFile.Length > 0)
            {
                if (newSourceFile.Length > MaxVideoFileSize)
                {
                    ModelState.AddModelError("NewSourceFile", "File video quá lớn (tối đa 2GB).");
                }
                else
                {
                    try
                    {
                        uploadedSourceUrl = await SaveVideoFileAsync(newSourceFile);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("NewSourceFile", $"Không thể lưu file video: {ex.Message}");
                    }
                }
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Genres = await _db.Genres.AsNoTracking().OrderBy(g=>g.Name).ToListAsync();
                ViewBag.SelectedGenreIds = existing.MovieGenres.Select(x=>x.GenreId).ToList();
                return View(model);
            }

            _db.Entry(existing).CurrentValues.SetValues(model);
            if (posterFile != null && posterFile.Length > 0)
            {
                var uploads = Path.Combine(_env.WebRootPath, "uploads", "posters");
                Directory.CreateDirectory(uploads);
                var fileName = $"poster_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Path.GetFileName(posterFile.FileName)}";
                var path = Path.Combine(uploads, fileName);
                using (var stream = System.IO.File.Create(path))
                {
                    await posterFile.CopyToAsync(stream);
                }
                if (!string.IsNullOrWhiteSpace(existing.PosterUrl) && existing.PosterUrl.StartsWith("/uploads/posters/"))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, existing.PosterUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(oldPath))
                    {
                        try { System.IO.File.Delete(oldPath); } catch { /* ignore */ }
                    }
                }
                existing.PosterUrl = $"/uploads/posters/{fileName}";
            }
            existing.MovieGenres.Clear();
            var selected = Request.Form["GenreIds"].Select(int.Parse).ToList();
            existing.MovieGenres = selected.Select(gid => new MovieGenre { MovieId = existing.Id, GenreId = gid }).ToList();
            var newSources = new List<MovieSource>();
            var server = Request.Form["NewSourceServer"].FirstOrDefault() ?? "Server 1";
            var quality = Request.Form["NewSourceQuality"].FirstOrDefault() ?? "1080p";
            var lang = Request.Form["NewSourceLanguage"].FirstOrDefault() ?? "Vietsub";
            int? episode = null; if (int.TryParse(Request.Form["NewSourceEpisode"].FirstOrDefault(), out var epVal)) episode = Math.Max(1, epVal);

            var externalUrl = Request.Form["NewSourceUrl"].FirstOrDefault();
            var externalDefault = string.Equals(Request.Form["NewSourceDefault"].FirstOrDefault(), "on", StringComparison.OrdinalIgnoreCase);
            if (IsValidSourceUrl(externalUrl))
            {
                newSources.Add(new MovieSource
                {
                    MovieId = existing.Id,
                    ServerName = server,
                    Quality = quality,
                    Language = lang,
                    Url = externalUrl!,
                    EpisodeNumber = episode,
                    IsDefault = externalDefault
                });
            }
            if (!string.IsNullOrWhiteSpace(uploadedSourceUrl))
            {
                var localDefault = string.Equals(Request.Form["NewSourceLocalDefault"].FirstOrDefault(), "on", StringComparison.OrdinalIgnoreCase);
                newSources.Add(new MovieSource
                {
                    MovieId = existing.Id,
                    ServerName = string.IsNullOrWhiteSpace(server) ? "Local Upload" : server,
                    Quality = quality,
                    Language = lang,
                    Url = uploadedSourceUrl,
                    EpisodeNumber = episode,
                    IsDefault = localDefault || !newSources.Any(s => s.IsDefault)
                });
            }
            if (newSources.Any())
            {
                if (newSources.Count(s => s.IsDefault) > 1)
                {
                    var defaultSet = false;
                    foreach (var src in newSources)
                    {
                        if (src.IsDefault)
                        {
                            if (!defaultSet) defaultSet = true;
                            else src.IsDefault = false;
                        }
                    }
                }
                if (newSources.Any(s => s.IsDefault))
                {
                    var defaults = _db.MovieSources.Where(s => s.MovieId == existing.Id && s.IsDefault);
                    foreach (var s in defaults) s.IsDefault = false;
                }
                _db.MovieSources.AddRange(newSources);
            }
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var movie = await _db.Movies.FindAsync(id);
                if (movie == null)
                {
                    TempData["Message"] = "Không tìm thấy phim để xóa.";
                    return RedirectToAction(nameof(Index));
                }

                // Xóa dữ liệu liên quan trước (các bảng không có cascade delete)
                
                // 1. Xóa UserInventoryItems liên quan đến phim
                var userInventoryItems = await _db.UserInventoryItems
                    .Where(ui => ui.MovieId.HasValue && ui.MovieId.Value == id)
                    .ToListAsync();
                if (userInventoryItems.Any())
                {
                    _db.UserInventoryItems.RemoveRange(userInventoryItems);
                    await _db.SaveChangesAsync();
                }

                // 2. Xóa Notifications liên quan đến phim (có Restrict, cần xóa thủ công)
                var notifications = await _db.Notifications
                    .Where(n => n.MovieId.HasValue && n.MovieId.Value == id)
                    .ToListAsync();
                if (notifications.Any())
                {
                    _db.Notifications.RemoveRange(notifications);
                    await _db.SaveChangesAsync();
                }

                // 3. Xóa MovieGenres (bảng trung gian - có cascade nhưng xóa thủ công để chắc chắn)
                var movieGenres = await _db.MovieGenres
                    .Where(mg => mg.MovieId == id)
                    .ToListAsync();
                if (movieGenres.Any())
                {
                    _db.MovieGenres.RemoveRange(movieGenres);
                    await _db.SaveChangesAsync();
                }

                // Các bảng có cascade delete sẽ tự động xóa khi xóa Movie:
                // - Comments (Cascade)
                // - MovieSources (Cascade)
                // - ViewHits (Cascade)
                // - Ratings (Cascade)
                // - WatchParties (Cascade) - sẽ tự động xóa WatchPartyParticipants và WatchPartyMessages
                // - Subtitles (Cascade)
                // - UserShares (Cascade)
                // - Feedbacks (SetNull - sẽ set MovieId = null)

                // Xóa phim
                _db.Movies.Remove(movie);
                await _db.SaveChangesAsync();

                TempData["Message"] = $"Đã xóa thành công phim '{movie.Title}' và tất cả dữ liệu liên quan.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Lỗi khi xóa phim: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public IActionResult ImportOphim()
        {
            ViewBag.ImportResult = TempData["ImportResult"];
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ImportOphim(string ophimSlug)
        {
            if (string.IsNullOrWhiteSpace(ophimSlug)) {
                TempData["ImportResult"] = "Bạn phải nhập slug phim (mã đường dẫn trên ophim1.com)!";
                return RedirectToAction("ImportOphim");
            }
            using var http = new HttpClient();
            var api = $"https://ophim1.com/phim/{ophimSlug.Trim()}";
            try {
                var response = await http.GetAsync(api);
                if (!response.IsSuccessStatusCode) {
                    TempData["ImportResult"] = $"Không tìm thấy hoặc lỗi API Ophim. Status {response.StatusCode}";
                    return RedirectToAction("ImportOphim");
                }
                var json = await response.Content.ReadAsStringAsync();
                dynamic data = System.Text.Json.JsonDocument.Parse(json).RootElement;
                var movieData = data.GetProperty("movie");
                JsonElement epArr;
                var episodes = data.TryGetProperty("episodes", out epArr) ? epArr : default;
                // Kiểm tra tồn tại
                var exist = await _db.Movies.FirstOrDefaultAsync(m=>m.Slug==ophimSlug);
                if (exist!=null) {
                    TempData["ImportResult"] = $"Phim '{exist.Title}' (slug: {ophimSlug}) đã tồn tại trong database!";
                    return RedirectToAction("ImportOphim");
                }
                // Thêm movie mới
                JsonElement p; JsonElement yr; JsonElement c;
                var movie = new Movie{
                    Title = movieData.GetProperty("name").GetString(),
                    Slug = ophimSlug,
                    PosterUrl = movieData.TryGetProperty("poster_url", out p) ? p.GetString():null,
                    Year = movieData.TryGetProperty("year", out yr)&&yr.ValueKind==System.Text.Json.JsonValueKind.Number?yr.GetInt32():null,
                    Description = movieData.TryGetProperty("content", out c) ? c.GetString(): null,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Movies.Add(movie);
                await _db.SaveChangesAsync();
                // Thêm các server nguồn nếu có
                if (episodes.ValueKind == System.Text.Json.JsonValueKind.Array){
                    foreach(var server in episodes.EnumerateArray()){
                        var serverName = server.TryGetProperty("server_name", out var sn)?sn.GetString():"";
                        var serverData = server.TryGetProperty("server_data", out var sdata)?sdata:default;
                        if (serverData.ValueKind == System.Text.Json.JsonValueKind.Array){
                            foreach(var ep in serverData.EnumerateArray()){
                                var epLink = ep.TryGetProperty("link_embed",out var l)?l.GetString():null;
                                if (!string.IsNullOrEmpty(epLink)){
                                    _db.MovieSources.Add(new MovieSource{
                                        MovieId = movie.Id,
                                        ServerName = serverName,
                                        Url = epLink,
                                        IsDefault = true
                                    });
                                    break; // mỗi server lấy 1 tập đầu làm demo nhanh
                                }
                            }
                        }
                    }
                    await _db.SaveChangesAsync();
                }
                TempData["ImportResult"] = $"Đã import phim '{movie.Title}' thành công, bạn kiểm tra 'Xem phim' để test source!";
                return RedirectToAction("ImportOphim");
            }
            catch(Exception ex){
                TempData["ImportResult"] = "Lỗi khi import/parse phim từ Ophim: " + ex.Message;
                return RedirectToAction("ImportOphim");
            }
        }

        [HttpGet]
        public IActionResult ImportRophim()
        {
            ViewBag.ImportResult = TempData["ImportResult"];
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ImportRophim(string rophimSlug)
        {
            if (string.IsNullOrWhiteSpace(rophimSlug))
            {
                TempData["ImportResult"] = "Bạn phải nhập slug phim (mã đường dẫn trên rophim.li)!";
                return RedirectToAction("ImportRophim");
            }

            try
            {
                var rophimMovie = await _rophim.ScrapeMovie(rophimSlug.Trim());
                
                if (rophimMovie == null || string.IsNullOrWhiteSpace(rophimMovie.Title))
                {
                    TempData["ImportResult"] = "Không tìm thấy phim hoặc lỗi khi scrape từ Rophim!";
                    return RedirectToAction("ImportRophim");
                }

                // Check if exists
                var exist = await _db.Movies.FirstOrDefaultAsync(m => m.Slug == rophimSlug);
                if (exist != null)
                {
                    TempData["ImportResult"] = $"Phim '{exist.Title}' (slug: {rophimSlug}) đã tồn tại trong database!";
                    return RedirectToAction("ImportRophim");
                }

                // Create movie
                var movie = new Movie
                {
                    Title = rophimMovie.Title,
                    Slug = rophimSlug,
                    PosterUrl = rophimMovie.PosterUrl,
                    Year = rophimMovie.Year,
                    Description = rophimMovie.Description,
                    IsSeries = true, // rophim.li is for TV series
                    CreatedAt = DateTime.UtcNow
                };

                _db.Movies.Add(movie);
                await _db.SaveChangesAsync();

                // Add sources
                foreach (var source in rophimMovie.Sources)
                {
                    _db.MovieSources.Add(new MovieSource
                    {
                        MovieId = movie.Id,
                        ServerName = source.ServerName,
                        Url = source.Url,
                        IsDefault = source.IsDefault,
                        Quality = "1080p",
                        Language = "Vietsub"
                    });
                }

                await _db.SaveChangesAsync();
                TempData["ImportResult"] = $"Đã import phim '{movie.Title}' thành công với {rophimMovie.Sources.Count} sources!";
                return RedirectToAction("ImportRophim");
            }
            catch (Exception ex)
            {
                TempData["ImportResult"] = "Lỗi khi import phim từ Rophim: " + ex.Message;
                return RedirectToAction("ImportRophim");
            }
        }

        private async Task<string> SaveVideoFileAsync(IFormFile file)
        {
            var videos = Path.Combine(_env.WebRootPath, "uploads", "videos");
            Directory.CreateDirectory(videos);
            var sanitizedName = SanitizeFileName(Path.GetFileNameWithoutExtension(file.FileName));
            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension)) extension = ".mp4";
            var fileName = $"movie_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{sanitizedName}{extension}";
            var path = Path.Combine(videos, fileName);
            using (var stream = System.IO.File.Create(path))
            {
                await file.CopyToAsync(stream);
            }
            return $"/uploads/videos/{fileName}";
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var cleaned = new string(fileName.Where(ch => !invalidChars.Contains(ch)).ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? "video" : cleaned;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllMovies()
        {
            try
            {
                // Lấy danh sách tất cả Movie IDs trước
                var movieIds = await _db.Movies.Select(m => m.Id).ToListAsync();
                
                if (!movieIds.Any())
                {
                    TempData["Message"] = "Không có phim nào trong database để xóa.";
                    return RedirectToAction(nameof(Index));
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

                // Các bảng có cascade delete sẽ tự động xóa khi xóa Movies:
                // - Comments (Cascade)
                // - MovieSources (Cascade)
                // - Ratings (Cascade)
                // - WatchParties (Cascade) - sẽ tự động xóa WatchPartyParticipants và WatchPartyMessages
                // - Subtitles (Cascade)
                // - UserShares (Cascade)

                // Xóa tất cả Movies (các bảng có cascade sẽ tự động xóa)
                var allMovies = await _db.Movies.ToListAsync();
                _db.Movies.RemoveRange(allMovies);
                await _db.SaveChangesAsync();

                TempData["Message"] = $"Đã xóa thành công {movieCount} phim và tất cả dữ liệu liên quan khỏi database.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Lỗi khi xóa phim: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}


