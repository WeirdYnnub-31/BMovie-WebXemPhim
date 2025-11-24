using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using webxemphim.Models;
using webxemphim.Models.DTOs;
using webxemphim.Services;

namespace webxemphim.Controllers.Api
{
    [ApiController]
    [Route("api/v2/recommendations")]
    [Produces("application/json")]
    public class RecommendationsApiController : ControllerBase
    {
        private readonly RecommendationService _recommendationService;
        private readonly ILogger<RecommendationsApiController> _logger;

        public RecommendationsApiController(
            RecommendationService recommendationService,
            ILogger<RecommendationsApiController> logger)
        {
            _recommendationService = recommendationService;
            _logger = logger;
        }

        /// <summary>
        /// Get personalized movie recommendations for the current user
        /// </summary>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(List<MovieDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRecommendations([FromQuery] int limit = 10)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var recommendations = await _recommendationService.GetRecommendationsAsync(userId, limit);
                var result = recommendations.Select(m => new MovieDto
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
                    AverageRating = m.AverageRating,
                    Genres = m.MovieGenres.Select(mg => mg.Genre.Name).ToList()
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommendations for user");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get movie recommendations for anonymous users (trending/popular)
        /// </summary>
        [HttpGet("trending")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<MovieDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTrending([FromQuery] int limit = 10)
        {
            try
            {
                var movies = await _recommendationService.GetTrendingMoviesAsync(limit);
                var result = movies.Select(m => new MovieDto
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
                    AverageRating = m.AverageRating,
                    Genres = m.MovieGenres.Select(mg => mg.Genre.Name).ToList()
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trending movies");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get new releases
        /// </summary>
        [HttpGet("new-releases")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<MovieDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNewReleases([FromQuery] int limit = 10)
        {
            try
            {
                var movies = await _recommendationService.GetNewReleasesAsync(limit);
                var result = movies.Select(m => new MovieDto
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
                    AverageRating = m.AverageRating,
                    Genres = m.MovieGenres.Select(mg => mg.Genre.Name).ToList()
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting new releases");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get similar movies to a specific movie
        /// </summary>
        [HttpGet("similar/{movieId}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<MovieDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSimilarMovies(int movieId, [FromQuery] int limit = 6)
        {
            try
            {
                var movies = await _recommendationService.GetSimilarMoviesAsync(movieId, limit);
                var result = movies.Select(m => new MovieDto
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
                    AverageRating = m.AverageRating,
                    Genres = m.MovieGenres.Select(mg => mg.Genre.Name).ToList()
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting similar movies for movie {MovieId}", movieId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get recommendations for anonymous users (popular movies)
        /// </summary>
        [HttpGet("anonymous")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<MovieDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAnonymousRecommendations([FromQuery] int limit = 10)
        {
            try
            {
                var movies = await _recommendationService.GetRecommendationsAsync(null, limit);
                var result = movies.Select(m => new MovieDto
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
                    AverageRating = m.AverageRating,
                    Genres = m.MovieGenres.Select(mg => mg.Genre.Name).ToList()
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting anonymous recommendations");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}

