using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using webxemphim.Data;
using webxemphim.Models;

namespace webxemphim.Services
{
    public class CoinService
    {
        private readonly ApplicationDbContext _db;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CoinService> _logger;

        public CoinService(
            ApplicationDbContext db,
            IServiceProvider serviceProvider,
            ILogger<CoinService> logger)
        {
            _db = db;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Lấy hoặc tạo wallet cho user
        /// </summary>
        public async Task<CoinWallet> GetOrCreateWalletAsync(string userId)
        {
            var wallet = await _db.CoinWallets
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                wallet = new CoinWallet
                {
                    UserId = userId,
                    Balance = 0,
                    TotalEarned = 0,
                    TotalSpent = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.CoinWallets.Add(wallet);
                await _db.SaveChangesAsync();
            }

            return wallet;
        }

        /// <summary>
        /// Kiếm coin (earn)
        /// </summary>
        public async Task<bool> EarnCoinsAsync(string userId, int amount, TransactionType type, string description, int? movieId = null)
        {
            if (amount <= 0) return false;

            var wallet = await GetOrCreateWalletAsync(userId);

            wallet.Balance += amount;
            wallet.TotalEarned += amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var transaction = new CoinTransaction
            {
                WalletId = wallet.Id,
                UserId = userId,
                Amount = amount,
                Type = type,
                Description = description,
                MovieId = movieId,
                CreatedAt = DateTime.UtcNow
            };

            _db.CoinTransactions.Add(transaction);
            await _db.SaveChangesAsync();

            _logger.LogInformation("User {UserId} earned {Amount} coins ({Type})", userId, amount, type);
            return true;
        }

        /// <summary>
        /// Chi tiêu coin (spend)
        /// </summary>
        public async Task<(bool Success, string? Error)> SpendCoinsAsync(string userId, int amount, TransactionType type, string description, int? movieId = null)
        {
            if (amount <= 0) return (false, "Số coin không hợp lệ");

            var wallet = await GetOrCreateWalletAsync(userId);

            if (wallet.Balance < amount)
            {
                return (false, "Không đủ coin");
            }

            wallet.Balance -= amount;
            wallet.TotalSpent += amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var transaction = new CoinTransaction
            {
                WalletId = wallet.Id,
                UserId = userId,
                Amount = -amount, // Negative for spending
                Type = type,
                Description = description,
                MovieId = movieId,
                CreatedAt = DateTime.UtcNow
            };

            _db.CoinTransactions.Add(transaction);
            await _db.SaveChangesAsync();

            _logger.LogInformation("User {UserId} spent {Amount} coins ({Type})", userId, amount, type);
            return (true, null);
        }

        /// <summary>
        /// Lấy số dư coin
        /// </summary>
        public async Task<int> GetBalanceAsync(string userId)
        {
            var wallet = await GetOrCreateWalletAsync(userId);
            return wallet.Balance;
        }

        /// <summary>
        /// Lấy lịch sử giao dịch
        /// </summary>
        public async Task<List<CoinTransaction>> GetTransactionHistoryAsync(string userId, int page = 1, int pageSize = 20)
        {
            return await _db.CoinTransactions
                .Include(t => t.Movie)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Thưởng coin khi xem phim
        /// </summary>
        public async Task RewardWatchMovieAsync(string userId, int movieId, string movieTitle)
        {
            const int coinsPerWatch = 5; // 5 coin mỗi lần xem phim

            await EarnCoinsAsync(
                userId,
                coinsPerWatch,
                TransactionType.EarnWatchMovie,
                $"Xem phim: {movieTitle}",
                movieId
            );
        }

        /// <summary>
        /// Thưởng coin khi viết review
        /// </summary>
        public async Task RewardReviewAsync(string userId, int movieId, string movieTitle)
        {
            const int coinsPerReview = 10; // 10 coin mỗi review

            await EarnCoinsAsync(
                userId,
                coinsPerReview,
                TransactionType.EarnReview,
                $"Viết review cho phim: {movieTitle}",
                movieId
            );
        }

        /// <summary>
        /// Thưởng coin khi bình luận
        /// </summary>
        public async Task RewardCommentAsync(string userId, int movieId)
        {
            const int coinsPerComment = 2; // 2 coin mỗi comment

            await EarnCoinsAsync(
                userId,
                coinsPerComment,
                TransactionType.EarnComment,
                "Bình luận phim",
                movieId
            );
        }

        /// <summary>
        /// Thưởng coin khi đăng nhập hàng ngày
        /// </summary>
        public async Task RewardDailyLoginAsync(string userId)
        {
            const int coinsPerDay = 1; // 1 coin mỗi ngày đăng nhập

            // Kiểm tra đã nhận coin hôm nay chưa
            var today = DateTime.UtcNow.Date;
            var todayTransaction = await _db.CoinTransactions
                .FirstOrDefaultAsync(t =>
                    t.UserId == userId &&
                    t.Type == TransactionType.EarnDailyLogin &&
                    t.CreatedAt.Date == today);

            if (todayTransaction == null)
            {
                await EarnCoinsAsync(
                    userId,
                    coinsPerDay,
                    TransactionType.EarnDailyLogin,
                    "Đăng nhập hàng ngày"
                );
            }
        }

        /// <summary>
        /// Thưởng coin khi chia sẻ phim
        /// </summary>
        public async Task RewardShareAsync(string userId, int movieId, string movieTitle)
        {
            const int coinsPerShare = 3; // 3 coin mỗi lần chia sẻ

            await EarnCoinsAsync(
                userId,
                coinsPerShare,
                TransactionType.EarnShare,
                $"Chia sẻ phim: {movieTitle}",
                movieId
            );
        }

        /// <summary>
        /// Mở khóa phim bằng coin
        /// </summary>
        public async Task<(bool Success, string? Error)> UnlockMovieAsync(string userId, int movieId, int coinCost)
        {
            var movie = await _db.Movies.FindAsync(movieId);
            if (movie == null) return (false, "Phim không tồn tại");

            var result = await SpendCoinsAsync(
                userId,
                coinCost,
                TransactionType.SpendUnlockMovie,
                $"Mở khóa phim: {movie.Title}",
                movieId
            );

            return result;
        }

        /// <summary>
        /// Admin điều chỉnh coin
        /// </summary>
        public async Task<bool> AdminAdjustCoinsAsync(string userId, int amount, string reason)
        {
            if (amount == 0) return false;

            var wallet = await GetOrCreateWalletAsync(userId);

            if (amount > 0)
            {
                wallet.Balance += amount;
                wallet.TotalEarned += amount;
                await EarnCoinsAsync(userId, amount, TransactionType.AdminAdjust, reason);
            }
            else
            {
                var absAmount = Math.Abs(amount);
                if (wallet.Balance >= absAmount)
                {
                    wallet.Balance -= absAmount;
                    wallet.TotalSpent += absAmount;
                    await SpendCoinsAsync(userId, absAmount, TransactionType.AdminAdjust, reason);
                }
                else
                {
                    return false;
                }
            }

            wallet.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return true;
        }
    }
}

