using webxemphim.Services;

namespace webxemphim.Middleware
{
    public class ApiKeyAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;
        private const string ApiKeyHeaderName = "X-API-Key";

        public ApiKeyAuthenticationMiddleware(RequestDelegate next, ILogger<ApiKeyAuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ApiKeyService apiKeyService)
        {
            // Only apply to /api/public/* routes
            if (context.Request.Path.StartsWithSegments("/api/public"))
            {
                var apiKey = context.Request.Headers[ApiKeyHeaderName].FirstOrDefault() 
                    ?? context.Request.Query["apiKey"].FirstOrDefault();

                if (string.IsNullOrEmpty(apiKey))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { error = "API key is required" });
                    return;
                }

                var ipAddress = context.Connection.RemoteIpAddress?.ToString();
                var endpoint = context.Request.Path;

                var isValid = await apiKeyService.ValidateApiKeyAsync(apiKey, ipAddress, endpoint);
                if (!isValid)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired API key" });
                    return;
                }

                // Store API key in context for use in controllers
                context.Items["ApiKey"] = apiKey;
            }

            await _next(context);
        }
    }
}

