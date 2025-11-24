using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Data;
using webxemphim.Models;
using webxemphim.Services;
using System.Security.Claims;

namespace webxemphim.Controllers.Api
{
    [ApiController]
    [Route("api/social")]
    [Authorize]
    public class SocialApiController : ControllerBase
    {
        private readonly SocialService _socialService;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SocialApiController> _logger;

        public SocialApiController(SocialService socialService, ApplicationDbContext db, ILogger<SocialApiController> logger)
        {
            _socialService = socialService;
            _db = db;
            _logger = logger;
        }

        [HttpPost("follow/{userId}")]
        public async Task<IActionResult> FollowUser(string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            var success = await _socialService.FollowUserAsync(currentUserId, userId);
            if (!success)
            {
                return BadRequest(new { error = "Cannot follow this user" });
            }

            var followerCount = await _socialService.GetFollowerCountAsync(userId);
            return Ok(new { success = true, followerCount });
        }

        [HttpPost("unfollow/{userId}")]
        public async Task<IActionResult> UnfollowUser(string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            var success = await _socialService.UnfollowUserAsync(currentUserId, userId);
            if (!success)
            {
                return BadRequest(new { error = "Cannot unfollow this user" });
            }

            var followerCount = await _socialService.GetFollowerCountAsync(userId);
            return Ok(new { success = true, followerCount });
        }

        [HttpGet("follow-status/{userId}")]
        public async Task<IActionResult> GetFollowStatus(string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            var isFollowing = await _socialService.IsFollowingAsync(currentUserId, userId);
            var followerCount = await _socialService.GetFollowerCountAsync(userId);
            var followingCount = await _socialService.GetFollowingCountAsync(userId);

            return Ok(new { isFollowing, followerCount, followingCount });
        }

        [HttpGet("followers/{userId}")]
        public async Task<IActionResult> GetFollowers(string userId, int limit = 20)
        {
            var followers = await _socialService.GetFollowersAsync(userId, limit);
            return Ok(followers.Select(u => new
            {
                u.Id,
                u.UserName,
                u.Email,
                u.AvatarUrl
            }));
        }

        [HttpGet("following/{userId}")]
        public async Task<IActionResult> GetFollowing(string userId, int limit = 20)
        {
            var following = await _socialService.GetFollowingAsync(userId, limit);
            return Ok(following.Select(u => new
            {
                u.Id,
                u.UserName,
                u.Email,
                u.AvatarUrl
            }));
        }

        [HttpPost("share/{movieId}")]
        public async Task<IActionResult> ShareMovie(int movieId, [FromBody] ShareRequest request)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            var share = await _socialService.ShareMovieAsync(currentUserId, movieId, request.Platform);
            var shareCount = await _socialService.GetShareCountAsync(movieId);

            return Ok(new { success = true, shareCount });
        }

        [HttpGet("movie/{movieId}/friend-reviews")]
        public async Task<IActionResult> GetFriendReviews(int movieId, int limit = 10)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            var reviews = await _socialService.GetFriendReviewsAsync(currentUserId, movieId, limit);
            return Ok(reviews.Select(r => new
            {
                r.Id,
                r.Content,
                r.CreatedAt,
                User = new
                {
                    r.User!.Id,
                    r.User.UserName,
                    r.User.Email,
                    r.User.AvatarUrl
                }
            }));
        }

        [HttpGet("movie/{movieId}/friend-ratings")]
        public async Task<IActionResult> GetFriendRatings(int movieId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            var ratings = await _socialService.GetFriendRatingsAsync(currentUserId, movieId);
            return Ok(ratings.Select(r => new
            {
                r.Id,
                r.Score,
                r.CreatedAt,
                User = new
                {
                    r.User!.Id,
                    r.User.UserName,
                    r.User.Email,
                    r.User.AvatarUrl
                }
            }));
        }

        [HttpGet("activity")]
        public async Task<IActionResult> GetFriendActivity(int limit = 20)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            var activities = await _socialService.GetFriendActivityAsync(currentUserId, limit);
            return Ok(activities);
        }
    }

    public class ShareRequest
    {
        public SharePlatform Platform { get; set; }
    }
}

