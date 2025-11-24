using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Data;
using webxemphim.Models;
using webxemphim.Models.DTOs;
using webxemphim.Services;

namespace webxemphim.Controllers.Api
{
    [ApiController]
    [Route("api/v2/movies")]
    [Produces("application/json")]
    public class MoviesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly TMDbService _tmdb;
        private readonly RecommendationService _recommendationService;

        public MoviesApiController(
            ApplicationDbContext db,
            TMDbService tmdb,
            RecommendationService recommendationService)
        {
            _db = db;
            _tmdb = tmdb;
            _recommendationService = recommendationService;
        }

        /// <summary>
        /// Get list of movies with pagination
        /// </summary>
        [HttpGet]
        [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "page", "pageSize", "genre", "search", "sortBy" })] // Cache 5 phút
        [ProducesResponseType(typeof(PagedResultDto<MovieListItemDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResultDto<MovieListItemDto>>> GetMovies(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? genre = null,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "viewcount") // viewcount, rating, date
        {
            try
            {
                var query = _db.Movies
                    .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                    .AsNoTracking()
                    .AsQueryable();

                // Filter by genre
                if (!string.IsNullOrWhiteSpace(genre))
                {
                    query = query.Where(m => m.MovieGenres.Any(mg => mg.Genre.Slug == genre || mg.Genre.Name == genre));
                }

                // Search by title
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(m => m.Title.Contains(search));
                }

                // Sorting
                query = sortBy.ToLower() switch
                {
                    "rating" => query.OrderByDescending(m => m.AverageRating),
                    "date" => query.OrderByDescending(m => m.ReleaseDate ?? m.CreatedAt),
                    _ => query.OrderByDescending(m => m.ViewCount)
                };

                var totalCount = await query.CountAsync();
                var movies = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = new PagedResultDto<MovieListItemDto>
                {
                    Data = movies.Select(m => new MovieListItemDto
                    {
                        Id = m.Id,
                        Title = m.Title,
                        Slug = m.Slug,
                        PosterUrl = m.PosterUrl,
                        Imdb = m.Imdb,
                        Year = m.Year,
                        AgeRating = m.AgeRating,
                        IsSeries = m.IsSeries,
                        ContentType = m.ContentType.ToString(),
                        Genres = m.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
                        ViewCount = m.ViewCount,
                        AverageRating = m.AverageRating
                    }).ToList(),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while fetching movies", message = ex.Message });
            }
        }

        /// <summary>
        /// Get a movie by slug
        /// </summary>
        [HttpGet("{slug}")]
        [ProducesResponseType(typeof(MovieDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MovieDto>> GetMovieBySlug(string slug)
        {
            try
            {
                var movie = await _db.Movies
                    .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Slug == slug);

                if (movie == null)
                    return NotFound(new { error = "Movie not found" });

                var result = new MovieDto
                {
                    Id = movie.Id,
                    Title = movie.Title,
                    Slug = movie.Slug,
                    PosterUrl = movie.PosterUrl,
                    TrailerUrl = movie.TrailerUrl,
                    Imdb = movie.Imdb,
                    AgeRating = movie.AgeRating,
                    DurationMinutes = movie.DurationMinutes,
                    Year = movie.Year,
                    Country = movie.Country,
                    Description = movie.Description,
                    IsSeries = movie.IsSeries,
                    Genres = movie.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
                    ViewCount = movie.ViewCount,
                    AverageRating = movie.AverageRating,
                    TotalRatings = movie.TotalRatings,
                    Director = movie.Director,
                    Cast = movie.Cast,
                    ReleaseDate = movie.ReleaseDate,
                    TMDbId = movie.TMDbId,
                    CreatedAt = movie.CreatedAt
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }

        /// <summary>
        /// Get movies by ID
        /// </summary>
        [HttpGet("id/{id}")]
        [ProducesResponseType(typeof(MovieDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MovieDto>> GetMovieById(int id)
        {
            try
            {
                var movie = await _db.Movies
                    .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (movie == null)
                    return NotFound(new { error = "Movie not found" });

                var result = new MovieDto
                {
                    Id = movie.Id,
                    Title = movie.Title,
                    Slug = movie.Slug,
                    PosterUrl = movie.PosterUrl,
                    TrailerUrl = movie.TrailerUrl,
                    Imdb = movie.Imdb,
                    AgeRating = movie.AgeRating,
                    DurationMinutes = movie.DurationMinutes,
                    Year = movie.Year,
                    Country = movie.Country,
                    Description = movie.Description,
                    IsSeries = movie.IsSeries,
                    Genres = movie.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
                    ViewCount = movie.ViewCount,
                    AverageRating = movie.AverageRating,
                    TotalRatings = movie.TotalRatings,
                    Director = movie.Director,
                    Cast = movie.Cast,
                    ReleaseDate = movie.ReleaseDate,
                    TMDbId = movie.TMDbId,
                    CreatedAt = movie.CreatedAt
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }

        /// <summary>
        /// Get similar movies based on a movie ID
        /// </summary>
        [HttpGet("{id}/similar")]
        [ProducesResponseType(typeof(List<MovieListItemDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<MovieListItemDto>>> GetSimilarMovies(int id, [FromQuery] int limit = 8)
        {
            try
            {
                var similarMovies = await _recommendationService.GetSimilarMoviesAsync(id, limit);
                var result = similarMovies.Select(m => new MovieListItemDto
                {
                    Id = m.Id,
                    Title = m.Title,
                    Slug = m.Slug,
                    PosterUrl = m.PosterUrl,
                    Imdb = m.Imdb,
                    Year = m.Year,
                    AgeRating = m.AgeRating,
                    IsSeries = m.IsSeries,
                    ViewCount = m.ViewCount,
                    AverageRating = m.AverageRating
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }

        /// <summary>
        /// Search movies by query
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(PagedResultDto<MovieListItemDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResultDto<MovieListItemDto>>> SearchMovies(
            [FromQuery] string q,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _db.Movies
                    .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                    .AsNoTracking()
                    .Where(m => m.Title.Contains(q))
                    .OrderByDescending(m => m.ViewCount);

                var totalCount = await query.CountAsync();
                var movies = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = new PagedResultDto<MovieListItemDto>
                {
                    Data = movies.Select(m => new MovieListItemDto
                    {
                        Id = m.Id,
                        Title = m.Title,
                        Slug = m.Slug,
                        PosterUrl = m.PosterUrl,
                        Imdb = m.Imdb,
                        Year = m.Year,
                        AgeRating = m.AgeRating,
                        IsSeries = m.IsSeries,
                        ContentType = m.ContentType.ToString(),
                        Genres = m.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
                        ViewCount = m.ViewCount,
                        AverageRating = m.AverageRating
                    }).ToList(),
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }

        /// <summary>
        /// Create a new movie with optional movie sources (m3u8, mpd, embed links) (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(MovieDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<MovieDto>> CreateMovie([FromBody] CreateMovieDto dto)
        {
            try
            {
                // Validate movie sources if provided
                if (dto.Sources != null && dto.Sources.Any())
                {
                    foreach (var source in dto.Sources)
                    {
                        if (!IsValidSourceUrl(source.Url))
                        {
                            return BadRequest(new { error = $"Invalid source URL: {source.Url}. URL must be a valid http(s) link ending with .m3u8, .mpd, or contain /embed/, iframe, player.vimeo.com, or youtube.com/embed" });
                        }
                    }
                }

                var movie = new Movie
                {
                    Title = dto.Title,
                    Slug = dto.Slug ?? GenerateSlug(dto.Title),
                    PosterUrl = dto.PosterUrl,
                    TrailerUrl = dto.TrailerUrl,
                    Imdb = dto.Imdb,
                    AgeRating = dto.AgeRating,
                    DurationMinutes = dto.DurationMinutes,
                    Year = dto.Year,
                    Country = dto.Country,
                    Description = dto.Description,
                    IsSeries = dto.IsSeries,
                    Director = dto.Director,
                    Cast = dto.Cast,
                    ReleaseDate = dto.ReleaseDate,
                    TMDbId = dto.TMDbId,
                    CreatedAt = DateTime.UtcNow
                };

                if (dto.GenreIds.Any())
                {
                    foreach (var genreId in dto.GenreIds)
                    {
                        var genre = await _db.Genres.FindAsync(genreId);
                        if (genre != null)
                        {
                            movie.MovieGenres.Add(new MovieGenre { Genre = genre });
                        }
                    }
                }

                _db.Movies.Add(movie);
                await _db.SaveChangesAsync();

                // Add movie sources if provided
                if (dto.Sources != null && dto.Sources.Any())
                {
                    // Unset existing defaults if any source is marked as default
                    var hasDefault = dto.Sources.Any(s => s.IsDefault);
                    if (hasDefault)
                    {
                        // This is a new movie, so no existing sources, but we ensure only one default
                        var defaultCount = dto.Sources.Count(s => s.IsDefault);
                        if (defaultCount > 1)
                        {
                            // Only the first default will be kept
                            var firstDefault = true;
                            foreach (var sourceDto in dto.Sources)
                            {
                                if (sourceDto.IsDefault && !firstDefault)
                                {
                                    sourceDto.IsDefault = false;
                                }
                                if (sourceDto.IsDefault) firstDefault = false;
                            }
                        }
                    }

                    foreach (var sourceDto in dto.Sources)
                    {
                        _db.MovieSources.Add(new MovieSource
                        {
                            MovieId = movie.Id,
                            ServerName = sourceDto.ServerName ?? "Server 1",
                            Quality = sourceDto.Quality ?? "1080p",
                            Language = sourceDto.Language ?? "Vietsub",
                            Url = sourceDto.Url,
                            IsDefault = sourceDto.IsDefault
                        });
                    }
                    await _db.SaveChangesAsync();
                }

                var result = new MovieDto
                {
                    Id = movie.Id,
                    Title = movie.Title,
                    Slug = movie.Slug,
                    PosterUrl = movie.PosterUrl,
                    TrailerUrl = movie.TrailerUrl,
                    Imdb = movie.Imdb,
                    AgeRating = movie.AgeRating,
                    DurationMinutes = movie.DurationMinutes,
                    Year = movie.Year,
                    Country = movie.Country,
                    Description = movie.Description,
                    IsSeries = movie.IsSeries,
                    Genres = movie.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
                    ViewCount = movie.ViewCount,
                    AverageRating = movie.AverageRating,
                    TotalRatings = movie.TotalRatings,
                    Director = movie.Director,
                    Cast = movie.Cast,
                    ReleaseDate = movie.ReleaseDate,
                    TMDbId = movie.TMDbId,
                    CreatedAt = movie.CreatedAt
                };

                return CreatedAtAction(nameof(GetMovieById), new { id = movie.Id }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }

        private static bool IsValidSourceUrl(string? url)
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

        private static string GenerateSlug(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return "movie";
            return System.Text.RegularExpressions.Regex.Replace(
                title.ToLower()
                    .Normalize(System.Text.NormalizationForm.FormD)
                    .Replace("đ", "d")
                    .Replace("Đ", "D"),
                @"[^a-z0-9\s-]", "")
                .Trim()
                .Replace(" ", "-")
                .Replace("-+", "-");
        }

        /// <summary>
        /// Update a movie (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(MovieDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<MovieDto>> UpdateMovie(int id, [FromBody] UpdateMovieDto dto)
        {
            try
            {
                var movie = await _db.Movies
                    .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (movie == null)
                    return NotFound(new { error = "Movie not found" });

                movie.Title = dto.Title;
                movie.Slug = dto.Slug ?? movie.Slug;
                movie.PosterUrl = dto.PosterUrl;
                movie.TrailerUrl = dto.TrailerUrl;
                movie.Imdb = dto.Imdb;
                movie.AgeRating = dto.AgeRating;
                movie.DurationMinutes = dto.DurationMinutes;
                movie.Year = dto.Year;
                movie.Country = dto.Country;
                movie.Description = dto.Description;
                movie.IsSeries = dto.IsSeries;
                movie.Director = dto.Director;
                movie.Cast = dto.Cast;
                movie.ReleaseDate = dto.ReleaseDate;

                // Update genres
                movie.MovieGenres.Clear();
                if (dto.GenreIds.Any())
                {
                    foreach (var genreId in dto.GenreIds)
                    {
                        var genre = await _db.Genres.FindAsync(genreId);
                        if (genre != null)
                        {
                            movie.MovieGenres.Add(new MovieGenre { Genre = genre });
                        }
                    }
                }

                await _db.SaveChangesAsync();

                var result = new MovieDto
                {
                    Id = movie.Id,
                    Title = movie.Title,
                    Slug = movie.Slug,
                    PosterUrl = movie.PosterUrl,
                    TrailerUrl = movie.TrailerUrl,
                    Imdb = movie.Imdb,
                    AgeRating = movie.AgeRating,
                    DurationMinutes = movie.DurationMinutes,
                    Year = movie.Year,
                    Country = movie.Country,
                    Description = movie.Description,
                    IsSeries = movie.IsSeries,
                    Genres = movie.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
                    ViewCount = movie.ViewCount,
                    AverageRating = movie.AverageRating,
                    TotalRatings = movie.TotalRatings,
                    Director = movie.Director,
                    Cast = movie.Cast,
                    ReleaseDate = movie.ReleaseDate,
                    TMDbId = movie.TMDbId,
                    CreatedAt = movie.CreatedAt
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a movie (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteMovie(int id)
        {
            try
            {
                var movie = await _db.Movies.FindAsync(id);
                if (movie == null)
                    return NotFound(new { error = "Movie not found" });

                _db.Movies.Remove(movie);
                await _db.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", message = ex.Message });
            }
        }
    }
}

