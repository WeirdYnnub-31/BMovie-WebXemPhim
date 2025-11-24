using Microsoft.EntityFrameworkCore;
using webxemphim.Data;

namespace webxemphim.Services
{
    public class RatingService
    {
        private readonly ApplicationDbContext _db;

        public RatingService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<bool> RateMovieAsync(string userId, int movieId, int score)
        {
            if (score < 1 || score > 5) return false;

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // Check if user already rated this movie
                var existingRating = await _db.Ratings
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == movieId);

                if (existingRating != null)
                {
                    // Update existing rating
                    existingRating.Score = score;
                    existingRating.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new rating
                    _db.Ratings.Add(new Models.Rating
                    {
                        UserId = userId,
                        MovieId = movieId,
                        Score = score
                    });
                }

                await _db.SaveChangesAsync();

                // Update movie's average rating and total ratings
                await UpdateMovieRatingStatsAsync(movieId);

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<int?> GetUserRatingAsync(string userId, int movieId)
        {
            var rating = await _db.Ratings
                .FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == movieId);
            
            return rating?.Score;
        }

        public async Task<double?> GetMovieAverageRatingAsync(int movieId)
        {
            var movie = await _db.Movies
                .FirstOrDefaultAsync(m => m.Id == movieId);
            
            return movie?.AverageRating;
        }

        public async Task<int> GetMovieTotalRatingsAsync(int movieId)
        {
            var movie = await _db.Movies
                .FirstOrDefaultAsync(m => m.Id == movieId);
            
            return movie?.TotalRatings ?? 0;
        }

        private async Task UpdateMovieRatingStatsAsync(int movieId)
        {
            var stats = await _db.Ratings
                .Where(r => r.MovieId == movieId)
                .GroupBy(r => r.MovieId)
                .Select(g => new
                {
                    AverageRating = g.Average(r => r.Score),
                    TotalRatings = g.Count()
                })
                .FirstOrDefaultAsync();

            var movie = await _db.Movies.FindAsync(movieId);
            if (movie != null)
            {
                movie.AverageRating = stats?.AverageRating;
                movie.TotalRatings = stats?.TotalRatings ?? 0;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<bool> RemoveRatingAsync(string userId, int movieId)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var rating = await _db.Ratings
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == movieId);

                if (rating != null)
                {
                    _db.Ratings.Remove(rating);
                    await _db.SaveChangesAsync();
                    await UpdateMovieRatingStatsAsync(movieId);
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }
    }
}
