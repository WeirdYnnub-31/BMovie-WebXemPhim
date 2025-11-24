using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Data;
using webxemphim.Models;
using webxemphim.Models.DTOs;

namespace webxemphim.Controllers.Api
{
    [ApiController]
    [Route("api/public")]
    public class PublicApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<PublicApiController> _logger;

        public PublicApiController(ApplicationDbContext db, ILogger<PublicApiController> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Get list of movies (public API)
        /// </summary>
        [HttpGet("movies")]
        [ProducesResponseType(typeof(PagedResultDto<MovieListItemDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResultDto<MovieListItemDto>>> GetMovies(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? genre = null,
            [FromQuery] string? search = null,
            [FromQuery] ContentType? contentType = null)
        {
            try
            {
                var query = _db.Movies
                    .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(m => m.Title.Contains(search));
                }

                if (!string.IsNullOrWhiteSpace(genre))
                {
                    query = query.Where(m => m.MovieGenres.Any(mg => 
                        mg.Genre.Name.Contains(genre) || mg.Genre.Slug == genre));
                }

                if (contentType.HasValue)
                {
                    query = query.Where(m => m.ContentType == contentType.Value);
                }

                query = query.OrderByDescending(m => m.ViewCount);

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
                _logger.LogError(ex, "Error in GetMovies");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get movie details by ID or slug
        /// </summary>
        [HttpGet("movies/{idOrSlug}")]
        [ProducesResponseType(typeof(MovieDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<MovieDto>> GetMovie(string idOrSlug)
        {
            try
            {
                Movie? movie = null;
                
                // Try to parse as ID first
                if (int.TryParse(idOrSlug, out int movieId))
                {
                    movie = await _db.Movies
                        .Include(m => m.MovieGenres)
                        .ThenInclude(mg => mg.Genre)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.Id == movieId);
                }
                else
                {
                    // Try as slug
                    movie = await _db.Movies
                        .Include(m => m.MovieGenres)
                        .ThenInclude(mg => mg.Genre)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.Slug == idOrSlug);
                }

                if (movie == null)
                {
                    return NotFound(new { error = "Movie not found" });
                }

                var result = new MovieDto
                {
                    Id = movie.Id,
                    Title = movie.Title,
                    Slug = movie.Slug,
                    PosterUrl = movie.PosterUrl,
                    TrailerUrl = movie.TrailerUrl,
                    Imdb = movie.Imdb,
                    Year = movie.Year,
                    AgeRating = movie.AgeRating,
                    DurationMinutes = movie.DurationMinutes,
                    Country = movie.Country,
                    Description = movie.Description,
                    IsSeries = movie.IsSeries,
                    ContentType = movie.ContentType.ToString(),
                    Genres = movie.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
                    ViewCount = movie.ViewCount,
                    AverageRating = movie.AverageRating,
                    TotalRatings = movie.TotalRatings,
                    Director = movie.Director,
                    Cast = movie.Cast ?? string.Empty,
                    ReleaseDate = movie.ReleaseDate,
                    TMDbId = movie.TMDbId,
                    CreatedAt = movie.CreatedAt
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMovie");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get genres list
        /// </summary>
        [HttpGet("genres")]
        public async Task<ActionResult<List<GenreDto>>> GetGenres()
        {
            try
            {
                var genres = await _db.Genres
                    .AsNoTracking()
                    .OrderBy(g => g.Name)
                    .ToListAsync();

                var result = genres.Select(g => new GenreDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Slug = g.Slug
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetGenres");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get trending movies
        /// </summary>
        [HttpGet("movies/trending")]
        public async Task<ActionResult<List<MovieListItemDto>>> GetTrendingMovies([FromQuery] int limit = 10)
        {
            try
            {
                var movies = await _db.Movies
                    .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                    .AsNoTracking()
                    .OrderByDescending(m => m.ViewCount)
                    .Take(limit)
                    .ToListAsync();

                var result = movies.Select(m => new MovieListItemDto
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
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTrendingMovies");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }

}

