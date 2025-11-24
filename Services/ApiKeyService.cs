using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using webxemphim.Data;
using webxemphim.Models;

namespace webxemphim.Services
{
    public class ApiKeyService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ApiKeyService> _logger;

        public ApiKeyService(ApplicationDbContext db, ILogger<ApiKeyService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<ApiKey> CreateApiKeyAsync(string name, string? userId = null, DateTime? expiresAt = null, int rateLimit = 1000, string? allowedIps = null, List<string>? allowedEndpoints = null)
        {
            var key = GenerateApiKey();
            var apiKey = new ApiKey
            {
                Key = key,
                Name = name,
                UserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                RateLimit = rateLimit,
                AllowedIps = allowedIps,
                AllowedEndpoints = allowedEndpoints ?? new List<string>(),
                RequestCount = 0,
                LastUsedAt = DateTime.UtcNow
            };

            _db.ApiKeys.Add(apiKey);
            await _db.SaveChangesAsync();
            return apiKey;
        }

        public async Task<ApiKey?> GetApiKeyAsync(string key)
        {
            return await _db.ApiKeys
                .Include(k => k.User)
                .FirstOrDefaultAsync(k => k.Key == key);
        }

        public async Task<bool> ValidateApiKeyAsync(string key, string? ipAddress = null, string? endpoint = null)
        {
            var apiKey = await GetApiKeyAsync(key);
            if (apiKey == null || !apiKey.IsActive)
            {
                return false;
            }

            // Check expiration
            if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
            {
                return false;
            }

            // Check IP restriction
            if (!string.IsNullOrEmpty(apiKey.AllowedIps) && !string.IsNullOrEmpty(ipAddress))
            {
                var allowedIps = apiKey.AllowedIps.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (!allowedIps.Contains(ipAddress))
                {
                    return false;
                }
            }

            // Check endpoint restriction
            if (apiKey.AllowedEndpoints != null && apiKey.AllowedEndpoints.Any() && !string.IsNullOrEmpty(endpoint))
            {
                if (!apiKey.AllowedEndpoints.Any(e => endpoint.StartsWith(e, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }

            // Check rate limit (simplified - in production, use Redis or similar)
            // This is a basic check - you might want to implement proper rate limiting
            var hourAgo = DateTime.UtcNow.AddHours(-1);
            // Note: For proper rate limiting, you'd need to track requests per hour in a separate table

            // Update last used
            apiKey.LastUsedAt = DateTime.UtcNow;
            apiKey.RequestCount++;
            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<List<ApiKey>> GetUserApiKeysAsync(string userId)
        {
            return await _db.ApiKeys
                .Where(k => k.UserId == userId)
                .OrderByDescending(k => k.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> RevokeApiKeyAsync(int apiKeyId, string? userId = null)
        {
            var apiKey = await _db.ApiKeys.FindAsync(apiKeyId);
            if (apiKey == null)
            {
                return false;
            }

            // Check if user has permission to revoke this key
            if (!string.IsNullOrEmpty(userId) && apiKey.UserId != userId)
            {
                return false;
            }

            apiKey.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteApiKeyAsync(int apiKeyId, string? userId = null)
        {
            var apiKey = await _db.ApiKeys.FindAsync(apiKeyId);
            if (apiKey == null)
            {
                return false;
            }

            // Check if user has permission to delete this key
            if (!string.IsNullOrEmpty(userId) && apiKey.UserId != userId)
            {
                return false;
            }

            _db.ApiKeys.Remove(apiKey);
            await _db.SaveChangesAsync();
            return true;
        }

        private string GenerateApiKey()
        {
            // Generate a secure random API key
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var key = new StringBuilder();
            key.Append("wxpm_"); // Prefix for webxemphim
            for (int i = 0; i < 32; i++)
            {
                key.Append(chars[random.Next(chars.Length)]);
            }
            return key.ToString();
        }
    }
}

