using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using webxemphim.Services;

namespace webxemphim.Controllers.Api
{
    [ApiController]
    [Route("api/v2/achievements")]
    [Authorize]
    public class AchievementsApiController : ControllerBase
    {
        private readonly AchievementService _achievementService;
        private readonly ILogger<AchievementsApiController> _logger;

        public AchievementsApiController(
            AchievementService achievementService,
            ILogger<AchievementsApiController> logger)
        {
            _achievementService = achievementService;
            _logger = logger;
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUserAchievements()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var achievements = await _achievementService.GetUserAchievementsAsync(userId);
            return Ok(achievements.Select(a => new
            {
                id = a.Id,
                achievementId = a.AchievementId,
                achievementName = a.Achievement?.Name,
                description = a.Achievement?.Description,
                progress = a.Progress,
                requirement = a.Achievement?.Requirement ?? 0,
                isUnlocked = a.IsUnlocked,
                unlockedAt = a.UnlockedAt,
                rewardCoins = a.Achievement?.RewardCoins ?? 0
            }));
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllAchievements()
        {
            var achievements = await _achievementService.GetAllAchievementsAsync();
            return Ok(achievements.Select(a => new
            {
                id = a.Id,
                name = a.Name,
                description = a.Description,
                iconUrl = a.IconUrl,
                type = a.Type.ToString(),
                requirement = a.Requirement,
                rewardCoins = a.RewardCoins
            }));
        }

        [HttpGet("ranking")]
        public async Task<IActionResult> GetRanking(int limit = 10)
        {
            var rankings = await _achievementService.GetAchievementRankingAsync(limit);
            return Ok(rankings.Select(r => new
            {
                userId = r.User.Id,
                userName = r.User.UserName,
                email = r.User.Email,
                achievementCount = r.AchievementCount,
                totalCoins = r.TotalCoins
            }));
        }
    }
}

