using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Models;
using webxemphim.Services;
using webxemphim.Data;

namespace webxemphim.Controllers
{
    public class MoviesController : Controller
    {
        private readonly TMDbService _tmdb;
        private readonly ApplicationDbContext _db;
        private readonly RecommendationService _recommendationService;
        private readonly RatingService _ratingService;
        private readonly CoinService _coinService;
        private readonly AchievementService _achievementService;
        private readonly MovieStateService _movieStateService;
        private readonly SubtitleService _subtitleService;
        private readonly SocialService _socialService;
        
        public MoviesController(
            TMDbService tmdb, 
            ApplicationDbContext db, 
            RecommendationService recommendationService, 
            RatingService ratingService,
            CoinService coinService,
            AchievementService achievementService,
            MovieStateService movieStateService,
            SubtitleService subtitleService,
            SocialService socialService) 
        { 
            _tmdb = tmdb; 
            _db = db; 
            _recommendationService = recommendationService;
            _ratingService = ratingService;
            _coinService = coinService;
            _achievementService = achievementService;
            _movieStateService = movieStateService;
            _subtitleService = subtitleService;
            _socialService = socialService;
        }
        private List<Movie> GetMock()
        {
            return Enumerable.Range(1, 20).Select(i => new Movie
            {
                Id = i,
                Title = $"Phim demo {i}",
                Slug = $"phim-demo-{i}",
                PosterUrl = "/favicon.ico",
                Imdb = 7.0 + (i % 4),
                AgeRating = (i % 3 == 0) ? "P" : (i % 2 == 0 ? "16+" : "18+"),
                DurationMinutes = 90 + i,
                Year = 2023 + (i % 3),
                Country = (i % 2 == 0) ? "US-UK" : "Korea",
                Description = "Mô tả ngắn cho phim demo.",
                IsSeries = i % 2 == 0,
                MovieGenres = new List<MovieGenre>{ new MovieGenre{ Genre = new Genre{ Id=1, Name = (i%2==0?"Hành động":"Tâm lý") } } }
            }).ToList();
        }

        [HttpGet("movies")]
        public async Task<IActionResult> Index(bool? isSeries = null, string? genre = null, string? country = null)
        {
            var viewModels = new List<MovieViewModel>();
            
            // If any filter is applied, use database
            bool hasFilter = isSeries.HasValue || !string.IsNullOrWhiteSpace(genre) || !string.IsNullOrWhiteSpace(country);
            
            try
            {
                // Try to get from TMDb first (only if no filter)
                if (!hasFilter)
                {
                    var list = await _tmdb.GetPopularMoviesParsedAsync();
                    ViewBag.Image = (Func<string, string, string>)((path, size) => _tmdb.GetImageUrl(path, size));
                    
                    viewModels = list.Select(m => new MovieViewModel
                    {
                        Id = m.Id,
                        Title = m.Title ?? m.Name ?? "Unknown",
                        Name = m.Name,
                        PosterPath = m.PosterPath,
                        VoteAverage = m.VoteAverage,
                        Overview = m.Overview,
                        ReleaseDate = m.ReleaseDate,
                        PosterUrl = _tmdb.GetImageUrl(m.PosterPath ?? "", "w500"),
                        Year = DateTime.TryParse(m.ReleaseDate, out var d) ? d.Year : DateTime.Now.Year,
                        Description = m.Overview
                    }).ToList();
                }
                else
                {
                    throw new HttpRequestException("Filter applied, use database");
                }
            }
            catch (HttpRequestException)
            {
                // Fallback to database movies if TMDb fails or filter applied
                var query = _db.Movies
                    .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                    .AsQueryable();
                
                // Filter by IsSeries if specified
                if (isSeries.HasValue)
                {
                    query = query.Where(m => m.IsSeries == isSeries.Value);
                }
                
                // Filter by genre if specified
                if (!string.IsNullOrWhiteSpace(genre))
                {
                    query = query.Where(m => m.MovieGenres.Any(mg => mg.Genre.Slug == genre || mg.Genre.Name == genre));
                }
                
                // Filter by country if specified
                if (!string.IsNullOrWhiteSpace(country))
                {
                    query = query.Where(m => m.Country != null && m.Country.Contains(country));
                }
                
                var movies = await query
                    .OrderByDescending(m => m.ViewCount)
                    .Take(50)
                    .ToListAsync();
                
                ViewBag.Image = (Func<string, string, string>)((path, size) => path ?? "/favicon.ico");
                
                viewModels = movies.Select(m => new MovieViewModel
                {
                    Id = m.Id,
                    Title = m.Title,
                    PosterUrl = m.PosterUrl,
                    Slug = m.Slug,
                    VoteAverage = m.Imdb,
                    Overview = m.Description,
                    Year = m.Year ?? DateTime.Now.Year,
                    Description = m.Description,
                    Genres = m.MovieGenres?.Select(mg => mg.Genre?.Name ?? "").Where(g => !string.IsNullOrEmpty(g)).ToList() ?? new List<string>()
                }).ToList();
            }
            
            // Genres for quick filters on /movies
            ViewBag.Genres = await _db.Genres.AsNoTracking().OrderBy(g=>g.Name).ToListAsync();
            return View(viewModels);
        }

        [HttpGet("phim-le")]
        public async Task<IActionResult> SingleMovies()
        {
            var viewModels = new List<MovieViewModel>();
            
            // Get single movies (ContentType = Movie or IsSeries = false) from database
            var query = _db.Movies
                .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .Where(m => m.ContentType == ContentType.Movie || (m.ContentType == ContentType.Movie && m.IsSeries == false))
                .AsQueryable();
            
            var movies = await query
                .OrderByDescending(m => m.ViewCount)
                .Take(50)
                .ToListAsync();
            
            ViewBag.Image = (Func<string, string, string>)((path, size) => path ?? "/favicon.ico");
            
            viewModels = movies.Select(m => new MovieViewModel
            {
                Id = m.Id,
                Title = m.Title,
                PosterUrl = m.PosterUrl,
                Slug = m.Slug,
                VoteAverage = m.Imdb,
                Overview = m.Description,
                Year = m.Year ?? DateTime.Now.Year,
                Description = m.Description,
                Genres = m.MovieGenres?.Select(mg => mg.Genre?.Name ?? "").Where(g => !string.IsNullOrEmpty(g)).ToList() ?? new List<string>()
            }).ToList();
            
            ViewBag.Genres = await _db.Genres.AsNoTracking().OrderBy(g=>g.Name).ToListAsync();
            return View("Index", viewModels); // Explicitly use Index view
        }

        [HttpGet("phim-bo")]
        public async Task<IActionResult> SeriesMovies()
        {
            var viewModels = new List<MovieViewModel>();
            
            // Get series movies (ContentType = TVShow or IsSeries = true) from database
            var query = _db.Movies
                .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .Where(m => m.ContentType == ContentType.TVShow || (m.IsSeries == true && m.ContentType == ContentType.Movie))
                .AsQueryable();
            
            var movies = await query
                .OrderByDescending(m => m.ViewCount)
                .Take(50)
                .ToListAsync();
            
            ViewBag.Image = (Func<string, string, string>)((path, size) => path ?? "/favicon.ico");
            
            viewModels = movies.Select(m => new MovieViewModel
            {
                Id = m.Id,
                Title = m.Title,
                PosterUrl = m.PosterUrl,
                Slug = m.Slug,
                VoteAverage = m.Imdb,
                Overview = m.Description,
                Year = m.Year ?? DateTime.Now.Year,
                Description = m.Description,
                Genres = m.MovieGenres?.Select(mg => mg.Genre?.Name ?? "").Where(g => !string.IsNullOrEmpty(g)).ToList() ?? new List<string>()
            }).ToList();
            
            ViewBag.Genres = await _db.Genres.AsNoTracking().OrderBy(g=>g.Name).ToListAsync();
            return View("Index", viewModels); // Explicitly use Index view
        }

        [HttpGet("movie/{slug}")]
        public async Task<IActionResult> Detail(string slug)
        {
            var dbMovie = await _db.Movies
                .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Slug == slug);

            if (dbMovie == null)
            {
                // Smart fallback: if slug is a demo-like slug, redirect to a real movie
                if (!string.IsNullOrWhiteSpace(slug) && slug.StartsWith("phim-demo", StringComparison.OrdinalIgnoreCase))
                {
                    var fallback = await _db.Movies.AsNoTracking().OrderByDescending(m => m.ViewCount).FirstOrDefaultAsync();
                    if (fallback != null && !string.IsNullOrWhiteSpace(fallback.Slug))
                        return RedirectToAction("Detail", new { slug = fallback.Slug });
                }
                return NotFound();
            }

            // Get similar movies using recommendation service
            var similarMovies = await _recommendationService.GetSimilarMoviesAsync(dbMovie.Id, 8);
            ViewBag.Related = similarMovies;

            // Lấy trailer từ TMDb nếu chưa có (load lại với tracking để update)
            if (string.IsNullOrWhiteSpace(dbMovie.TrailerUrl) && dbMovie.TMDbId.HasValue)
            {
                try
                {
                    var movieToUpdate = await _db.Movies.FindAsync(dbMovie.Id);
                    if (movieToUpdate != null)
                    {
                        var trailerUrl = await _tmdb.GetMovieYoutubeTrailerEmbedAsync(dbMovie.TMDbId.Value);
                        if (!string.IsNullOrWhiteSpace(trailerUrl))
                        {
                            movieToUpdate.TrailerUrl = trailerUrl;
                            await _db.SaveChangesAsync();
                            dbMovie.TrailerUrl = trailerUrl; // Update view model
                        }
                    }
                }
                catch
                {
                    // Ignore errors when fetching trailer
                }
            }

            // Top by views for sidebar
            ViewBag.TopByViews = await _db.Movies
                .AsNoTracking()
                .OrderByDescending(m => m.ViewCount)
                .Take(10)
                .ToListAsync();

            // Get rating information
            var userId = User.Identity?.IsAuthenticated == true ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;
            var userRating = userId != null ? await _ratingService.GetUserRatingAsync(userId, dbMovie.Id) : 0;
            var averageRating = await _ratingService.GetMovieAverageRatingAsync(dbMovie.Id);
            var totalRatings = await _ratingService.GetMovieTotalRatingsAsync(dbMovie.Id);

            ViewBag.MovieId = dbMovie.Id;
            ViewBag.UserRating = userRating;
            ViewBag.AverageRating = averageRating;
            ViewBag.TotalRatings = totalRatings;

            ViewBag.ApprovedComments = _db.Comments
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Replies)
                .Where(c => c.MovieId == dbMovie.Id && c.IsApproved && c.ParentCommentId == null)
                .OrderByDescending(c => c.Id)
                .ToList();
            return View(dbMovie);
        }

        [HttpGet("movie")]
        public IActionResult Detail()
        {
            // Redirect hoặc trả về trang lỗi tùy ý
            //return View("NotFound");
            return RedirectToAction("Index");
        }

        [HttpGet("watch/{slug}")]
        public async Task<IActionResult> Watch(string slug, int? ep = null, string? server = null)
        {
            var dbMovie = await _db.Movies.FirstOrDefaultAsync(m => m.Slug == slug);
            if (dbMovie == null)
            {
                // Smart fallback: redirect demo slug to a real movie with sources
                if (!string.IsNullOrWhiteSpace(slug) && slug.StartsWith("phim-demo", StringComparison.OrdinalIgnoreCase))
                {
                    // Try to parse TMDb id from demo slug: phim-demo-{id}
                    var parts = slug.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (parts.Length >= 3 && int.TryParse(parts[^1], out var tmdbId))
                    {
                        // If movie not exists, import minimal movie and a default source
                        var exists = await _db.Movies.AnyAsync(x => x.TMDbId == tmdbId);
                        if (!exists)
                        {
                            var details = await _tmdb.GetMovieDetailsByIdAsync(tmdbId);
                            if (details != null)
                            {
                                var movie = new Movie
                                {
                                    TMDbId = details.Id,
                                    Title = details.Title ?? details.Name ?? $"TMDb {tmdbId}",
                                    Slug = GenerateSlug(details.Title ?? details.Name ?? $"tmdb-{tmdbId}"),
                                    PosterUrl = _tmdb.GetImageUrl(details.PosterPath ?? ""),
                                    Imdb = details.VoteAverage,
                                    Year = !string.IsNullOrEmpty(details.ReleaseDate) && DateTime.TryParse(details.ReleaseDate, out var dy) ? (int?)dy.Year : null,
                                    Description = details.Overview,
                                    ReleaseDate = !string.IsNullOrEmpty(details.ReleaseDate) && DateTime.TryParse(details.ReleaseDate, out var d2) ? d2 : null,
                                    AgeRating = "PG-13",
                                    DurationMinutes = 120,
                                    Country = "US",
                                    CreatedAt = DateTime.UtcNow
                                };
                                if (details.Genres != null && details.Genres.Any())
                                {
                                    foreach (var gn in details.Genres)
                                    {
                                        var dbg = await _db.Genres.FirstOrDefaultAsync(x => x.Name == gn.Name);
                                        if (dbg != null)
                                        {
                                            movie.MovieGenres.Add(new MovieGenre { Genre = dbg });
                                        }
                                    }
                                }
                                _db.Movies.Add(movie);
                                await _db.SaveChangesAsync();

                                // Try to attach official trailer as default source if available
                                var trailerUrl = await _tmdb.GetMovieYoutubeTrailerEmbedAsync(details.Id);
                                if (!string.IsNullOrWhiteSpace(trailerUrl))
                                {
                                    var movieSource = new MovieSource
                                    {
                                        MovieId = movie.Id,
                                        ServerName = "Trailer",
                                        Quality = "HD",
                                        Language = "",
                                        Url = trailerUrl,
                                        IsDefault = true
                                    };
                                    _db.MovieSources.Add(movieSource);
                                    await _db.SaveChangesAsync();
                                }

                                return RedirectToAction("Watch", new { slug = movie.Slug, ep, server });
                            }
                        }

                        var byTmdb = await _db.Movies.FirstOrDefaultAsync(x => x.TMDbId == tmdbId);
                        if (byTmdb != null && !string.IsNullOrWhiteSpace(byTmdb.Slug))
                            return RedirectToAction("Watch", new { slug = byTmdb.Slug, ep, server });
                    }
                    else
                    {
                        var fallback = await _db.Movies
                            .OrderByDescending(m => m.ViewCount)
                            .FirstOrDefaultAsync();
                        if (fallback != null && !string.IsNullOrWhiteSpace(fallback.Slug))
                            return RedirectToAction("Watch", new { slug = fallback.Slug, ep, server });
                    }
                }
                return NotFound();
            }

            dbMovie.ViewCount += 1;
            await _db.SaveChangesAsync();

            // Track view và thưởng coin cho user đã đăng nhập
            var userId = User.Identity?.IsAuthenticated == true 
                ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                : null;
            
            if (!string.IsNullOrEmpty(userId))
            {
                // Kiểm tra xem đã xem phim hôm nay chưa (trước khi thêm view hit)
                var today = DateTime.UtcNow.Date;
                var todayView = await _db.ViewHits
                    .AnyAsync(v => v.MovieId == dbMovie.Id && v.UserId == userId && v.ViewedAt.Date == today);
                
                // Thêm vào watch history
                var viewHit = new ViewHit 
                { 
                    MovieId = dbMovie.Id, 
                    UserId = userId,
                    ViewedAt = DateTime.UtcNow 
                };
                _db.ViewHits.Add(viewHit);
                
                // Thưởng coin khi xem phim (chỉ thưởng 1 lần mỗi phim mỗi ngày)
                if (!todayView)
                {
                    await _coinService.RewardWatchMovieAsync(userId, dbMovie.Id, dbMovie.Title);
                    
                    // Cập nhật achievement
                    await _achievementService.CheckAndUpdateAchievementsAsync(userId, AchievementType.MoviesWatched);
                    
                    // Thêm vào inventory (watched)
                    var existingWatched = await _db.UserInventoryItems
                        .AnyAsync(ui => ui.UserId == userId && ui.MovieId == dbMovie.Id && ui.Type == InventoryItemType.Watched);
                    
                    if (!existingWatched)
                    {
                        _db.UserInventoryItems.Add(new UserInventoryItem
                        {
                            UserId = userId,
                            MovieId = dbMovie.Id,
                            Type = InventoryItemType.Watched,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                
                await _db.SaveChangesAsync();
            }
            else
            {
                // Anonymous user - chỉ track view hit
                _db.ViewHits.Add(new ViewHit 
                { 
                    MovieId = dbMovie.Id, 
                    ViewedAt = DateTime.UtcNow 
                });
                await _db.SaveChangesAsync();
            }

            // Determine episode list from MovieSources if available
            var allSources = await _db.MovieSources.AsNoTracking().Where(s => s.MovieId == dbMovie.Id).ToListAsync();
            var episodesFromDb = allSources.Where(s => s.EpisodeNumber.HasValue).Select(s => s.EpisodeNumber!.Value).Distinct().OrderBy(x => x).ToList();
            var selectedEp = ep ?? (episodesFromDb.Any() ? episodesFromDb.Min() : 1);

            if (allSources.Count == 0)
            {
                // Try get trailer from TMDb if available
                if (dbMovie.TMDbId.HasValue)
                {
                    var trailer = await _tmdb.GetMovieYoutubeTrailerEmbedAsync(dbMovie.TMDbId.Value);
                    if (!string.IsNullOrWhiteSpace(trailer))
                    {
                        var ms = new MovieSource
                        {
                            MovieId = dbMovie.Id,
                            ServerName = "Trailer",
                            Quality = "HD",
                            Language = "",
                            Url = trailer,
                            IsDefault = true,
                            EpisodeNumber = null
                        };
                        _db.MovieSources.Add(ms);
                        await _db.SaveChangesAsync();
                        allSources = new List<MovieSource> { ms };
                        ViewBag.UsingTrailer = true;
                    }
                }
            }
            else
            {
                ViewBag.UsingTrailer = allSources.All(s => (s.ServerName ?? "").Equals("Trailer", StringComparison.OrdinalIgnoreCase));
            }

            // Filter sources for selected episode (if any), otherwise all for movie
            var srcs = (episodesFromDb.Any())
                ? allSources.Where(s => (s.EpisodeNumber ?? selectedEp) == selectedEp).ToList()
                : allSources;
            ViewBag.Sources = srcs.OrderByDescending(s => s.IsDefault).ThenBy(s => s.ServerName).ToList();

            // Episodes for UI
            ViewBag.Episodes = episodesFromDb.Any() ? episodesFromDb : Enumerable.Range(1, dbMovie.IsSeries ? 12 : 1).ToList();
            ViewBag.SelectedEpisode = selectedEp;
            ViewBag.Servers = new[] { "Vietsub", "Thuyết minh", "Lồng tiếng" };
            ViewBag.SelectedServer = server ?? "Vietsub";

            // Load movie with genres
            dbMovie = await _db.Movies
                .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .FirstOrDefaultAsync(m => m.Id == dbMovie.Id);

            // Get comments
            ViewBag.ApprovedComments = await _db.Comments
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Replies)
                .ThenInclude(r => r.User)
                .Where(c => c.MovieId == dbMovie.Id && c.IsApproved && c.ParentCommentId == null)
                .OrderByDescending(c => c.Id)
                .Take(25)
                .ToListAsync();

            // Get recommended movies
            var recommendedMovies = await _recommendationService.GetSimilarMoviesAsync(dbMovie.Id, 6);
            ViewBag.RecommendedMovies = recommendedMovies;

            // Load subtitles
            var subtitles = await _subtitleService.GetMovieSubtitlesAsync(dbMovie.Id);
            ViewBag.Subtitles = subtitles;

            // Load user subtitle settings if authenticated
            if (!string.IsNullOrEmpty(userId))
            {
                var userSubtitleSettings = await _subtitleService.GetUserSubtitleSettingsAsync(userId);
                ViewBag.UserSubtitleSettings = userSubtitleSettings;

                // Load friend reviews and ratings
                var friendReviews = await _socialService.GetFriendReviewsAsync(userId, dbMovie.Id, 5);
                var friendRatings = await _socialService.GetFriendRatingsAsync(userId, dbMovie.Id);
                ViewBag.FriendReviews = friendReviews;
                ViewBag.FriendRatings = friendRatings;

                // Get share count
                var shareCount = await _socialService.GetShareCountAsync(dbMovie.Id);
                ViewBag.ShareCount = shareCount;
            }

            // Get top viewed movies for sidebar
            var topViewedMovies = await _db.Movies
                .AsNoTracking()
                .Where(m => m.Id != dbMovie.Id)
                .OrderByDescending(m => m.ViewCount)
                .Take(8)
                .ToListAsync();
            ViewBag.TopViewedMovies = topViewedMovies;

            // Get movies by same genre
            var genreIds = dbMovie.MovieGenres.Select(mg => mg.GenreId).ToList();
            var sameGenreMovies = await _db.Movies
                .AsNoTracking()
                .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .Where(m => m.Id != dbMovie.Id && m.MovieGenres.Any(mg => genreIds.Contains(mg.GenreId)))
                .OrderByDescending(m => m.ViewCount)
                .Take(6)
                .ToListAsync();
            ViewBag.SameGenreMovies = sameGenreMovies;

            // Get latest movies
            var latestMovies = await _db.Movies
                .AsNoTracking()
                .Where(m => m.Id != dbMovie.Id)
                .OrderByDescending(m => m.CreatedAt)
                .Take(6)
                .ToListAsync();
            ViewBag.LatestMovies = latestMovies;

            return View(dbMovie);
        }

        [HttpGet("search")]
        [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "q", "genre", "country", "year", "imdb", "age", "contentType" })] // Cache 5 phút
        public async Task<IActionResult> Search(string? q, string? genre, string? country, int? year, double? imdb, string? age, 
            string? director, string? cast, DateTime? releaseDateFrom, DateTime? releaseDateTo, double? ratingFrom, double? ratingTo,
            ContentType? contentType = null)
        {
            var query = _db.Movies
                .AsNoTracking()
                .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .AsQueryable();

            // Search by title
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(m => m.Title.Contains(q));
            }

            // Filter by genre
            if (!string.IsNullOrWhiteSpace(genre))
            {
                query = query.Where(m => m.MovieGenres.Any(mg => 
                    mg.Genre.Name.Contains(genre) || 
                    mg.Genre.Slug == genre));
            }

            // Filter by country
            if (!string.IsNullOrWhiteSpace(country))
            {
                query = query.Where(m => m.Country != null && m.Country.Contains(country));
            }

            // Filter by year
            if (year.HasValue)
            {
                query = query.Where(m => m.Year == year.Value);
            }

            // Filter by IMDb rating
            if (imdb.HasValue)
            {
                query = query.Where(m => m.Imdb.HasValue && m.Imdb >= imdb.Value);
            }

            // Filter by age rating
            if (!string.IsNullOrWhiteSpace(age))
            {
                query = query.Where(m => m.AgeRating != null && m.AgeRating.Contains(age));
            }

            // Filter by director
            if (!string.IsNullOrWhiteSpace(director))
            {
                query = query.Where(m => m.Director != null && m.Director.Contains(director));
            }

            // Filter by cast
            if (!string.IsNullOrWhiteSpace(cast))
            {
                query = query.Where(m => m.Cast != null && m.Cast.Contains(cast));
            }

            // Filter by release date range
            if (releaseDateFrom.HasValue)
            {
                query = query.Where(m => m.ReleaseDate.HasValue && m.ReleaseDate >= releaseDateFrom.Value);
            }

            if (releaseDateTo.HasValue)
            {
                query = query.Where(m => m.ReleaseDate.HasValue && m.ReleaseDate <= releaseDateTo.Value);
            }

            // Filter by rating range
            if (ratingFrom.HasValue)
            {
                query = query.Where(m => m.AverageRating.HasValue && m.AverageRating >= ratingFrom.Value);
            }

            if (ratingTo.HasValue)
            {
                query = query.Where(m => m.AverageRating.HasValue && m.AverageRating <= ratingTo.Value);
            }

            // Filter by content type
            if (contentType.HasValue)
            {
                query = query.Where(m => m.ContentType == contentType.Value);
            }

            // Order by view count (most popular first)
            query = query.OrderByDescending(m => m.ViewCount);

            var list = await query.Take(100).ToListAsync();

            // Enrich posters from TMDb if missing
            if (!string.IsNullOrWhiteSpace(q) && list.Any(m => string.IsNullOrWhiteSpace(m.PosterUrl)))
            {
                try
                {
                    var tm = await _tmdb.SearchMoviesParsedAsync(q);
                    var map = tm.Where(x => !string.IsNullOrWhiteSpace(x.Title))
                        .ToDictionary(x => x.Title!.Trim(), x => x, StringComparer.OrdinalIgnoreCase);
                    
                    foreach (var m in list)
                    {
                        if (string.IsNullOrWhiteSpace(m.PosterUrl) && 
                            map.TryGetValue(m.Title.Trim(), out var dto) && 
                            !string.IsNullOrWhiteSpace(dto.PosterPath))
                        {
                            m.PosterUrl = _tmdb.GetImageUrl(dto.PosterPath!, "w342");
                        }
                    }
                }
                catch
                {
                    // Ignore TMDb errors
                }
            }

            ViewBag.Query = new { q, genre, country, year, imdb, age };
            ViewBag.ResultCount = list.Count;
            return View(list);
        }

        [HttpGet("category/{slug}")]
        [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "slug", "page", "pageSize" })] // Cache 5 phút
        public async Task<IActionResult> Category(string slug, int page = 1, int pageSize = 24)
        {
            var genre = await _db.Genres.AsNoTracking().FirstOrDefaultAsync(g=>g.Slug==slug);
            if (genre == null) { ViewBag.Title = $"Danh mục: {slug}"; return View("Listing", new List<Movie>()); }
            var query = _db.Movies.AsNoTracking().Where(m=>m.MovieGenres.Any(x=>x.GenreId==genre.Id)).OrderByDescending(m=>m.Id);
            var list = await query.Take(pageSize).ToListAsync();
            // Enrich posters by TMDb if missing
            foreach (var m in list)
            {
                if (string.IsNullOrWhiteSpace(m.PosterUrl))
                {
                    var tm = await _tmdb.SearchMoviesParsedAsync(m.Title ?? string.Empty);
                    var dto = tm.FirstOrDefault(x=>!string.IsNullOrWhiteSpace(x.PosterPath));
                    if (dto != null) m.PosterUrl = _tmdb.GetImageUrl(dto.PosterPath!, "w342");
                }
            }
            ViewBag.Title = $"Danh mục: {genre.Name}";
            return View("Listing", list);
        }

        [HttpGet("tag/{slug}")]
        public async Task<IActionResult> Tag(string slug)
        {
            // For now, reuse genre slug mapping or search title contains
            var query = _db.Movies.AsNoTracking().Where(m=> (m.Slug ?? m.Title).Contains(slug));
            var list = await query.OrderByDescending(m=>m.Id).Take(24).ToListAsync();
            foreach (var m in list)
            {
                if (string.IsNullOrWhiteSpace(m.PosterUrl))
                {
                    var tm = await _tmdb.SearchMoviesParsedAsync(m.Title ?? string.Empty);
                    var dto = tm.FirstOrDefault(x=>!string.IsNullOrWhiteSpace(x.PosterPath));
                    if (dto != null) m.PosterUrl = _tmdb.GetImageUrl(dto.PosterPath!, "w342");
                }
            }
            ViewBag.Title = $"Chủ đề: {slug}";
            return View("Listing", list);
        }

        [HttpGet("tmdb/auth")]
        public async Task<IActionResult> TMDbAuth()
        {
            var apiUrl = "https://api.themoviedb.org/3/authentication";
            var bearerToken = "eyJhbGciOiJIUzI1NiJ9.eyJhdWQiOiI0OTEyNGM4MmFjMGIzMDRhMzc2Mzg1YjVhNDdhYTE4OCIsIm5iZiI6MTc2MTgwNjE4Mi43NTUsInN1YiI6IjY5MDMwNzY2NmJmNDVhMWMwYjA5Y2NiZiIsInNjb3BlcyI6WyJhcGlfcmVhZCJdLCJ2ZXJzaW9uIjoxfQ.wsEpKSMY-pFGWEgZx_ZKfjwS_qS69Z4H3e_LpFiNIxw";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");
                client.DefaultRequestHeaders.Add("accept", "application/json");

                var response = await client.GetAsync(apiUrl);
                var content = await response.Content.ReadAsStringAsync();

                // Trả toàn bộ JSON (nếu muốn parse thì có thể dùng JsonDocument/JsonConvert)
                return Content(content, "application/json");
            }
        }

        [HttpPost("movies/import-auto")]
        public async Task<IActionResult> ImportAuto(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { error = "Missing search keyword!" });

            // 1. Search TMDb phim theo từ khóa
            var tmdbMovies = await _tmdb.SearchMoviesParsedAsync(q);
            var main = tmdbMovies.FirstOrDefault();
            if (main == null)
                return NotFound(new { error = "No movie found from TMDb" });
            // 2. Lấy chi tiết phim từ TMDb
            var details = await _tmdb.GetMovieDetailsByIdAsync(main.Id);
            if (details == null)
                return NotFound(new { error = "Cannot fetch movie details from TMDb" });

            // 3. Kiểm tra đã tồn tại chưa (ưu tiên theo TMDbId)
            bool exists = await _db.Movies.AnyAsync(x => x.TMDbId == details.Id);
            if (!exists && !string.IsNullOrWhiteSpace(details.Title))
                exists = await _db.Movies.AnyAsync(x => x.Title == details.Title && x.TMDbId == null);
            if (exists)
                return Conflict(new { message = "Movie already exists in database" });

            // 4. Mapping dữ liệu TMDb sang Movie (có TMDbId)
            var duration = details.Runtime.HasValue && details.Runtime.Value > 0 ? details.Runtime.Value : 120;
            var country = "US";
            if (details.ProductionCountries != null && details.ProductionCountries.Any())
            {
                var countries = details.ProductionCountries.Select(c => c.Name ?? c.IsoCode).Where(c => !string.IsNullOrWhiteSpace(c));
                country = string.Join(", ", countries);
            }
            var castString = details.Cast != null && details.Cast.Any() 
                ? string.Join(", ", details.Cast.Take(10)) 
                : null;

            var movie = new Movie {
                TMDbId = details.Id,
                Title = details.Title ?? details.Name ?? "",
                Slug = GenerateSlug(details.Title ?? details.Name ?? ""),
                PosterUrl = _tmdb.GetImageUrl(details.PosterPath ?? ""),
                Imdb = details.VoteAverage,
                Year = !string.IsNullOrEmpty(details.ReleaseDate) && DateTime.TryParse(details.ReleaseDate, out var dy) ? (int?)dy.Year : null,
                Description = details.Overview,
                ReleaseDate = !string.IsNullOrEmpty(details.ReleaseDate) && DateTime.TryParse(details.ReleaseDate, out var d2) ? d2 : null,
                AgeRating = "PG-13",
                DurationMinutes = duration,
                Country = country,
                Cast = castString,
                Director = details.Director,
                CreatedAt = DateTime.UtcNow
            };
            // Thêm genre nếu có (tự động tạo genre nếu chưa có)
            if (details.Genres != null && details.Genres.Any()) {
                foreach (var gn in details.Genres) {
                    if (string.IsNullOrWhiteSpace(gn.Name)) continue;

                    var dbg = await _db.Genres.FirstOrDefaultAsync(x=>x.Name == gn.Name);
                    if (dbg == null) {
                        // Tự động tạo genre mới nếu chưa có
                        dbg = new Genre
                        {
                            Name = gn.Name,
                            Slug = gn.Name.ToLower()
                                .Replace(" ", "-")
                                .Replace("'", "")
                                .Replace(",", "")
                        };
                        _db.Genres.Add(dbg);
                        await _db.SaveChangesAsync(); // Save to get ID
                    }
                    movie.MovieGenres.Add(new MovieGenre{ Genre = dbg });
                }
            }

            // 5. Lưu database
            _db.Movies.Add(movie);
            await _db.SaveChangesAsync();
            
            // 6. Thêm movie source mặc định
            var movieSource = new Models.MovieSource
            {
                MovieId = movie.Id,
                ServerName = "YouTube",
                Quality = "720p",
                Language = "Vietsub",
                Url = "https://www.youtube.com/embed/dQw4w9WgXcQ", // Placeholder
                IsDefault = true
            };
            _db.MovieSources.Add(movieSource);
            await _db.SaveChangesAsync();
            
            return Ok(new { message = "Movie imported successfully", movie });
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
    }
}


