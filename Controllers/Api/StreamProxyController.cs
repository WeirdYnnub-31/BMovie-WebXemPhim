using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using webxemphim.Services;

namespace webxemphim.Controllers.Api
{
    /// <summary>
    /// Controller để proxy video streams với bảo vệ token
    /// </summary>
    [ApiController]
    [Route("api/stream")]
    public class StreamProxyController : ControllerBase
    {
        private readonly StreamProtectionService _streamProtection;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<StreamProxyController> _logger;

        public StreamProxyController(
            StreamProtectionService streamProtection,
            IHttpClientFactory httpClientFactory,
            ILogger<StreamProxyController> logger)
        {
            _streamProtection = streamProtection;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Proxy video stream với token protection
        /// </summary>
        [HttpGet("proxy")]
        [Authorize(AuthenticationSchemes = "JwtBearer,Identity.Application")] // Yêu cầu đăng nhập (JWT hoặc Cookie)
        public async Task<IActionResult> ProxyStream([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { error = "Token is required" });
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var (isValid, videoUrl, error) = _streamProtection.ValidateToken(token, userId);

            if (!isValid || string.IsNullOrWhiteSpace(videoUrl))
            {
                _logger.LogWarning("Invalid stream token: {Error}", error);
                return Unauthorized(new { error = error ?? "Invalid token" });
            }

            try
            {
                // Proxy video stream
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromMinutes(10); // Timeout cho video streams

                var response = await client.GetAsync(videoUrl, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new { error = "Failed to fetch video" });
                }

                // Copy headers
                Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "video/mp4";
                if (response.Content.Headers.ContentLength.HasValue)
                {
                    Response.ContentLength = response.Content.Headers.ContentLength.Value;
                }

                // Copy stream
                await response.Content.CopyToAsync(Response.Body);
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error proxying stream");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Lấy protected URL cho video
        /// </summary>
        [HttpPost("protected-url")]
        [Authorize(AuthenticationSchemes = "JwtBearer,Identity.Application")]
        public IActionResult GetProtectedUrl([FromBody] ProtectedUrlRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.VideoUrl))
            {
                return BadRequest(new { error = "VideoUrl is required" });
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var protectedUrl = _streamProtection.CreateProtectedUrl(baseUrl, request.VideoUrl, userId);

            return Ok(new { protectedUrl, expiresAt = DateTime.UtcNow.AddMinutes(30) });
        }
    }

    public class ProtectedUrlRequest
    {
        public string VideoUrl { get; set; } = string.Empty;
    }
}

