using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using webxemphim.Data;
using webxemphim.Models;

namespace webxemphim.Services
{
    public class RecommendationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RecommendationService> _logger;
        private readonly AIRecommendationService? _aiRecommendationService;

        public RecommendationService(
            ApplicationDbContext db, 
            IMemoryCache cache, 
            ILogger<RecommendationService> logger,
            IServiceProvider serviceProvider)
        {
            _db = db;
            _cache = cache;
            _logger = logger;
            
            // Resolve AIRecommendationService optionally to avoid circular dependencies
            try
            {
                _aiRecommendationService = serviceProvider.CreateScope().ServiceProvider.GetService<AIRecommendationService>();
            }
            catch
            {
                _aiRecommendationService = null;
            }
        }

        public async Task<List<Movie>> GetRecommendationsAsync(string? userId, int limit = 10)
        {
            var cacheKey = $"recommendations_{userId ?? "anonymous"}_{limit}";
            
            if (_cache.TryGetValue(cacheKey, out List<Movie>? cachedRecommendations) && cachedRecommendations != null)
            {
                _logger.LogInformation("Returning cached recommendations for user: {UserId}", userId ?? "anonymous");
                return cachedRecommendations;
            }

            List<Movie> recommendations;

            if (string.IsNullOrEmpty(userId))
            {
                // Return popular movies for anonymous users
                recommendations = await _db.Movies
                    .AsNoTracking()
                    .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                    .OrderByDescending(m => m.ViewCount)
                    .ThenByDescending(m => m.CreatedAt)
                    .Take(limit)
                    .ToListAsync();
            }
            else
            {
                // Try AI-powered recommendations first if service is available
                if (_aiRecommendationService != null)
                {
                    try
                    {
                        // Check if user has enough data for AI recommendations
                        var userRatingCount = await _db.Ratings
                            .AsNoTracking()
                            .Where(r => r.UserId == userId)
                            .CountAsync();
                        
                        var userWatchedCount = await _db.UserInventoryItems
                            .AsNoTracking()
                            .Where(ui => ui.UserId == userId && ui.Type == webxemphim.Models.InventoryItemType.Watched)
                            .CountAsync();

                        // Use AI recommendations if user has at least 3 ratings or 5 watched movies
                        if (userRatingCount >= 3 || userWatchedCount >= 5)
                        {
                            _logger.LogInformation("Using AI recommendations for user: {UserId} (ratings: {RatingCount}, watched: {WatchedCount})", 
                                userId, userRatingCount, userWatchedCount);
                            
                            recommendations = await _aiRecommendationService.GetAIRecommendationsAsync(userId, limit);
                            
                            // If AI recommendations are sufficient, return them
                            if (recommendations.Count >= limit * 0.7) // At least 70% of requested
                            {
                                _cache.Set(cacheKey, recommendations, TimeSpan.FromMinutes(10));
                                _logger.LogInformation("Generated {Count} AI recommendations for user: {UserId}", recommendations.Count, userId);
                                return recommendations;
                            }
                            else
                            {
                                _logger.LogInformation("AI recommendations insufficient ({Count}/{Limit}), falling back to simple recommendations", 
                                    recommendations.Count, limit);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error using AI recommendations, falling back to simple recommendations for user: {UserId}", userId);
                    }
                }

                // Fallback to simple genre-based recommendations
                // Get user's favorite genres
                var userFavoriteGenres = await _db.UserInventoryItems
                    .AsNoTracking()
                    .Where(ui => ui.UserId == userId && ui.Type == webxemphim.Models.InventoryItemType.Favorite)
                    .Include(ui => ui.Movie)
                    .ThenInclude(m => m.MovieGenres)
                    .SelectMany(ui => ui.Movie.MovieGenres)
                    .Select(mg => mg.GenreId)
                    .Distinct()
                    .ToListAsync();

                // Get user's watched movies
                var watchedMovieIds = await _db.UserInventoryItems
                    .AsNoTracking()
                    .Where(ui => ui.UserId == userId && ui.Type == webxemphim.Models.InventoryItemType.Watched)
                    .Select(ui => ui.MovieId)
                    .ToListAsync();

                // Get recommendations based on favorite genres
                recommendations = await _db.Movies
                    .AsNoTracking()
                    .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                    .Where(m => !watchedMovieIds.Contains(m.Id))
                    .Where(m => m.MovieGenres.Any(mg => userFavoriteGenres.Contains(mg.GenreId)))
                    .OrderByDescending(m => m.ViewCount)
                    .ThenByDescending(m => m.AverageRating ?? 0)
                    .Take(limit)
                    .ToListAsync();

                // If not enough recommendations, fill with popular movies
                if (recommendations.Count < limit)
                {
                    var additionalMovies = await _db.Movies
                        .AsNoTracking()
                        .Include(m => m.MovieGenres)
                        .ThenInclude(mg => mg.Genre)
                        .Where(m => !watchedMovieIds.Contains(m.Id) && !recommendations.Select(r => r.Id).Contains(m.Id))
                        .OrderByDescending(m => m.ViewCount)
                        .ThenByDescending(m => m.AverageRating ?? 0)
                        .Take(limit - recommendations.Count)
                        .ToListAsync();

                    recommendations.AddRange(additionalMovies);
                }
            }

            // Cache for 10 minutes
            _cache.Set(cacheKey, recommendations, TimeSpan.FromMinutes(10));
            
            _logger.LogInformation("Generated {Count} recommendations for user: {UserId}", recommendations.Count, userId ?? "anonymous");
            return recommendations;
        }

        public async Task<List<Movie>> GetSimilarMoviesAsync(int movieId, int limit = 6)
        {
            var cacheKey = $"similar_movies_{movieId}_{limit}";
            
            if (_cache.TryGetValue(cacheKey, out List<Movie>? cachedMovies) && cachedMovies != null)
            {
                return cachedMovies;
            }

            // Try AI-powered similar movies first if service is available
            if (_aiRecommendationService != null)
            {
                try
                {
                    var aiSimilarMovies = await _aiRecommendationService.GetSimilarMoviesAIAsync(movieId, limit);
                    if (aiSimilarMovies.Any())
                    {
                        _cache.Set(cacheKey, aiSimilarMovies, TimeSpan.FromMinutes(15));
                        _logger.LogInformation("Generated {Count} AI similar movies for movie: {MovieId}", aiSimilarMovies.Count, movieId);
                        return aiSimilarMovies;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error using AI similar movies, falling back to simple similarity for movie: {MovieId}", movieId);
                }
            }

            // Fallback to simple genre-based similarity
            var movie = await _db.Movies
                .AsNoTracking()
                .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .FirstOrDefaultAsync(m => m.Id == movieId);

            if (movie == null) return new List<Movie>();

            var genreIds = movie.MovieGenres.Select(mg => mg.GenreId).ToList();

            var similarMovies = await _db.Movies
                .AsNoTracking()
                .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .Where(m => m.Id != movieId)
                .Where(m => m.MovieGenres.Any(mg => genreIds.Contains(mg.GenreId)))
                .OrderByDescending(m => m.ViewCount)
                .ThenByDescending(m => m.AverageRating ?? 0)
                .Take(limit)
                .ToListAsync();

            // Cache for 15 minutes
            _cache.Set(cacheKey, similarMovies, TimeSpan.FromMinutes(15));
            
            return similarMovies;
        }

        public async Task<List<Movie>> GetTrendingMoviesAsync(int limit = 10)
        {
            var cacheKey = $"trending_movies_{limit}";
            
            if (_cache.TryGetValue(cacheKey, out List<Movie>? cachedMovies) && cachedMovies != null)
            {
                return cachedMovies;
            }

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            
            var trendingMovies = await _db.Movies
                .AsNoTracking()
                .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .Where(m => m.CreatedAt >= thirtyDaysAgo)
                .OrderByDescending(m => m.ViewCount)
                .ThenByDescending(m => m.AverageRating ?? 0)
                .Take(limit)
                .ToListAsync();

            // Cache for 1 hour
            _cache.Set(cacheKey, trendingMovies, TimeSpan.FromHours(1));
            
            return trendingMovies;
        }

        public async Task<List<Movie>> GetNewReleasesAsync(int limit = 10)
        {
            var cacheKey = $"new_releases_{limit}";
            
            if (_cache.TryGetValue(cacheKey, out List<Movie>? cachedMovies) && cachedMovies != null)
            {
                return cachedMovies;
            }

            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
            
            var newReleases = await _db.Movies
                .AsNoTracking()
                .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .Where(m => m.CreatedAt >= sevenDaysAgo)
                .OrderByDescending(m => m.CreatedAt)
                .ThenByDescending(m => m.ViewCount)
                .Take(limit)
                .ToListAsync();

            // Cache for 30 minutes
            _cache.Set(cacheKey, newReleases, TimeSpan.FromMinutes(30));
            
            return newReleases;
        }
    }
}
