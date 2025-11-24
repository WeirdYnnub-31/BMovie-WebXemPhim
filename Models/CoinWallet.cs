using webxemphim.Data;

namespace webxemphim.Models
{
    public class CoinWallet
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int Balance { get; set; } = 0;
        public int TotalEarned { get; set; } = 0; // Tổng coin đã kiếm được
        public int TotalSpent { get; set; } = 0; // Tổng coin đã tiêu
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ApplicationUser? User { get; set; }
        public List<CoinTransaction> Transactions { get; set; } = new();
    }

    public class CoinTransaction
    {
        public int Id { get; set; }
        public int WalletId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int Amount { get; set; } // Có thể âm (chi tiêu) hoặc dương (kiếm được)
        public TransactionType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public int? MovieId { get; set; } // Liên quan đến phim (nếu có)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public CoinWallet? Wallet { get; set; }
        public ApplicationUser? User { get; set; }
        public Movie? Movie { get; set; }
    }

    public enum TransactionType
    {
        EarnWatchMovie,        // Xem phim
        EarnReview,            // Viết review
        EarnComment,           // Bình luận
        EarnDailyLogin,        // Đăng nhập hàng ngày
        EarnShare,             // Chia sẻ phim
        SpendUnlockMovie,      // Mở khóa phim đặc biệt
        SpendVoucher,          // Đổi voucher
        SpendGift,             // Đổi gift
        AdminAdjust,           // Admin điều chỉnh
        Refund                 // Hoàn tiền
    }
}

