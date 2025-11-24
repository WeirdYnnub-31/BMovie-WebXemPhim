using Microsoft.EntityFrameworkCore;
using webxemphim.Data;
using webxemphim.Models;

namespace webxemphim.Services
{
    public class SocialService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SocialService> _logger;

        public SocialService(ApplicationDbContext db, ILogger<SocialService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // Follow/Unfollow
        public async Task<bool> FollowUserAsync(string followerId, string followingId)
        {
            if (followerId == followingId)
            {
                return false; // Cannot follow yourself
            }

            var existing = await _db.UserFollows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);

            if (existing != null)
            {
                return false; // Already following
            }

            var follow = new UserFollow
            {
                FollowerId = followerId,
                FollowingId = followingId,
                CreatedAt = DateTime.UtcNow
            };

            _db.UserFollows.Add(follow);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnfollowUserAsync(string followerId, string followingId)
        {
            var follow = await _db.UserFollows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);

            if (follow == null)
            {
                return false;
            }

            _db.UserFollows.Remove(follow);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsFollowingAsync(string followerId, string followingId)
        {
            return await _db.UserFollows
                .AnyAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);
        }

        public async Task<int> GetFollowerCountAsync(string userId)
        {
            return await _db.UserFollows.CountAsync(f => f.FollowingId == userId);
        }

        public async Task<int> GetFollowingCountAsync(string userId)
        {
            return await _db.UserFollows.CountAsync(f => f.FollowerId == userId);
        }

        public async Task<List<ApplicationUser>> GetFollowersAsync(string userId, int limit = 20)
        {
            return await _db.UserFollows
                .Include(f => f.Follower)
                .Where(f => f.FollowingId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => f.Follower!)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<ApplicationUser>> GetFollowingAsync(string userId, int limit = 20)
        {
            return await _db.UserFollows
                .Include(f => f.Following)
                .Where(f => f.FollowerId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => f.Following!)
                .Take(limit)
                .ToListAsync();
        }

        // Share
        public async Task<UserShare> ShareMovieAsync(string userId, int movieId, SharePlatform platform)
        {
            var share = new UserShare
            {
                UserId = userId,
                MovieId = movieId,
                Platform = platform,
                SharedAt = DateTime.UtcNow
            };

            _db.UserShares.Add(share);
            await _db.SaveChangesAsync();
            return share;
        }

        public async Task<int> GetShareCountAsync(int movieId)
        {
            return await _db.UserShares.CountAsync(s => s.MovieId == movieId);
        }

        // Friend Reviews
        public async Task<List<Comment>> GetFriendReviewsAsync(string userId, int movieId, int limit = 10)
        {
            // Lấy danh sách user mà userId đang follow
            var followingIds = await _db.UserFollows
                .Where(f => f.FollowerId == userId)
                .Select(f => f.FollowingId)
                .ToListAsync();

            if (!followingIds.Any())
            {
                return new List<Comment>();
            }

            // Lấy comments của những user đó về phim này
            return await _db.Comments
                .Include(c => c.User)
                .Where(c => c.MovieId == movieId 
                    && c.IsApproved 
                    && followingIds.Contains(c.UserId))
                .OrderByDescending(c => c.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<Rating>> GetFriendRatingsAsync(string userId, int movieId)
        {
            var followingIds = await _db.UserFollows
                .Where(f => f.FollowerId == userId)
                .Select(f => f.FollowingId)
                .ToListAsync();

            if (!followingIds.Any())
            {
                return new List<Rating>();
            }

            return await _db.Ratings
                .Include(r => r.User)
                .Where(r => r.MovieId == movieId && followingIds.Contains(r.UserId))
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        // Activity Feed
        public async Task<List<object>> GetFriendActivityAsync(string userId, int limit = 20)
        {
            var followingIds = await _db.UserFollows
                .Where(f => f.FollowerId == userId)
                .Select(f => f.FollowingId)
                .ToListAsync();

            if (!followingIds.Any())
            {
                return new List<object>();
            }

            var activities = new List<object>();

            // Recent comments from friends
            var friendComments = await _db.Comments
                .Include(c => c.User)
                .Include(c => c.Movie)
                .Where(c => c.IsApproved && followingIds.Contains(c.UserId))
                .OrderByDescending(c => c.CreatedAt)
                .Take(limit / 2)
                .ToListAsync();

            foreach (var comment in friendComments)
            {
                activities.Add(new
                {
                    Type = "comment",
                    User = comment.User,
                    Movie = comment.Movie,
                    Content = comment.Content,
                    CreatedAt = comment.CreatedAt
                });
            }

            // Recent ratings from friends
            var friendRatings = await _db.Ratings
                .Include(r => r.User)
                .Include(r => r.Movie)
                .Where(r => followingIds.Contains(r.UserId))
                .OrderByDescending(r => r.CreatedAt)
                .Take(limit / 2)
                .ToListAsync();

            foreach (var rating in friendRatings)
            {
                activities.Add(new
                {
                    Type = "rating",
                    User = rating.User,
                    Movie = rating.Movie,
                    Score = rating.Score,
                    CreatedAt = rating.CreatedAt
                });
            }

            return activities.OrderByDescending(a => ((dynamic)a).CreatedAt).Take(limit).ToList();
        }
    }
}

