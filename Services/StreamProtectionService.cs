using System.Security.Cryptography;
using System.Text;

namespace webxemphim.Services
{
    /// <summary>
    /// Service để bảo vệ video streaming URLs khỏi truy cập trực tiếp
    /// </summary>
    public class StreamProtectionService
    {
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly TimeSpan _tokenExpiration;

        public StreamProtectionService(IConfiguration configuration)
        {
            _configuration = configuration;
            _secretKey = configuration["StreamProtection:SecretKey"] ?? "your-stream-protection-secret-key-change-this";
            _tokenExpiration = TimeSpan.FromMinutes(int.TryParse(configuration["StreamProtection:TokenExpirationMinutes"], out var exp) ? exp : 30);
        }

        /// <summary>
        /// Tạo token bảo vệ cho video URL
        /// </summary>
        public string GenerateToken(string videoUrl, string? userId = null, DateTime? expiresAt = null)
        {
            var expires = expiresAt ?? DateTime.UtcNow.Add(_tokenExpiration);
            var timestamp = ((DateTimeOffset)expires).ToUnixTimeSeconds();
            
            var payload = $"{videoUrl}|{userId ?? "anonymous"}|{timestamp}";
            var hash = ComputeHash(payload);
            
            // Encode token: base64(url|user|timestamp|hash)
            var tokenData = $"{videoUrl}|{userId ?? "anonymous"}|{timestamp}|{hash}";
            var tokenBytes = Encoding.UTF8.GetBytes(tokenData);
            return Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        /// <summary>
        /// Xác thực token và lấy video URL
        /// </summary>
        public (bool IsValid, string? VideoUrl, string? Error) ValidateToken(string token, string? userId = null)
        {
            try
            {
                // Decode token
                var tokenData = token.Replace("-", "+").Replace("_", "/");
                // Padding
                switch (tokenData.Length % 4)
                {
                    case 2: tokenData += "=="; break;
                    case 3: tokenData += "="; break;
                }

                var tokenBytes = Convert.FromBase64String(tokenData);
                var decoded = Encoding.UTF8.GetString(tokenBytes);
                var parts = decoded.Split('|');

                if (parts.Length != 4)
                {
                    return (false, null, "Token format invalid");
                }

                var videoUrl = parts[0];
                var tokenUserId = parts[1];
                var timestamp = long.Parse(parts[2]);
                var hash = parts[3];

                // Kiểm tra expiration
                var expires = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                if (expires < DateTime.UtcNow)
                {
                    return (false, null, "Token expired");
                }

                // Kiểm tra hash
                var expectedHash = ComputeHash($"{videoUrl}|{tokenUserId}|{timestamp}");
                if (hash != expectedHash)
                {
                    return (false, null, "Token signature invalid");
                }

                // Kiểm tra user (nếu có)
                if (!string.IsNullOrEmpty(userId) && tokenUserId != "anonymous" && tokenUserId != userId)
                {
                    return (false, null, "Token user mismatch");
                }

                return (true, videoUrl, null);
            }
            catch (Exception ex)
            {
                return (false, null, $"Token validation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Tạo protected URL với token
        /// </summary>
        public string CreateProtectedUrl(string baseUrl, string videoUrl, string? userId = null)
        {
            var token = GenerateToken(videoUrl, userId);
            return $"{baseUrl}/api/stream/proxy?token={Uri.EscapeDataString(token)}";
        }

        private string ComputeHash(string input)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashBytes);
        }
    }
}

