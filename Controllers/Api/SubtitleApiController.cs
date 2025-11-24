using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Data;
using webxemphim.Models;
using webxemphim.Services;

namespace webxemphim.Controllers.Api
{
    [ApiController]
    [Route("api/subtitles")]
    public class SubtitleApiController : ControllerBase
    {
        private readonly SubtitleService _subtitleService;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SubtitleApiController> _logger;

        public SubtitleApiController(SubtitleService subtitleService, ApplicationDbContext db, ILogger<SubtitleApiController> logger)
        {
            _subtitleService = subtitleService;
            _db = db;
            _logger = logger;
        }

        [HttpGet("movie/{movieId}")]
        public async Task<IActionResult> GetMovieSubtitles(int movieId)
        {
            var subtitles = await _subtitleService.GetMovieSubtitlesAsync(movieId);
            return Ok(subtitles);
        }

        [HttpGet("{subtitleId}")]
        public async Task<IActionResult> GetSubtitle(int subtitleId)
        {
            var subtitle = await _subtitleService.GetSubtitleAsync(subtitleId);
            if (subtitle == null) return NotFound();
            return Ok(subtitle);
        }

        [Authorize]
        [HttpPost("movie/{movieId}")]
        public async Task<IActionResult> AddSubtitle(int movieId, [FromBody] AddSubtitleRequest request)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var movie = await _db.Movies.FindAsync(movieId);
            if (movie == null) return NotFound("Movie not found");

            try
            {
                var subtitle = await _subtitleService.AddSubtitleAsync(
                    movieId,
                    request.Language,
                    request.LanguageName,
                    request.FileUrl,
                    request.FilePath,
                    request.IsDefault,
                    false,
                    userId
                );

                return Ok(subtitle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding subtitle");
                return StatusCode(500, new { error = "Failed to add subtitle" });
            }
        }

        [Authorize]
        [HttpDelete("{subtitleId}")]
        public async Task<IActionResult> DeleteSubtitle(int subtitleId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var success = await _subtitleService.DeleteSubtitleAsync(subtitleId, userId);
            if (!success) return NotFound();

            return Ok(new { message = "Subtitle deleted successfully" });
        }

        [Authorize]
        [HttpPost("{subtitleId}/set-default")]
        public async Task<IActionResult> SetDefaultSubtitle(int subtitleId, [FromBody] SetDefaultRequest request)
        {
            var success = await _subtitleService.SetDefaultSubtitleAsync(subtitleId, request.MovieId);
            if (!success) return NotFound();

            return Ok(new { message = "Default subtitle set successfully" });
        }

        [Authorize]
        [HttpGet("settings")]
        public async Task<IActionResult> GetUserSettings()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var settings = await _subtitleService.GetOrCreateUserSubtitleSettingsAsync(userId);
            return Ok(settings);
        }

        [Authorize]
        [HttpPost("settings")]
        public async Task<IActionResult> UpdateUserSettings([FromBody] UpdateSubtitleSettingsRequest request)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var settings = await _subtitleService.UpdateUserSubtitleSettingsAsync(
                userId,
                request.FontFamily,
                request.FontSize,
                request.FontColor,
                request.BackgroundColor,
                request.BackgroundOpacity,
                request.Position,
                request.ShowBackground,
                request.PreferredLanguage
            );

            return Ok(settings);
        }

        [HttpGet("movie/{movieId}/languages")]
        public async Task<IActionResult> GetAvailableLanguages(int movieId)
        {
            var languages = await _subtitleService.GetAvailableLanguagesAsync(movieId);
            return Ok(languages);
        }
    }

    public class AddSubtitleRequest
    {
        public string Language { get; set; } = string.Empty;
        public string LanguageName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public bool IsDefault { get; set; } = false;
    }

    public class SetDefaultRequest
    {
        public int MovieId { get; set; }
    }

    public class UpdateSubtitleSettingsRequest
    {
        public string? FontFamily { get; set; }
        public int? FontSize { get; set; }
        public string? FontColor { get; set; }
        public string? BackgroundColor { get; set; }
        public double? BackgroundOpacity { get; set; }
        public string? Position { get; set; }
        public bool? ShowBackground { get; set; }
        public string? PreferredLanguage { get; set; }
    }
}

