using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using webxemphim.Services;

namespace webxemphim.Controllers.Api
{
    [ApiController]
    [Route("api/apikeys")]
    [Authorize]
    public class ApiKeyManagementController : ControllerBase
    {
        private readonly ApiKeyService _apiKeyService;
        private readonly ILogger<ApiKeyManagementController> _logger;

        public ApiKeyManagementController(ApiKeyService apiKeyService, ILogger<ApiKeyManagementController> logger)
        {
            _apiKeyService = apiKeyService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                var apiKey = await _apiKeyService.CreateApiKeyAsync(
                    request.Name,
                    userId,
                    request.ExpiresAt,
                    request.RateLimit ?? 1000,
                    request.AllowedIps,
                    request.AllowedEndpoints
                );

                return Ok(new
                {
                    id = apiKey.Id,
                    key = apiKey.Key, // Only return key on creation
                    name = apiKey.Name,
                    createdAt = apiKey.CreatedAt,
                    expiresAt = apiKey.ExpiresAt,
                    rateLimit = apiKey.RateLimit
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating API key");
                return StatusCode(500, new { error = "Failed to create API key" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetApiKeys()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var apiKeys = await _apiKeyService.GetUserApiKeysAsync(userId);
            return Ok(apiKeys.Select(k => new
            {
                id = k.Id,
                name = k.Name,
                isActive = k.IsActive,
                createdAt = k.CreatedAt,
                expiresAt = k.ExpiresAt,
                rateLimit = k.RateLimit,
                requestCount = k.RequestCount,
                lastUsedAt = k.LastUsedAt
                // Don't return the key value for security
            }));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteApiKey(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _apiKeyService.DeleteApiKeyAsync(id, userId);
            if (!success)
            {
                return NotFound();
            }

            return Ok(new { message = "API key deleted successfully" });
        }

        [HttpPost("{id}/revoke")]
        public async Task<IActionResult> RevokeApiKey(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _apiKeyService.RevokeApiKeyAsync(id, userId);
            if (!success)
            {
                return NotFound();
            }

            return Ok(new { message = "API key revoked successfully" });
        }
    }

    public class CreateApiKeyRequest
    {
        public string Name { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public int? RateLimit { get; set; }
        public string? AllowedIps { get; set; }
        public List<string>? AllowedEndpoints { get; set; }
    }
}

