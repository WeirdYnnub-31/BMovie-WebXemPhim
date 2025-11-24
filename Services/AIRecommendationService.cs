using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using webxemphim.Data;
using webxemphim.Models;

namespace webxemphim.Services
{
    /// <summary>
    /// Advanced AI-powered recommendation service using Collaborative Filtering and Content-Based Filtering
    /// </summary>
    public class AIRecommendationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AIRecommendationService> _logger;

        // Weights for hybrid approach
        private const double CollaborativeFilteringWeight = 0.6;
        private const double ContentBasedWeight = 0.4;

        public AIRecommendationService(
            ApplicationDbContext db,
            IMemoryCache cache,
            ILogger<AIRecommendationService> logger)
        {
            _db = db;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Get AI-powered recommendations for a user using hybrid approach
        /// </summary>
        public async Task<List<Movie>> GetAIRecommendationsAsync(string userId, int limit = 10)
        {
            var cacheKey = $"ai_recommendations_{userId}_{limit}";
            
            if (_cache.TryGetValue(cacheKey, out List<Movie>? cachedRecommendations) && cachedRecommendations != null)
            {
                _logger.LogInformation("Returning cached AI recommendations for user: {UserId}", userId);
                return cachedRecommendations;
            }

            try
            {
                // Get user's ratings and viewing history
                var userRatings = await _db.Ratings
                    .AsNoTracking()
                    .Where(r => r.UserId == userId)
                    .ToListAsync();

                var watchedMovieIds = await _db.UserInventoryItems
                    .AsNoTracking()
                    .Where(ui => ui.UserId == userId && ui.Type == InventoryItemType.Watched)
                    .Select(ui => ui.MovieId)
                    .ToListAsync();

                // If user has no ratings or viewing history, fall back to popular movies
                if (!userRatings.Any() && !watchedMovieIds.Any())
                {
                    _logger.LogInformation("User {UserId} has no ratings or viewing history, returning popular movies", userId);
                    return await GetPopularMoviesAsync(limit);
                }

                // Get all movies (excluding watched ones)
                var allMovies = await _db.Movies
                    .AsNoTracking()
                    .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                    .Where(m => !watchedMovieIds.Contains(m.Id))
                    .ToListAsync();

                if (!allMovies.Any())
                {
                    return new List<Movie>();
                }

                // Calculate recommendation scores using hybrid approach
                var movieScores = new Dictionary<int, double>();

                // 1. Collaborative Filtering (User-based)
                var collaborativeScores = await GetCollaborativeFilteringScoresAsync(userId, userRatings, allMovies);
                
                // 2. Content-Based Filtering
                var contentBasedScores = await GetContentBasedScoresAsync(userId, userRatings, watchedMovieIds, allMovies);

                // 3. Hybrid: Combine both approaches
                foreach (var movie in allMovies)
                {
                    var collaborativeScore = collaborativeScores.GetValueOrDefault(movie.Id, 0.0);
                    var contentBasedScore = contentBasedScores.GetValueOrDefault(movie.Id, 0.0);
                    
                    // Normalize scores to 0-1 range
                    var normalizedCollaborative = collaborativeScore;
                    var normalizedContentBased = contentBasedScore;

                    // Weighted combination
                    var hybridScore = (CollaborativeFilteringWeight * normalizedCollaborative) + 
                                     (ContentBasedWeight * normalizedContentBased);

                    // Boost score for popular movies (to ensure diversity)
                    var popularityBoost = Math.Min(1.0, Math.Log10(movie.ViewCount + 1) / 10.0) * 0.1;
                    hybridScore += popularityBoost;

                    movieScores[movie.Id] = hybridScore;
                }

                // Sort by score and take top N
                var recommendations = movieScores
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(limit)
                    .Select(kvp => allMovies.First(m => m.Id == kvp.Key))
                    .ToList();

                // Cache for 30 minutes
                _cache.Set(cacheKey, recommendations, TimeSpan.FromMinutes(30));
                
                _logger.LogInformation("Generated {Count} AI recommendations for user: {UserId}", recommendations.Count, userId);
                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI recommendations for user: {UserId}", userId);
                // Fall back to popular movies on error
                return await GetPopularMoviesAsync(limit);
            }
        }

        /// <summary>
        /// User-based Collaborative Filtering: Find similar users and recommend movies they liked
        /// </summary>
        private async Task<Dictionary<int, double>> GetCollaborativeFilteringScoresAsync(
            string userId,
            List<Rating> userRatings,
            List<Movie> candidateMovies)
        {
            var scores = new Dictionary<int, double>();

            if (!userRatings.Any())
            {
                return scores;
            }

            try
            {
                // Get all users who rated movies
                var allUserRatings = await _db.Ratings
                    .AsNoTracking()
                    .Where(r => r.UserId != userId)
                    .ToListAsync();

                // Build user-item matrix
                var userItemMatrix = allUserRatings
                    .GroupBy(r => r.UserId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.ToDictionary(r => r.MovieId, r => r.Score)
                    );

                // Build target user's ratings
                var targetUserRatings = userRatings.ToDictionary(r => r.MovieId, r => r.Score);

                // Find similar users using cosine similarity
                var similarUsers = new Dictionary<string, double>();
                
                foreach (var (otherUserId, otherUserRatings) in userItemMatrix)
                {
                    var similarity = CalculateCosineSimilarity(targetUserRatings, otherUserRatings);
                    if (similarity > 0.1) // Only consider users with meaningful similarity
                    {
                        similarUsers[otherUserId] = similarity;
                    }
                }

                // Sort by similarity and take top 50 similar users
                var topSimilarUsers = similarUsers
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(50)
                    .ToList();

                // Calculate predicted scores for candidate movies
                foreach (var movie in candidateMovies)
                {
                    double weightedSum = 0.0;
                    double similaritySum = 0.0;

                    foreach (var (similarUserId, similarity) in topSimilarUsers)
                    {
                        if (userItemMatrix[similarUserId].TryGetValue(movie.Id, out int rating))
                        {
                            // Get user's average rating for normalization
                            var userAvgRating = userItemMatrix[similarUserId].Values.Average();
                            var normalizedRating = rating - userAvgRating;
                            
                            weightedSum += similarity * normalizedRating;
                            similaritySum += Math.Abs(similarity);
                        }
                    }

                    if (similaritySum > 0)
                    {
                        var targetUserAvgRating = targetUserRatings.Values.Average();
                        var predictedScore = targetUserAvgRating + (weightedSum / similaritySum);
                        scores[movie.Id] = Math.Max(0, Math.Min(5, predictedScore)) / 5.0; // Normalize to 0-1
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in collaborative filtering for user: {UserId}", userId);
            }

            return scores;
        }

        /// <summary>
        /// Content-Based Filtering: Recommend movies similar to user's preferences
        /// </summary>
        private async Task<Dictionary<int, double>> GetContentBasedScoresAsync(
            string userId,
            List<Rating> userRatings,
            List<int?> watchedMovieIds,
            List<Movie> candidateMovies)
        {
            var scores = new Dictionary<int, double>();

            try
            {
                // Get user's liked movies (rated 4 or 5 stars, or watched)
                var likedMovieIds = userRatings
                    .Where(r => r.Score >= 4)
                    .Select(r => r.MovieId)
                    .Union(watchedMovieIds.Where(id => id.HasValue).Select(id => id!.Value))
                    .Distinct()
                    .ToList();

                if (!likedMovieIds.Any())
                {
                    return scores;
                }

                // Get user's liked movies with full data
                var likedMovies = await _db.Movies
                    .AsNoTracking()
                    .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                    .Where(m => likedMovieIds.Contains(m.Id))
                    .ToListAsync();

                // Build user preference profile
                var userProfile = BuildUserProfile(likedMovies, userRatings);

                // Calculate similarity for each candidate movie
                foreach (var movie in candidateMovies)
                {
                    var movieFeatures = BuildMovieFeatures(movie);
                    var similarity = CalculateContentSimilarity(userProfile, movieFeatures);
                    scores[movie.Id] = similarity;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in content-based filtering for user: {UserId}", userId);
            }

            return scores;
        }

        /// <summary>
        /// Build user preference profile from liked movies
        /// </summary>
        private Dictionary<string, double> BuildUserProfile(List<Movie> likedMovies, List<Rating> userRatings)
        {
            var profile = new Dictionary<string, double>();
            var ratingDict = userRatings.ToDictionary(r => r.MovieId, r => r.Score);

            // Genre preferences
            var genreWeights = new Dictionary<int, double>();
            foreach (var movie in likedMovies)
            {
                var rating = ratingDict.ContainsKey(movie.Id) ? ratingDict[movie.Id] : 3;
                var weight = rating / 5.0; // Normalize to 0-1
                foreach (var movieGenre in movie.MovieGenres)
                {
                    if (!genreWeights.ContainsKey(movieGenre.GenreId))
                    {
                        genreWeights[movieGenre.GenreId] = 0.0;
                    }
                    genreWeights[movieGenre.GenreId] += weight;
                }
            }

            // Normalize genre weights
            var maxGenreWeight = genreWeights.Values.DefaultIfEmpty(1).Max();
            foreach (var (genreId, weight) in genreWeights)
            {
                profile[$"genre_{genreId}"] = weight / maxGenreWeight;
            }

            // Director preferences
            var directorWeights = new Dictionary<string, double>();
            foreach (var movie in likedMovies.Where(m => !string.IsNullOrEmpty(m.Director)))
            {
                var rating = ratingDict.ContainsKey(movie.Id) ? ratingDict[movie.Id] : 3;
                var weight = rating / 5.0;
                var director = movie.Director!.ToLowerInvariant();
                if (!directorWeights.ContainsKey(director))
                {
                    directorWeights[director] = 0.0;
                }
                directorWeights[director] += weight;
            }

            var maxDirectorWeight = directorWeights.Values.DefaultIfEmpty(1).Max();
            foreach (var (director, weight) in directorWeights)
            {
                profile[$"director_{director}"] = weight / maxDirectorWeight;
            }

            // Year preferences (prefer similar years)
            if (likedMovies.Any(m => m.Year.HasValue))
            {
                var preferredYears = likedMovies
                    .Where(m => m.Year.HasValue)
                    .Select(m => m.Year!.Value)
                    .ToList();
                var avgYear = preferredYears.Average();
                profile["avg_year"] = avgYear / 2100.0; // Normalize
                profile["year_range"] = (preferredYears.Max() - preferredYears.Min()) / 100.0;
            }

            // Content type preferences
            var contentTypeWeights = new Dictionary<ContentType, double>();
            foreach (var movie in likedMovies)
            {
                var rating = ratingDict.ContainsKey(movie.Id) ? ratingDict[movie.Id] : 3;
                var weight = rating / 5.0;
                if (!contentTypeWeights.ContainsKey(movie.ContentType))
                {
                    contentTypeWeights[movie.ContentType] = 0.0;
                }
                contentTypeWeights[movie.ContentType] += weight;
            }

            var maxContentTypeWeight = contentTypeWeights.Values.DefaultIfEmpty(1).Max();
            foreach (var (contentType, weight) in contentTypeWeights)
            {
                profile[$"contenttype_{(int)contentType}"] = weight / maxContentTypeWeight;
            }

            return profile;
        }

        /// <summary>
        /// Build feature vector for a movie
        /// </summary>
        private Dictionary<string, double> BuildMovieFeatures(Movie movie)
        {
            var features = new Dictionary<string, double>();

            // Genre features
            foreach (var movieGenre in movie.MovieGenres)
            {
                features[$"genre_{movieGenre.GenreId}"] = 1.0;
            }

            // Director feature
            if (!string.IsNullOrEmpty(movie.Director))
            {
                features[$"director_{movie.Director.ToLowerInvariant()}"] = 1.0;
            }

            // Year feature
            if (movie.Year.HasValue)
            {
                features["avg_year"] = movie.Year.Value / 2100.0;
            }

            // Content type feature
            features[$"contenttype_{(int)movie.ContentType}"] = 1.0;

            return features;
        }

        /// <summary>
        /// Calculate cosine similarity between user profile and movie features
        /// </summary>
        private double CalculateContentSimilarity(
            Dictionary<string, double> userProfile,
            Dictionary<string, double> movieFeatures)
        {
            if (!userProfile.Any() || !movieFeatures.Any())
            {
                return 0.0;
            }

            // Get all unique keys
            var allKeys = userProfile.Keys.Union(movieFeatures.Keys).ToList();

            double dotProduct = 0.0;
            double userMagnitude = 0.0;
            double movieMagnitude = 0.0;

            foreach (var key in allKeys)
            {
                var userValue = userProfile.ContainsKey(key) ? userProfile[key] : 0.0;
                var movieValue = movieFeatures.ContainsKey(key) ? movieFeatures[key] : 0.0;

                dotProduct += userValue * movieValue;
                userMagnitude += userValue * userValue;
                movieMagnitude += movieValue * movieValue;
            }

            if (userMagnitude == 0 || movieMagnitude == 0)
            {
                return 0.0;
            }

            return dotProduct / (Math.Sqrt(userMagnitude) * Math.Sqrt(movieMagnitude));
        }

        /// <summary>
        /// Calculate cosine similarity between two rating vectors
        /// </summary>
        private double CalculateCosineSimilarity(
            Dictionary<int, int> ratings1,
            Dictionary<int, int> ratings2)
        {
            if (!ratings1.Any() || !ratings2.Any())
            {
                return 0.0;
            }

            // Get common movies
            var commonMovies = ratings1.Keys.Intersect(ratings2.Keys).ToList();

            if (!commonMovies.Any())
            {
                return 0.0;
            }

            // Calculate cosine similarity
            double dotProduct = 0.0;
            double magnitude1 = 0.0;
            double magnitude2 = 0.0;

            foreach (var movieId in commonMovies)
            {
                var rating1 = ratings1[movieId];
                var rating2 = ratings2[movieId];

                dotProduct += rating1 * rating2;
                magnitude1 += rating1 * rating1;
                magnitude2 += rating2 * rating2;
            }

            if (magnitude1 == 0 || magnitude2 == 0)
            {
                return 0.0;
            }

            return dotProduct / (Math.Sqrt(magnitude1) * Math.Sqrt(magnitude2));
        }

        /// <summary>
        /// Get popular movies as fallback
        /// </summary>
        private async Task<List<Movie>> GetPopularMoviesAsync(int limit)
        {
            return await _db.Movies
                .AsNoTracking()
                .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .OrderByDescending(m => m.ViewCount)
                .ThenByDescending(m => m.AverageRating ?? 0)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Enhanced similar movies using content-based filtering
        /// </summary>
        public async Task<List<Movie>> GetSimilarMoviesAIAsync(int movieId, int limit = 6)
        {
            var cacheKey = $"ai_similar_movies_{movieId}_{limit}";
            
            if (_cache.TryGetValue(cacheKey, out List<Movie>? cachedMovies) && cachedMovies != null)
            {
                return cachedMovies;
            }

            try
            {
                var movie = await _db.Movies
                    .AsNoTracking()
                    .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                    .FirstOrDefaultAsync(m => m.Id == movieId);

                if (movie == null)
                {
                    return new List<Movie>();
                }

                var movieFeatures = BuildMovieFeatures(movie);
                var allMovies = await _db.Movies
                    .AsNoTracking()
                    .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                    .Where(m => m.Id != movieId)
                    .ToListAsync();

                var similarities = new Dictionary<int, double>();

                foreach (var otherMovie in allMovies)
                {
                    var otherMovieFeatures = BuildMovieFeatures(otherMovie);
                    var similarity = CalculateContentSimilarity(movieFeatures, otherMovieFeatures);
                    
                    // Boost similarity for movies with same genres
                    var commonGenres = movie.MovieGenres
                        .Select(mg => mg.GenreId)
                        .Intersect(otherMovie.MovieGenres.Select(mg => mg.GenreId))
                        .Count();
                    var genreBoost = commonGenres * 0.1;
                    
                    similarities[otherMovie.Id] = similarity + genreBoost;
                }

                var similarMovies = similarities
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(limit)
                    .Select(kvp => allMovies.First(m => m.Id == kvp.Key))
                    .ToList();

                // Cache for 1 hour
                _cache.Set(cacheKey, similarMovies, TimeSpan.FromHours(1));
                
                return similarMovies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI similar movies for movie: {MovieId}", movieId);
                // Fall back to simple genre-based similarity
                return await GetSimpleSimilarMoviesAsync(movieId, limit);
            }
        }

        /// <summary>
        /// Simple genre-based similarity (fallback)
        /// </summary>
        private async Task<List<Movie>> GetSimpleSimilarMoviesAsync(int movieId, int limit)
        {
            var movie = await _db.Movies
                .AsNoTracking()
                .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .FirstOrDefaultAsync(m => m.Id == movieId);

            if (movie == null)
            {
                return new List<Movie>();
            }

            var genreIds = movie.MovieGenres.Select(mg => mg.GenreId).ToList();

            return await _db.Movies
                .AsNoTracking()
                .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .Where(m => m.Id != movieId)
                .Where(m => m.MovieGenres.Any(mg => genreIds.Contains(mg.GenreId)))
                .OrderByDescending(m => m.ViewCount)
                .ThenByDescending(m => m.AverageRating ?? 0)
                .Take(limit)
                .ToListAsync();
        }
    }
}

