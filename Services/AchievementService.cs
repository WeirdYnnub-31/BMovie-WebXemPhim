using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using webxemphim.Data;
using webxemphim.Models;

namespace webxemphim.Services
{
    public class AchievementService
    {
        private readonly ApplicationDbContext _db;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AchievementService> _logger;

        public AchievementService(
            ApplicationDbContext db,
            IServiceProvider serviceProvider,
            ILogger<AchievementService> logger)
        {
            _db = db;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Kiểm tra và cập nhật tiến độ achievement
        /// </summary>
        public async Task CheckAndUpdateAchievementsAsync(string userId, AchievementType type, int increment = 1)
        {
            var achievements = await _db.Achievements
                .Where(a => a.Type == type && a.IsActive)
                .ToListAsync();

            foreach (var achievement in achievements)
            {
                var userAchievement = await _db.UserAchievements
                    .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AchievementId == achievement.Id);

                if (userAchievement == null)
                {
                    // Tạo mới user achievement
                    userAchievement = new UserAchievement
                    {
                        UserId = userId,
                        AchievementId = achievement.Id,
                        Progress = increment,
                        IsUnlocked = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.UserAchievements.Add(userAchievement);
                }
                else if (!userAchievement.IsUnlocked)
                {
                    // Cập nhật tiến độ
                    userAchievement.Progress += increment;
                }

                // Kiểm tra đã đạt achievement chưa
                if (!userAchievement.IsUnlocked && userAchievement.Progress >= achievement.Requirement)
                {
                    userAchievement.IsUnlocked = true;
                    userAchievement.UnlockedAt = DateTime.UtcNow;

                    // Thưởng coin và gửi thông báo (sử dụng service provider để tránh circular dependency)
                    using var scope = _serviceProvider.CreateScope();
                    if (achievement.RewardCoins > 0)
                    {
                        var coinService = scope.ServiceProvider.GetRequiredService<CoinService>();
                        await coinService.EarnCoinsAsync(
                            userId,
                            achievement.RewardCoins,
                            TransactionType.EarnWatchMovie, // Có thể thêm TransactionType.Achievement
                            $"Thành tích: {achievement.Name}",
                            null
                        );
                    }

                    var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
                    await notificationService.NotifyAchievementAsync(
                        userId,
                        achievement.Name,
                        achievement.RewardCoins
                    );

                    _logger.LogInformation("User {UserId} unlocked achievement {AchievementId}", userId, achievement.Id);
                }

                await _db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Lấy danh sách achievements của user
        /// </summary>
        public async Task<List<UserAchievement>> GetUserAchievementsAsync(string userId)
        {
            return await _db.UserAchievements
                .Include(ua => ua.Achievement)
                .Where(ua => ua.UserId == userId)
                .OrderByDescending(ua => ua.UnlockedAt)
                .ThenBy(ua => ua.Achievement.Requirement)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy tất cả achievements có sẵn
        /// </summary>
        public async Task<List<Achievement>> GetAllAchievementsAsync()
        {
            return await _db.Achievements
                .Where(a => a.IsActive)
                .OrderBy(a => a.Requirement)
                .ToListAsync();
        }

        /// <summary>
        /// Tạo achievement mới (admin only)
        /// </summary>
        public async Task<Achievement> CreateAchievementAsync(
            string name,
            string description,
            string iconUrl,
            AchievementType type,
            int requirement,
            int rewardCoins = 0)
        {
            var achievement = new Achievement
            {
                Name = name,
                Description = description,
                IconUrl = iconUrl,
                Type = type,
                Requirement = requirement,
                RewardCoins = rewardCoins,
                IsActive = true
            };

            _db.Achievements.Add(achievement);
            await _db.SaveChangesAsync();

            return achievement;
        }

        /// <summary>
        /// Seed các achievements mặc định
        /// </summary>
        public async Task SeedDefaultAchievementsAsync()
        {
            if (await _db.Achievements.AnyAsync()) return;

            var defaultAchievements = new List<Achievement>
            {
                new() { Name = "Người mới", Description = "Xem 1 phim", Type = AchievementType.MoviesWatched, Requirement = 1, RewardCoins = 10, IsActive = true },
                new() { Name = "Người xem phim", Description = "Xem 10 phim", Type = AchievementType.MoviesWatched, Requirement = 10, RewardCoins = 50, IsActive = true },
                new() { Name = "Người xem chuyên nghiệp", Description = "Xem 50 phim", Type = AchievementType.MoviesWatched, Requirement = 50, RewardCoins = 200, IsActive = true },
                new() { Name = "Người xem xuất sắc", Description = "Xem 100 phim", Type = AchievementType.MoviesWatched, Requirement = 100, RewardCoins = 500, IsActive = true },
                new() { Name = "Nhà phê bình", Description = "Viết 10 review", Type = AchievementType.ReviewsWritten, Requirement = 10, RewardCoins = 100, IsActive = true },
                new() { Name = "Người bình luận", Description = "Viết 50 comment", Type = AchievementType.CommentsWritten, Requirement = 50, RewardCoins = 100, IsActive = true },
                new() { Name = "Người đánh giá", Description = "Đánh giá 20 phim", Type = AchievementType.MoviesRated, Requirement = 20, RewardCoins = 50, IsActive = true },
                new() { Name = "Người chơi hàng ngày", Description = "Đăng nhập 7 ngày liên tiếp", Type = AchievementType.DaysActive, Requirement = 7, RewardCoins = 50, IsActive = true },
                new() { Name = "Người chơi trung thành", Description = "Đăng nhập 30 ngày liên tiếp", Type = AchievementType.DaysActive, Requirement = 30, RewardCoins = 200, IsActive = true },
            };

            _db.Achievements.AddRange(defaultAchievements);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Seeded {Count} default achievements", defaultAchievements.Count);
        }

        /// <summary>
        /// Lấy ranking users theo achievements
        /// </summary>
        public async Task<List<(ApplicationUser User, int AchievementCount, int TotalCoins)>> GetAchievementRankingAsync(int limit = 10)
        {
            var rankings = await _db.UserAchievements
                .Where(ua => ua.IsUnlocked)
                .GroupBy(ua => ua.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    AchievementCount = g.Count()
                })
                .OrderByDescending(x => x.AchievementCount)
                .Take(limit)
                .ToListAsync();

            var result = new List<(ApplicationUser User, int AchievementCount, int TotalCoins)>();

            foreach (var ranking in rankings)
            {
                var user = await _db.Users.FindAsync(ranking.UserId);
                if (user == null) continue;

                var wallet = await _db.CoinWallets
                    .FirstOrDefaultAsync(w => w.UserId == ranking.UserId);

                result.Add((user, ranking.AchievementCount, wallet?.TotalEarned ?? 0));
            }

            return result;
        }
    }
}

