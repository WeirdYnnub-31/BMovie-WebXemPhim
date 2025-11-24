using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace webxemphim.Services
{
    public class RophimScraperService
    {
        private readonly HttpClient _httpClient;

        public RophimScraperService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("RophimScraper");
        }

        public class RophimMovie
        {
            public string Title { get; set; } = string.Empty;
            public string Slug { get; set; } = string.Empty;
            public string? PosterUrl { get; set; }
            public string? Description { get; set; }
            public int? Year { get; set; }
            public List<RophimSource> Sources { get; set; } = new();
        }

        public class RophimSource
        {
            public string ServerName { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
            public bool IsDefault { get; set; }
        }

        public async Task<RophimMovie?> ScrapeMovie(string slug)
        {
            try
            {
                var url = $"phim-bo/{slug}";
                var html = await _httpClient.GetStringAsync(url);
                
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var movie = new RophimMovie { Slug = slug };

                // Extract title
                var titleNode = doc.DocumentNode.SelectSingleNode("//h1[@class='entry-title']") 
                    ?? doc.DocumentNode.SelectSingleNode("//h1");
                if (titleNode != null)
                {
                    movie.Title = titleNode.InnerText.Trim();
                }

                // Extract poster
                var posterNode = doc.DocumentNode.SelectSingleNode("//img[@class='lazy poster']")
                    ?? doc.DocumentNode.SelectSingleNode("//img[@class='poster']")
                    ?? doc.DocumentNode.SelectSingleNode("//div[@class='poster']//img");
                if (posterNode != null)
                {
                    movie.PosterUrl = posterNode.GetAttributeValue("data-original", null) 
                        ?? posterNode.GetAttributeValue("src", null);
                }

                // Extract description
                var descNode = doc.DocumentNode.SelectSingleNode("//div[@class='entry-content']")
                    ?? doc.DocumentNode.SelectSingleNode("//div[@class='description']");
                if (descNode != null)
                {
                    movie.Description = descNode.InnerText.Trim();
                }

                // Extract year
                var yearMatch = Regex.Match(html, @"(\d{4})\s*(?:Trọn bộ|Năm phát hành)", RegexOptions.IgnoreCase);
                if (yearMatch.Success)
                {
                    movie.Year = int.Parse(yearMatch.Groups[1].Value);
                }

                // Extract video sources
                var sourceNodes = doc.DocumentNode.SelectNodes("//iframe[@class='episode-embed']")
                    ?? doc.DocumentNode.SelectNodes("//iframe[@data-src]")
                    ?? doc.DocumentNode.SelectNodes("//div[@id='player-content']//iframe");
                
                if (sourceNodes != null)
                {
                    int serverIndex = 1;
                    foreach (var iframe in sourceNodes)
                    {
                        var src = iframe.GetAttributeValue("data-src", null) 
                            ?? iframe.GetAttributeValue("src", null);
                        if (!string.IsNullOrWhiteSpace(src))
                        {
                            movie.Sources.Add(new RophimSource
                            {
                                ServerName = $"Server {serverIndex++}",
                                Url = src,
                                IsDefault = serverIndex == 2 // First server is default
                            });
                        }
                    }
                }

                return movie.Title != null ? movie : null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<RophimListItem>> ListMovies(int page = 1)
        {
            var movies = new List<RophimListItem>();
            try
            {
                var url = $"phim-bo/page/{page}/";
                var html = await _httpClient.GetStringAsync(url);
                
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var movieNodes = doc.DocumentNode.SelectNodes("//div[@class='item']")
                    ?? doc.DocumentNode.SelectNodes("//article[@class='item']");
                
                if (movieNodes != null)
                {
                    foreach (var node in movieNodes)
                    {
                        var a = node.SelectSingleNode(".//a[@class='mask-link']")
                            ?? node.SelectSingleNode(".//h2/a")
                            ?? node.SelectSingleNode(".//a");
                        
                        if (a != null)
                        {
                            var href = a.GetAttributeValue("href", null);
                            if (!string.IsNullOrWhiteSpace(href))
                            {
                                var slug = href.Split('/').LastOrDefault()?.Split('.').FirstOrDefault();
                                var title = a.InnerText.Trim();

                                var img = node.SelectSingleNode(".//img[@class='lazy']") 
                                    ?? node.SelectSingleNode(".//img");
                                var poster = img?.GetAttributeValue("data-original", null) 
                                    ?? img?.GetAttributeValue("src", null);

                                if (!string.IsNullOrWhiteSpace(slug) && !string.IsNullOrWhiteSpace(title))
                                {
                                    movies.Add(new RophimListItem
                                    {
                                        Title = title,
                                        Slug = slug,
                                        PosterUrl = poster
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return movies;
        }

        public class RophimListItem
        {
            public string Title { get; set; } = string.Empty;
            public string Slug { get; set; } = string.Empty;
            public string? PosterUrl { get; set; }
        }
    }
}

