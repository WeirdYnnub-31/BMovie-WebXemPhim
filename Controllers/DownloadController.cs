using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using webxemphim.Data;
using webxemphim.Models;
using webxemphim.Services;

namespace webxemphim.Controllers
{
    public class DownloadController : Controller
    {
        private readonly DownloadProtectionService _downloadService;
        private readonly CoinService _coinService;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DownloadController> _logger;

        public DownloadController(
            DownloadProtectionService downloadService,
            CoinService coinService,
            ApplicationDbContext db,
            ILogger<DownloadController> logger)
        {
            _downloadService = downloadService;
            _coinService = coinService;
            _db = db;
            _logger = logger;
        }

        [Authorize]
        [HttpGet("download/{movieId}")]
        public async Task<IActionResult> Download(int movieId, string? sourceUrl = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Kiểm tra quyền download
            var canDownload = await _downloadService.CanUserDownloadAsync(userId, movieId);
            if (!canDownload)
            {
                TempData["Error"] = "Bạn không có quyền download phim này hoặc không đủ coin.";
                return RedirectToAction("Watch", "Movies", new { id = movieId });
            }

            if (string.IsNullOrEmpty(sourceUrl))
            {
                TempData["Error"] = "Không tìm thấy nguồn video để download.";
                return RedirectToAction("Watch", "Movies", new { id = movieId });
            }

            // Tạo token và redirect đến download endpoint với token
            try
            {
                var token = await _downloadService.GenerateDownloadTokenAsync(userId, movieId, sourceUrl);
                return RedirectToAction("GetFile", new { token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating download token");
                TempData["Error"] = "Lỗi khi tạo link download. Vui lòng thử lại.";
                return RedirectToAction("Watch", "Movies", new { id = movieId });
            }
        }

        [HttpGet("getfile/{token}")]
        public async Task<IActionResult> GetFile(string token)
        {
            var tokenInfo = await _downloadService.ValidateDownloadTokenAsync(token);
            if (tokenInfo == null)
            {
                return BadRequest("Token không hợp lệ hoặc đã hết hạn.");
            }

            // Kiểm tra movie có yêu cầu coin không và trừ coin nếu cần
            var movie = await _db.Movies.FindAsync(tokenInfo.MovieId);
            if (movie != null && movie.RequiresCoins && movie.CoinCost > 0)
            {
                // Trừ coin khi download thực sự
                try
                {
                    await _coinService.SpendCoinsAsync(
                        tokenInfo.UserId,
                        movie.CoinCost,
                        TransactionType.SpendUnlockMovie,
                        $"Download phim '{movie.Title}'",
                        tokenInfo.MovieId
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deducting coins for download");
                    return BadRequest("Không thể trừ coin. Vui lòng kiểm tra số dư.");
                }
            }

            // Consume token
            await _downloadService.ConsumeDownloadTokenAsync(token);

            try
            {
                // Download file từ URL
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(10); // Timeout 10 phút cho file lớn
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                var response = await httpClient.GetAsync(tokenInfo.MovieSourceUrl, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest("Không thể tải file từ nguồn.");
                }

                var contentType = response.Content.Headers.ContentType?.MediaType ?? "video/mp4";
                var fileName = $"{movie?.Title ?? "movie"}_{tokenInfo.MovieId}_{DateTime.UtcNow:yyyyMMddHHmmss}.mp4";
                fileName = System.Text.RegularExpressions.Regex.Replace(fileName, @"[^\w\s-]", ""); // Remove special characters

                // Stream file về client
                var stream = await response.Content.ReadAsStreamAsync();
                return File(stream, contentType, fileName, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file");
                return StatusCode(500, "Lỗi khi tải file.");
            }
        }
    }
}

