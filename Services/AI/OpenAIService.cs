using System.Net.Http.Json;

namespace webxemphim.Services.AI
{
    public class OpenAIOptions
    {
        public string? Endpoint { get; set; }
        public string? ApiKey { get; set; }
        public string ChatDeployment { get; set; } = "gpt-4o-mini";
        public string EmbeddingDeployment { get; set; } = "text-embedding-3-small";
        public bool IsConfigured => !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(ApiKey);
    }

    public class OpenAIService
    {
        private readonly HttpClient _http;
        private readonly OpenAIOptions _opt;

        public bool IsConfigured => _opt.IsConfigured;

        public OpenAIService(IConfiguration cfg, IHttpClientFactory factory)
        {
            _opt = cfg.GetSection("AzureOpenAI").Get<OpenAIOptions>() ?? new OpenAIOptions();
            _http = factory.CreateClient("AzureOpenAI");
        }

        public async Task<string> ChatAsync(IEnumerable<(string role,string content)> messages, CancellationToken ct)
        {
            if (!IsConfigured) return "AI not configured.";
            var body = new
            {
                messages = messages.Select(m => new { role = m.role, content = m.content }).ToArray(),
                temperature = 0.6,
                top_p = 0.9
            };
            using var req = new HttpRequestMessage(HttpMethod.Post, $"openai/deployments/{_opt.ChatDeployment}/chat/completions?api-version=2024-02-15-preview");
            req.Content = JsonContent.Create(body);
            using var res = await _http.SendAsync(req, ct);
            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadFromJsonAsync<dynamic>(cancellationToken: ct);
            try { return json?.choices?[0]?.message?.content?.ToString() ?? string.Empty; } catch { return string.Empty; }
        }

        public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
        {
            if (!IsConfigured) return Array.Empty<float>();
            var body = new { input = text };            
            using var req = new HttpRequestMessage(HttpMethod.Post, $"openai/deployments/{_opt.EmbeddingDeployment}/embeddings?api-version=2023-05-15");
            req.Content = JsonContent.Create(body);
            using var res = await _http.SendAsync(req, ct);
            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadFromJsonAsync<dynamic>(cancellationToken: ct);
            try { return ((IEnumerable<object>)json!.data![0]!.embedding!).Select(v => Convert.ToSingle(v)).ToArray(); } catch { return Array.Empty<float>(); }
        }
    }
}


