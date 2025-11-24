using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using webxemphim.Services;

namespace webxemphim.Controllers.Api
{
    [ApiController]
    [Route("api/download")]
    [Authorize]
    public class DownloadApiController : ControllerBase
    {
        private readonly DownloadProtectionService _downloadService;
        private readonly ILogger<DownloadApiController> _logger;

        public DownloadApiController(DownloadProtectionService downloadService, ILogger<DownloadApiController> logger)
        {
            _downloadService = downloadService;
            _logger = logger;
        }

        [HttpPost("token/{movieId}")]
        public async Task<IActionResult> GenerateDownloadToken(int movieId, [FromBody] DownloadRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                // Kiểm tra user có quyền download không
                var canDownload = await _downloadService.CanUserDownloadAsync(userId, movieId);
                if (!canDownload)
                {
                    return BadRequest(new { error = "Bạn không có quyền download phim này hoặc không đủ coin." });
                }

                var token = await _downloadService.GenerateDownloadTokenAsync(userId, movieId, request.MovieSourceUrl);
                return Ok(new { token, expiresIn = 1800 }); // 30 minutes in seconds
            }
            catch (UnauthorizedAccessException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating download token");
                return StatusCode(500, new { error = "Lỗi khi tạo token download." });
            }
        }

        [HttpGet("verify/{token}")]
        public async Task<IActionResult> VerifyToken(string token)
        {
            var tokenInfo = await _downloadService.ValidateDownloadTokenAsync(token);
            if (tokenInfo == null)
            {
                return BadRequest(new { error = "Token không hợp lệ hoặc đã hết hạn." });
            }

            return Ok(new
            {
                valid = true,
                movieId = tokenInfo.MovieId,
                movieSourceUrl = tokenInfo.MovieSourceUrl,
                expiresAt = tokenInfo.ExpiresAt
            });
        }
    }

    public class DownloadRequest
    {
        public string MovieSourceUrl { get; set; } = string.Empty;
    }
}

