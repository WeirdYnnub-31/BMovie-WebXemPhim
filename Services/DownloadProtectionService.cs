using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;
using webxemphim.Data;
using webxemphim.Models;

namespace webxemphim.Services
{
    public class DownloadProtectionService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DownloadProtectionService> _logger;
        private const int TokenExpiryMinutes = 30; // Token hết hạn sau 30 phút
        private const int MaxDownloadsPerToken = 1; // Mỗi token chỉ cho phép download 1 lần

        public DownloadProtectionService(
            ApplicationDbContext db,
            IMemoryCache cache,
            ILogger<DownloadProtectionService> logger)
        {
            _db = db;
            _cache = cache;
            _logger = logger;
        }

        public async Task<string> GenerateDownloadTokenAsync(string userId, int movieId, string movieSourceUrl)
        {
            // Kiểm tra xem phim có cho phép download không
            var movie = await _db.Movies.FindAsync(movieId);
            if (movie == null || !movie.IsDownloadable)
            {
                throw new UnauthorizedAccessException("Phim này không cho phép download.");
            }

            // Kiểm tra user có quyền download không (có thể kiểm tra coin, subscription, etc.)
            // Ví dụ: kiểm tra coin nếu phim yêu cầu coin
            // Note: Không trừ coin ở đây, chỉ kiểm tra. Coin sẽ được trừ khi download thực sự
            if (movie.RequiresCoins && movie.CoinCost > 0)
            {
                var wallet = await _db.CoinWallets.FirstOrDefaultAsync(w => w.UserId == userId);
                if (wallet == null || wallet.Balance < movie.CoinCost)
                {
                    throw new UnauthorizedAccessException("Bạn không đủ coin để download phim này.");
                }
            }

            // Tạo token
            var tokenData = $"{userId}:{movieId}:{movieSourceUrl}:{DateTime.UtcNow:yyyyMMddHHmmss}";
            var token = GenerateSecureToken(tokenData);

            // Lưu token vào cache với thời gian hết hạn
            var cacheKey = $"download_token_{token}";
            var tokenInfo = new DownloadTokenInfo
            {
                UserId = userId,
                MovieId = movieId,
                MovieSourceUrl = movieSourceUrl,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(TokenExpiryMinutes),
                DownloadCount = 0,
                MaxDownloads = MaxDownloadsPerToken
            };

            _cache.Set(cacheKey, tokenInfo, TimeSpan.FromMinutes(TokenExpiryMinutes + 5)); // Cache thêm 5 phút buffer

            _logger.LogInformation("Download token generated for user {UserId}, movie {MovieId}", userId, movieId);
            return token;
        }

        public async Task<DownloadTokenInfo?> ValidateDownloadTokenAsync(string token)
        {
            var cacheKey = $"download_token_{token}";
            if (!_cache.TryGetValue(cacheKey, out DownloadTokenInfo? tokenInfo) || tokenInfo == null)
            {
                _logger.LogWarning("Invalid or expired download token: {Token}", token);
                return null;
            }

            // Kiểm tra token đã hết hạn chưa
            if (tokenInfo.ExpiresAt < DateTime.UtcNow)
            {
                _cache.Remove(cacheKey);
                _logger.LogWarning("Expired download token: {Token}", token);
                return null;
            }

            // Kiểm tra số lần download
            if (tokenInfo.DownloadCount >= tokenInfo.MaxDownloads)
            {
                _cache.Remove(cacheKey);
                _logger.LogWarning("Download token exceeded max downloads: {Token}", token);
                return null;
            }

            // Tăng download count
            tokenInfo.DownloadCount++;
            _cache.Set(cacheKey, tokenInfo, TimeSpan.FromMinutes(TokenExpiryMinutes + 5));

            return tokenInfo;
        }

        public async Task<bool> ConsumeDownloadTokenAsync(string token)
        {
            var tokenInfo = await ValidateDownloadTokenAsync(token);
            if (tokenInfo == null)
            {
                return false;
            }

            // Token đã được validate và download count đã tăng
            // Có thể thêm logic để track download history nếu cần
            return true;
        }

        private string GenerateSecureToken(string data)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("your-secret-key-change-in-production-min-32-chars"));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        public async Task<bool> CanUserDownloadAsync(string userId, int movieId)
        {
            var movie = await _db.Movies.FindAsync(movieId);
            if (movie == null || !movie.IsDownloadable)
            {
                return false;
            }

            // Kiểm tra coin nếu cần
            if (movie.RequiresCoins && movie.CoinCost > 0)
            {
                var wallet = await _db.CoinWallets.FirstOrDefaultAsync(w => w.UserId == userId);
                if (wallet == null || wallet.Balance < movie.CoinCost)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public class DownloadTokenInfo
    {
        public string UserId { get; set; } = string.Empty;
        public int MovieId { get; set; }
        public string MovieSourceUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int DownloadCount { get; set; }
        public int MaxDownloads { get; set; }
    }
}

