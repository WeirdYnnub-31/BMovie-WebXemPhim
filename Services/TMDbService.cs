using Microsoft.Extensions.Options;
using System.Text.Json;
using webxemphim.Models;
using webxemphim.Models.TMDb;

namespace webxemphim.Services
{
    public class TMDbService
    {
        private readonly IHttpClientFactory _factory;
        private readonly TMDbOptions _options;

        public TMDbService(IHttpClientFactory factory, IOptions<TMDbOptions> options)
        {
            _factory = factory;
            _options = options.Value;
        }

        public async Task<string> GetPopularMoviesAsync(int page = 1, string language = "vi-VN")
        {
            // Validate Access Token since we use Bearer auth
            if (string.IsNullOrWhiteSpace(_options.AccessToken) || _options.AccessToken == "YOUR_ACCESS_TOKEN")
            {
                throw new HttpRequestException("TMDb Access Token not configured");
            }

            var client = _factory.CreateClient("TMDbClient");
            var response = await client.GetAsync($"movie/popular?language={language}&page={page}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<List<TMDbMovieDto>> GetPopularMoviesParsedAsync(int page = 1, string language = "vi-VN")
        {
            var json = await GetPopularMoviesAsync(page, language);
            using var doc = JsonDocument.Parse(json);
            var results = doc.RootElement.GetProperty("results");
            var list = new List<TMDbMovieDto>(results.GetArrayLength());
            foreach (var el in results.EnumerateArray())
            {
                list.Add(new TMDbMovieDto
                {
                    Id = el.GetProperty("id").GetInt32(),
                    Title = el.TryGetProperty("title", out var t) ? t.GetString() : null,
                    Name = el.TryGetProperty("name", out var n) ? n.GetString() : null,
                    PosterPath = el.TryGetProperty("poster_path", out var p) ? p.GetString() : null,
                    VoteAverage = el.TryGetProperty("vote_average", out var v) ? v.GetDouble() : null,
                    Adult = el.TryGetProperty("adult", out var a) && a.GetBoolean(),
                    Overview = el.TryGetProperty("overview", out var o) ? o.GetString() : null,
                    ReleaseDate = el.TryGetProperty("release_date", out var rd) ? rd.GetString() : null,
                });
            }
            return list;
        }

        public async Task<List<TMDbMovieDto>> SearchMoviesParsedAsync(string query, string language = "vi-VN", int page = 1)
        {
            var client = _factory.CreateClient("TMDbClient");
            var response = await client.GetAsync($"search/movie?query={Uri.EscapeDataString(query)}&language={language}&page={page}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var results = doc.RootElement.GetProperty("results");
            var list = new List<TMDbMovieDto>(results.GetArrayLength());
            foreach (var el in results.EnumerateArray())
            {
                list.Add(new TMDbMovieDto
                {
                    Id = el.GetProperty("id").GetInt32(),
                    Title = el.TryGetProperty("title", out var t) ? t.GetString() : null,
                    PosterPath = el.TryGetProperty("poster_path", out var p) ? p.GetString() : null,
                    VoteAverage = el.TryGetProperty("vote_average", out var v) ? v.GetDouble() : null,
                    ReleaseDate = el.TryGetProperty("release_date", out var rd) ? rd.GetString() : null,
                });
            }
            return list;
        }

        public async Task<string> GetTrendingMoviesAsync(string timeWindow = "week", int page = 1)
        {
            // Validate Access Token
            if (string.IsNullOrWhiteSpace(_options.AccessToken) || _options.AccessToken == "YOUR_ACCESS_TOKEN")
            {
                throw new HttpRequestException("TMDb Access Token not configured");
            }

            var client = _factory.CreateClient("TMDbClient");
            var response = await client.GetAsync($"trending/movie/{timeWindow}?page={page}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<List<TMDbMovieDto>> GetTrendingMoviesParsedAsync(string timeWindow = "week", int page = 1)
        {
            var json = await GetTrendingMoviesAsync(timeWindow, page);
            using var doc = JsonDocument.Parse(json);
            var results = doc.RootElement.GetProperty("results");
            var list = new List<TMDbMovieDto>(results.GetArrayLength());
            foreach (var el in results.EnumerateArray())
            {
                list.Add(new TMDbMovieDto
                {
                    Id = el.GetProperty("id").GetInt32(),
                    Title = el.TryGetProperty("title", out var t) ? t.GetString() : null,
                    Name = el.TryGetProperty("name", out var n) ? n.GetString() : null,
                    PosterPath = el.TryGetProperty("poster_path", out var p) ? p.GetString() : null,
                    VoteAverage = el.TryGetProperty("vote_average", out var v) ? v.GetDouble() : null,
                    Overview = el.TryGetProperty("overview", out var o) ? o.GetString() : null,
                    ReleaseDate = el.TryGetProperty("release_date", out var rd) ? rd.GetString() : null,
                });
            }
            return list;
        }

        public string GetImageUrl(string posterPath, string size = "w500")
            => string.IsNullOrWhiteSpace(posterPath) ? string.Empty : $"{_options.ImageBaseUrl}/{size}{posterPath}";

        public async Task<TMDbMovieDto?> GetMovieDetailsByIdAsync(int tmdbId, string language = "vi-VN", bool includeCredits = true)
        {
            var client = _factory.CreateClient("TMDbClient");
            var response = await client.GetAsync($"movie/{tmdbId}?language={language}");
            if (!response.IsSuccessStatusCode)
            {
                // Fallback to en-US if the localized language fails
                response = await client.GetAsync($"movie/{tmdbId}?language=en-US");
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
            }
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var dto = new TMDbMovieDto
            {
                Id = root.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : tmdbId,
                Title = root.TryGetProperty("title", out var t) ? t.GetString() : null,
                Name = root.TryGetProperty("name", out var n) ? n.GetString() : null,
                PosterPath = root.TryGetProperty("poster_path", out var p) ? p.GetString() : null,
                VoteAverage = root.TryGetProperty("vote_average", out var v) ? v.GetDouble() : null,
                Overview = root.TryGetProperty("overview", out var o) ? o.GetString() : null,
                ReleaseDate = root.TryGetProperty("release_date", out var rd) ? rd.GetString() : null,
                Runtime = root.TryGetProperty("runtime", out var rt) ? rt.GetInt32() : null,
            };

            // Parse genres
            if (root.TryGetProperty("genres", out var genresEl) && genresEl.ValueKind == JsonValueKind.Array)
            {
                dto.Genres = new List<webxemphim.Models.TMDb.TMDbGenreDto>();
                foreach (var g in genresEl.EnumerateArray())
                {
                    dto.Genres.Add(new webxemphim.Models.TMDb.TMDbGenreDto
                    {
                        Id = g.TryGetProperty("id", out var gid) ? gid.GetInt32() : 0,
                        Name = g.TryGetProperty("name", out var gn) ? gn.GetString() : null
                    });
                }
            }

            // Parse production countries
            if (root.TryGetProperty("production_countries", out var countriesEl) && countriesEl.ValueKind == JsonValueKind.Array)
            {
                dto.ProductionCountries = new List<webxemphim.Models.TMDb.TMDbCountryDto>();
                foreach (var c in countriesEl.EnumerateArray())
                {
                    dto.ProductionCountries.Add(new webxemphim.Models.TMDb.TMDbCountryDto
                    {
                        IsoCode = c.TryGetProperty("iso_3166_1", out var iso) ? iso.GetString() : null,
                        Name = c.TryGetProperty("name", out var cn) ? cn.GetString() : null
                    });
                }
            }

            // Get credits (cast and director) if requested
            if (includeCredits)
            {
                var credits = await GetMovieCreditsAsync(tmdbId, language);
                if (credits != null)
                {
                    // Get top 10 cast members
                    dto.Cast = credits.Cast?
                        .OrderBy(c => c.Order)
                        .Take(10)
                        .Select(c => c.Name ?? string.Empty)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .ToList() ?? new List<string>();

                    // Get director from crew
                    dto.Director = credits.Crew?
                        .FirstOrDefault(c => c.Job?.Equals("Director", StringComparison.OrdinalIgnoreCase) == true ||
                                            c.Department?.Equals("Directing", StringComparison.OrdinalIgnoreCase) == true)
                        ?.Name;
                }
            }

            return dto;
        }

        /// <summary>
        /// Get movie credits (cast and crew) from TMDb API
        /// </summary>
        public async Task<webxemphim.Models.TMDb.TMDbCreditsDto?> GetMovieCreditsAsync(int tmdbId, string language = "vi-VN")
        {
            try
            {
                var client = _factory.CreateClient("TMDbClient");
                var response = await client.GetAsync($"movie/{tmdbId}/credits?language={language}");
                if (!response.IsSuccessStatusCode)
                {
                    // Fallback to en-US
                    response = await client.GetAsync($"movie/{tmdbId}/credits?language=en-US");
                    if (!response.IsSuccessStatusCode)
                    {
                        return null;
                    }
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var credits = new webxemphim.Models.TMDb.TMDbCreditsDto();

                // Parse cast
                if (root.TryGetProperty("cast", out var castEl) && castEl.ValueKind == JsonValueKind.Array)
                {
                    credits.Cast = new List<webxemphim.Models.TMDb.TMDbCastDto>();
                    foreach (var c in castEl.EnumerateArray())
                    {
                        credits.Cast.Add(new webxemphim.Models.TMDb.TMDbCastDto
                        {
                            Id = c.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
                            Name = c.TryGetProperty("name", out var n) ? n.GetString() : null,
                            Character = c.TryGetProperty("character", out var ch) ? ch.GetString() : null,
                            Order = c.TryGetProperty("order", out var o) ? o.GetInt32() : int.MaxValue,
                            ProfilePath = c.TryGetProperty("profile_path", out var pp) ? pp.GetString() : null
                        });
                    }
                }

                // Parse crew
                if (root.TryGetProperty("crew", out var crewEl) && crewEl.ValueKind == JsonValueKind.Array)
                {
                    credits.Crew = new List<webxemphim.Models.TMDb.TMDbCrewDto>();
                    foreach (var c in crewEl.EnumerateArray())
                    {
                        credits.Crew.Add(new webxemphim.Models.TMDb.TMDbCrewDto
                        {
                            Id = c.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
                            Name = c.TryGetProperty("name", out var n) ? n.GetString() : null,
                            Job = c.TryGetProperty("job", out var j) ? j.GetString() : null,
                            Department = c.TryGetProperty("department", out var d) ? d.GetString() : null
                        });
                    }
                }

                return credits;
            }
            catch
            {
                return null;
            }
        }

        public async Task<string?> GetMovieYoutubeTrailerEmbedAsync(int tmdbId, string language = "vi-VN")
        {
            var client = _factory.CreateClient("TMDbClient");
            // Try localized first, then fallback
            HttpResponseMessage response = await client.GetAsync($"movie/{tmdbId}/videos?language={language}");
            if (!response.IsSuccessStatusCode)
            {
                response = await client.GetAsync($"movie/{tmdbId}/videos?language=en-US");
                if (!response.IsSuccessStatusCode) return null;
            }
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
                return null;

            string? key = null;
            foreach (var el in results.EnumerateArray())
            {
                var site = el.TryGetProperty("site", out var s) ? s.GetString() : null;
                var type = el.TryGetProperty("type", out var t) ? t.GetString() : null;
                var official = el.TryGetProperty("official", out var off) && off.GetBoolean();
                var k = el.TryGetProperty("key", out var kEl) ? kEl.GetString() : null;
                if (string.Equals(site, "YouTube", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(k) && (official || string.Equals(type, "Trailer", StringComparison.OrdinalIgnoreCase)))
                {
                    key = k;
                    if (official && string.Equals(type, "Trailer", StringComparison.OrdinalIgnoreCase)) break;
                }
            }
            return key != null ? $"https://www.youtube.com/embed/{key}" : null;
        }
    }
}


