using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using webxemphim.Models.DTOs;

namespace webxemphim.Services
{
    public class AzureSearchService
    {
        private readonly SearchClient? _client;
        private readonly string? _semanticConfig;

        public bool IsConfigured => _client != null;

        public AzureSearchService(IConfiguration configuration)
        {
            var endpoint = configuration["AzureSearch:Endpoint"];
            var apiKey = configuration["AzureSearch:ApiKey"];
            var indexName = configuration["AzureSearch:IndexName"] ?? "movies";
            _semanticConfig = configuration["AzureSearch:SemanticConfig"] ?? "default";
            if (!string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(apiKey))
            {
                _client = new SearchClient(new Uri(endpoint), indexName, new AzureKeyCredential(apiKey));
            }
        }

        public async Task<PagedResultDto<MovieListItemDto>> SemanticSearchAsync(string query, int page = 1, int pageSize = 20)
        {
            if (_client == null) throw new InvalidOperationException("AzureSearch not configured");
            var opts = new SearchOptions
            {
                Size = pageSize,
                Skip = (page - 1) * pageSize
            };

            var resp = await _client.SearchAsync<SearchDocument>(query, opts);
            var results = new List<MovieListItemDto>();
            long total = resp.Value?.TotalCount ?? 0;
            if (resp.Value != null)
            {
                await foreach (var r in resp.Value.GetResultsAsync())
                {
                    var d = r.Document;
                    results.Add(new MovieListItemDto
                    {
                        Id = d.TryGetValue("id", out object? idObj) && int.TryParse(idObj?.ToString(), out var idVal) ? idVal : 0,
                        Title = d.TryGetValue("title", out var tObj) ? (tObj?.ToString() ?? string.Empty) : string.Empty,
                        Slug = d.TryGetValue("slug", out var sObj) ? sObj?.ToString() : null,
                        PosterUrl = d.TryGetValue("posterUrl", out var pObj) ? pObj?.ToString() : null,
                        Imdb = d.TryGetValue("imdb", out var imdbObj) && double.TryParse(imdbObj?.ToString(), out var imdbVal) ? imdbVal : null,
                        Year = d.TryGetValue("year", out var yObj) && int.TryParse(yObj?.ToString(), out var yVal) ? yVal : null,
                        AgeRating = d.TryGetValue("ageRating", out var aObj) ? aObj?.ToString() : null,
                        IsSeries = d.TryGetValue("isSeries", out var isObj) && bool.TryParse(isObj?.ToString(), out var isVal) && isVal,
                        AverageRating = d.TryGetValue("averageRating", out var arObj) && double.TryParse(arObj?.ToString(), out var arVal) ? arVal : null,
                    });
                }
            }
            return new PagedResultDto<MovieListItemDto>
            {
                Data = results,
                Page = page,
                PageSize = pageSize,
                TotalCount = (int)(total == 0 ? results.Count : total)
            };
        }
    }
}


