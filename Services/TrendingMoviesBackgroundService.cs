using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using webxemphim.Models;
using webxemphim.Models.TMDb;
using webxemphim.Services;
using webxemphim.Data;
using Microsoft.EntityFrameworkCore;

namespace webxemphim.Services
{
    public class TrendingMoviesBackgroundService : BackgroundService
    {
        private readonly ILogger<TrendingMoviesBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        // Bỏ: private readonly TMDbService _tmdbService;

        public TrendingMoviesBackgroundService(ILogger<TrendingMoviesBackgroundService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ImportTrendingMovies();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi tự động import trending movies từ TMDb");
                }

                // Đợi 1 ngày
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }

        private async Task ImportTrendingMovies()
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var tmdbService = scope.ServiceProvider.GetRequiredService<TMDbService>();
            var trending = await tmdbService.GetTrendingMoviesParsedAsync("week", 1);
            int added = 0;
            
            foreach (var t in trending)
            {
                if (string.IsNullOrWhiteSpace(t.Title)) continue;
                
                // Check if exists by TMDbId first, then by title
                bool exists = await db.Movies.AnyAsync(x => x.Title == t.Title);
                if (exists) continue;
                
                // Get full details from TMDb for more complete data
                TMDbMovieDto? details = null;
                try
                {
                    details = await tmdbService.GetMovieDetailsByIdAsync(t.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Could not fetch details for movie {t.Title}: {ex.Message}");
                }
                
                // Cập nhật runtime và country từ details
                var duration = details?.Runtime.HasValue == true && details.Runtime.Value > 0 ? details.Runtime.Value : 120;
                var country = "US";
                if (details?.ProductionCountries != null && details.ProductionCountries.Any())
                {
                    var countries = details.ProductionCountries.Select(c => c.Name ?? c.IsoCode).Where(c => !string.IsNullOrWhiteSpace(c));
                    country = string.Join(", ", countries);
                }
                var castString = details?.Cast != null && details.Cast.Any() 
                    ? string.Join(", ", details.Cast.Take(10)) 
                    : null;

                var movie = new Movie
                {
                    TMDbId = t.Id,
                    Title = t.Title,
                    Slug = GenerateSlug(t.Title),
                    PosterUrl = tmdbService.GetImageUrl(t.PosterPath ?? ""),
                    Imdb = t.VoteAverage,
                    Year = !string.IsNullOrEmpty(t.ReleaseDate) && DateTime.TryParse(t.ReleaseDate, out var dy) ? (int?)dy.Year : null,
                    Description = details?.Overview ?? t.Overview,
                    ReleaseDate = !string.IsNullOrEmpty(t.ReleaseDate) && DateTime.TryParse(t.ReleaseDate, out var d2) ? d2 : null,
                    AgeRating = "PG-13",
                    DurationMinutes = duration,
                    Country = country,
                    Cast = castString,
                    Director = details?.Director,
                    CreatedAt = DateTime.UtcNow
                };
                
                // Add genres if available (tự động tạo genre nếu chưa có)
                if (details?.Genres != null && details.Genres.Any())
                {
                    foreach (var gn in details.Genres)
                    {
                        if (string.IsNullOrWhiteSpace(gn.Name)) continue;

                        var dbg = await db.Genres.FirstOrDefaultAsync(x => x.Name == gn.Name);
                        if (dbg == null)
                        {
                            // Tự động tạo genre mới nếu chưa có
                            dbg = new Genre
                            {
                                Name = gn.Name,
                                Slug = gn.Name.ToLower()
                                    .Replace(" ", "-")
                                    .Replace("'", "")
                                    .Replace(",", "")
                            };
                            db.Genres.Add(dbg);
                            await db.SaveChangesAsync(); // Save to get ID
                        }
                        movie.MovieGenres.Add(new MovieGenre { Genre = dbg });
                    }
                }

                db.Movies.Add(movie);
                await db.SaveChangesAsync(); // Save to get the ID
                
                // Add a default movie source so it can be watched
                var movieSource = new Models.MovieSource
                {
                    MovieId = movie.Id,
                    ServerName = "YouTube",
                    Quality = "720p",
                    Language = "Vietsub",
                    Url = "https://www.youtube.com/embed/dQw4w9WgXcQ", // Placeholder YouTube embed
                    IsDefault = true
                };
                db.MovieSources.Add(movieSource);
                await db.SaveChangesAsync();
                
                added++;
            }
            
            _logger.LogInformation($"Đã tự động import {added} trending movies từ TMDb vào DB.");
        }
        
        private string GenerateSlug(string title)
        {
            if (string.IsNullOrEmpty(title)) return "movie";
            return title.ToLower()
                .Replace(" ", "-")
                .Replace(":", "")
                .Replace("'", "")
                .Replace("\"", "")
                .Replace("?", "")
                .Replace("!", "")
                .Replace(",", "")
                .Replace(".", "");
        }
    }
}
