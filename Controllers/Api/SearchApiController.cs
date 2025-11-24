using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Data;
using webxemphim.Services;

namespace webxemphim.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public SearchApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// API autocomplete cho tìm kiếm phim
        /// </summary>
        [HttpGet("autocomplete")]
        [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "q", "limit" })] // Cache 1 phút
        [ProducesResponseType(typeof(List<MovieAutocompleteDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Autocomplete([FromQuery] string q, [FromQuery] int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return Ok(new List<MovieAutocompleteDto>());
            }

            var query = q.Trim();
            var normalizedQuery = VietnameseSearchService.RemoveDiacritics(query).ToLower();

            // Tìm kiếm trong database với hỗ trợ tiếng Việt không dấu
            var movies = await _db.Movies
                .AsNoTracking()
                .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .Where(m => 
                    // Tìm kiếm có dấu
                    m.Title.Contains(query) ||
                    // Tìm kiếm không dấu (sử dụng hàm RemoveDiacritics trong memory)
                    VietnameseSearchService.RemoveDiacritics(m.Title).ToLower().Contains(normalizedQuery) ||
                    // Tìm kiếm trong description
                    (!string.IsNullOrEmpty(m.Description) && (
                        m.Description.Contains(query) ||
                        VietnameseSearchService.RemoveDiacritics(m.Description).ToLower().Contains(normalizedQuery)
                    ))
                )
                .OrderByDescending(m => m.ViewCount)
                .ThenByDescending(m => m.AverageRating)
                .Take(limit)
                .Select(m => new MovieAutocompleteDto
                {
                    Id = m.Id,
                    Title = m.Title,
                    Slug = m.Slug ?? "",
                    PosterUrl = m.PosterUrl ?? "",
                    Year = m.Year,
                    Imdb = m.Imdb,
                    IsSeries = m.IsSeries,
                    Genres = m.MovieGenres.Select(mg => mg.Genre.Name).ToList()
                })
                .ToListAsync();

            return Ok(movies);
        }

        /// <summary>
        /// API tìm kiếm nâng cao với pagination
        /// </summary>
        [HttpGet("advanced")]
        [ResponseCache(Duration = 180, VaryByQueryKeys = new[] { "q", "genre", "year", "imdbMin", "page", "pageSize" })] // Cache 3 phút
        [ProducesResponseType(typeof(PagedSearchResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> AdvancedSearch(
            [FromQuery] string? q,
            [FromQuery] string? genre,
            [FromQuery] int? year,
            [FromQuery] double? imdbMin,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _db.Movies
                .AsNoTracking()
                .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .AsQueryable();

            // Tìm kiếm theo tiêu đề (hỗ trợ tiếng Việt không dấu)
            if (!string.IsNullOrWhiteSpace(q))
            {
                var normalizedQuery = VietnameseSearchService.RemoveDiacritics(q).ToLower();
                query = query.Where(m => 
                    m.Title.Contains(q) ||
                    VietnameseSearchService.RemoveDiacritics(m.Title).ToLower().Contains(normalizedQuery)
                );
            }

            // Lọc theo thể loại
            if (!string.IsNullOrWhiteSpace(genre))
            {
                query = query.Where(m => m.MovieGenres.Any(mg => 
                    mg.Genre.Name == genre || 
                    mg.Genre.Slug == genre ||
                    VietnameseSearchService.RemoveDiacritics(mg.Genre.Name).ToLower().Contains(
                        VietnameseSearchService.RemoveDiacritics(genre).ToLower())
                ));
            }

            // Lọc theo năm
            if (year.HasValue)
            {
                query = query.Where(m => m.Year == year.Value);
            }

            // Lọc theo IMDb rating
            if (imdbMin.HasValue)
            {
                query = query.Where(m => m.Imdb.HasValue && m.Imdb >= imdbMin.Value);
            }

            var totalCount = await query.CountAsync();
            var movies = await query
                .OrderByDescending(m => m.ViewCount)
                .ThenByDescending(m => m.AverageRating)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MovieSearchResultDto
                {
                    Id = m.Id,
                    Title = m.Title,
                    Slug = m.Slug ?? "",
                    PosterUrl = m.PosterUrl ?? "",
                    Year = m.Year,
                    Imdb = m.Imdb,
                    AverageRating = m.AverageRating,
                    TotalRatings = m.TotalRatings,
                    IsSeries = m.IsSeries,
                    Genres = m.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
                    ViewCount = m.ViewCount
                })
                .ToListAsync();

            return Ok(new PagedSearchResultDto
            {
                Data = movies,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
    }

    public class MovieAutocompleteDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string PosterUrl { get; set; } = string.Empty;
        public int? Year { get; set; }
        public double? Imdb { get; set; }
        public bool IsSeries { get; set; }
        public List<string> Genres { get; set; } = new();
    }

    public class MovieSearchResultDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string PosterUrl { get; set; } = string.Empty;
        public int? Year { get; set; }
        public double? Imdb { get; set; }
        public double? AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public bool IsSeries { get; set; }
        public List<string> Genres { get; set; } = new();
        public long ViewCount { get; set; }
    }

    public class PagedSearchResultDto
    {
        public List<MovieSearchResultDto> Data { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}

