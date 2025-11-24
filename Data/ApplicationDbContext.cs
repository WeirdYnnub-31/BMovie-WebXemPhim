using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using webxemphim.Models;

namespace webxemphim.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string? ThemePreference { get; set; } = "light"; // light, dark, auto
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int LoginStreak { get; set; } = 0; // Số ngày đăng nhập liên tiếp
        public DateTime? LastLoginDate { get; set; } // Ngày đăng nhập cuối cùng
        
        // Navigation properties
        public List<Notification> Notifications { get; set; } = new();
        public List<CoinWallet> CoinWallets { get; set; } = new();
        public List<UserAchievement> UserAchievements { get; set; } = new();
        public List<UserFollow> Followers { get; set; } = new(); // Những người follow mình
        public List<UserFollow> Following { get; set; } = new(); // Những người mình follow
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Movie> Movies => Set<Movie>();
        public DbSet<Genre> Genres => Set<Genre>();
        public DbSet<MovieGenre> MovieGenres => Set<MovieGenre>();
        public DbSet<UserInventoryItem> UserInventoryItems => Set<UserInventoryItem>();
        public DbSet<webxemphim.Models.Comment> Comments => Set<webxemphim.Models.Comment>();
        public DbSet<webxemphim.Models.MovieSource> MovieSources => Set<webxemphim.Models.MovieSource>();
        public DbSet<webxemphim.Models.ViewHit> ViewHits => Set<webxemphim.Models.ViewHit>();
        public DbSet<webxemphim.Models.Rating> Ratings => Set<webxemphim.Models.Rating>();
        public DbSet<webxemphim.Services.AuditLog> AuditLogs => Set<webxemphim.Services.AuditLog>();
        public DbSet<webxemphim.Models.Notification> Notifications => Set<webxemphim.Models.Notification>();
        public DbSet<webxemphim.Models.WatchParty> WatchParties => Set<webxemphim.Models.WatchParty>();
        public DbSet<webxemphim.Models.WatchPartyParticipant> WatchPartyParticipants => Set<webxemphim.Models.WatchPartyParticipant>();
        public DbSet<webxemphim.Models.WatchPartyMessage> WatchPartyMessages => Set<webxemphim.Models.WatchPartyMessage>();
        public DbSet<webxemphim.Models.CoinWallet> CoinWallets => Set<webxemphim.Models.CoinWallet>();
        public DbSet<webxemphim.Models.CoinTransaction> CoinTransactions => Set<webxemphim.Models.CoinTransaction>();
        public DbSet<webxemphim.Models.Achievement> Achievements => Set<webxemphim.Models.Achievement>();
        public DbSet<webxemphim.Models.UserAchievement> UserAchievements => Set<webxemphim.Models.UserAchievement>();
        public DbSet<webxemphim.Models.Subtitle> Subtitles => Set<webxemphim.Models.Subtitle>();
        public DbSet<webxemphim.Models.UserSubtitleSettings> UserSubtitleSettings => Set<webxemphim.Models.UserSubtitleSettings>();
        public DbSet<webxemphim.Models.UserFollow> UserFollows => Set<webxemphim.Models.UserFollow>();
        public DbSet<webxemphim.Models.UserShare> UserShares => Set<webxemphim.Models.UserShare>();
        public DbSet<webxemphim.Models.Feedback> Feedbacks => Set<webxemphim.Models.Feedback>();
        public DbSet<webxemphim.Models.UserSession> UserSessions => Set<webxemphim.Models.UserSession>();
        public DbSet<webxemphim.Models.ApiKey> ApiKeys => Set<webxemphim.Models.ApiKey>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Movie>(e =>
            {
                e.HasIndex(x => x.Slug).IsUnique(false);
                e.Property(x => x.Title).IsRequired().HasMaxLength(256);
                e.Property(x => x.Country).HasMaxLength(64);
                e.Property(x => x.AgeRating).HasMaxLength(8);
            });

            builder.Entity<Genre>(e =>
            {
                e.Property(x => x.Name).IsRequired().HasMaxLength(128);
                e.Property(x => x.Slug).HasMaxLength(128);
            });

            builder.Entity<MovieGenre>(e =>
            {
                e.HasKey(x => new { x.MovieId, x.GenreId });
                e.HasOne(x => x.Movie).WithMany(x => x.MovieGenres).HasForeignKey(x => x.MovieId);
                e.HasOne(x => x.Genre).WithMany(x => x.MovieGenres).HasForeignKey(x => x.GenreId);
            });

            builder.Entity<UserInventoryItem>(e =>
            {
                e.HasIndex(x => new { x.UserId, x.MovieId, x.Type }).IsUnique(false);
                e.Property(x => x.Payload).HasMaxLength(1024);
            });

            builder.Entity<webxemphim.Models.Comment>(e =>
            {
                e.Property(x => x.Content).IsRequired().HasMaxLength(2000);
                e.HasOne(x => x.Movie).WithMany().HasForeignKey(x => x.MovieId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.ParentComment).WithMany(x => x.Replies).HasForeignKey(x => x.ParentCommentId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(x => new { x.MovieId, x.IsApproved });
                e.HasIndex(x => x.ParentCommentId);
            });

            builder.Entity<webxemphim.Models.MovieSource>(e =>
            {
                e.Property(x => x.ServerName).HasMaxLength(64).IsRequired();
                e.Property(x => x.Quality).HasMaxLength(32).IsRequired();
                e.Property(x => x.Language).HasMaxLength(32).IsRequired();
                e.Property(x => x.Url).HasMaxLength(1024).IsRequired();
                e.Property(x => x.IsDefault).HasDefaultValue(false);
                e.HasOne(x => x.Movie).WithMany().HasForeignKey(x => x.MovieId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.MovieId, x.ServerName, x.Quality, x.Language });
            });

            builder.Entity<webxemphim.Models.ViewHit>(e =>
            {
                e.HasIndex(x => x.MovieId);
                e.HasIndex(x => x.ViewedAt);
                e.HasIndex(x => new { x.UserId, x.MovieId, x.EpisodeNumber });
            });

            builder.Entity<webxemphim.Models.UserSession>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.UserId);
                e.HasIndex(x => x.SessionToken).IsUnique();
                e.HasIndex(x => x.LastActivityAt);
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<webxemphim.Models.Rating>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Score).IsRequired();
                e.HasIndex(x => new { x.UserId, x.MovieId }).IsUnique();
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Movie).WithMany().HasForeignKey(x => x.MovieId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<webxemphim.Services.AuditLog>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Action).IsRequired().HasMaxLength(64);
                e.Property(x => x.EntityType).HasMaxLength(64);
                e.Property(x => x.Details).HasMaxLength(2000);
                e.Property(x => x.IpAddress).HasMaxLength(45); // IPv6 support
                e.HasIndex(x => x.UserId);
                e.HasIndex(x => x.Action);
                e.HasIndex(x => new { x.EntityType, x.EntityId });
                e.HasIndex(x => x.CreatedAt);
            });

            // Notification - Sử dụng Restrict cho tất cả FK để tránh multiple cascade paths
            builder.Entity<webxemphim.Models.Notification>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Title).IsRequired().HasMaxLength(256);
                e.Property(x => x.Message).IsRequired().HasMaxLength(2000);
                e.Property(x => x.Link).HasMaxLength(512);
                e.HasIndex(x => new { x.UserId, x.IsRead });
                e.HasIndex(x => x.CreatedAt);
                e.HasOne(x => x.User).WithMany(u => u.Notifications).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Movie).WithMany().HasForeignKey(x => x.MovieId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Comment).WithMany().HasForeignKey(x => x.CommentId).OnDelete(DeleteBehavior.Restrict);
            });

            // WatchParty
            builder.Entity<webxemphim.Models.WatchParty>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.RoomId).IsRequired().HasMaxLength(64);
                e.Property(x => x.RoomName).IsRequired().HasMaxLength(256);
                e.HasIndex(x => x.RoomId).IsUnique();
                e.HasIndex(x => x.HostId);
                e.HasOne(x => x.Host).WithMany().HasForeignKey(x => x.HostId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Movie).WithMany(m => m.WatchParties).HasForeignKey(x => x.MovieId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<webxemphim.Models.WatchPartyParticipant>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.WatchPartyId, x.UserId }).IsUnique();
                e.HasOne(x => x.WatchParty).WithMany(wp => wp.Participants).HasForeignKey(x => x.WatchPartyId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<webxemphim.Models.WatchPartyMessage>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Message).IsRequired().HasMaxLength(1000);
                e.HasIndex(x => x.WatchPartyId);
                e.HasIndex(x => x.CreatedAt);
                e.HasOne(x => x.WatchParty).WithMany(wp => wp.Messages).HasForeignKey(x => x.WatchPartyId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            // CoinWallet
            builder.Entity<webxemphim.Models.CoinWallet>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.UserId).IsUnique();
                e.HasOne(x => x.User).WithMany(u => u.CoinWallets).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<webxemphim.Models.CoinTransaction>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Description).IsRequired().HasMaxLength(256);
                e.HasIndex(x => x.UserId);
                e.HasIndex(x => x.CreatedAt);
                e.HasOne(x => x.Wallet).WithMany(w => w.Transactions).HasForeignKey(x => x.WalletId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Movie).WithMany().HasForeignKey(x => x.MovieId).OnDelete(DeleteBehavior.SetNull);
            });

            // Achievement
            builder.Entity<webxemphim.Models.Achievement>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).IsRequired().HasMaxLength(128);
                e.Property(x => x.Description).IsRequired().HasMaxLength(512);
                e.Property(x => x.IconUrl).HasMaxLength(512);
            });

            builder.Entity<webxemphim.Models.UserAchievement>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.UserId, x.AchievementId }).IsUnique();
                e.HasOne(x => x.User).WithMany(u => u.UserAchievements).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Achievement).WithMany(a => a.UserAchievements).HasForeignKey(x => x.AchievementId).OnDelete(DeleteBehavior.Cascade);
            });

            // Subtitle
            builder.Entity<webxemphim.Models.Subtitle>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Language).IsRequired().HasMaxLength(10);
                e.Property(x => x.LanguageName).IsRequired().HasMaxLength(64);
                e.Property(x => x.FileUrl).IsRequired().HasMaxLength(1024);
                e.Property(x => x.FilePath).HasMaxLength(1024);
                e.HasIndex(x => x.MovieId);
                e.HasOne(x => x.Movie).WithMany(m => m.Subtitles).HasForeignKey(x => x.MovieId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Uploader).WithMany().HasForeignKey(x => x.UploadedBy).OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<webxemphim.Models.UserSubtitleSettings>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.UserId).IsUnique();
                e.Property(x => x.FontFamily).HasMaxLength(64);
                e.Property(x => x.FontColor).HasMaxLength(16);
                e.Property(x => x.BackgroundColor).HasMaxLength(16);
                e.Property(x => x.Position).HasMaxLength(16);
                e.Property(x => x.PreferredLanguage).HasMaxLength(10);
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            // Social
            builder.Entity<webxemphim.Models.UserFollow>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.FollowerId, x.FollowingId }).IsUnique();
                e.HasOne(x => x.Follower).WithMany(u => u.Following).HasForeignKey(x => x.FollowerId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Following).WithMany(u => u.Followers).HasForeignKey(x => x.FollowingId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<webxemphim.Models.UserShare>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.UserId);
                e.HasIndex(x => x.MovieId);
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Movie).WithMany().HasForeignKey(x => x.MovieId).OnDelete(DeleteBehavior.Cascade);
            });

            // Feedback - Sử dụng Restrict để tránh multiple cascade paths
            builder.Entity<webxemphim.Models.Feedback>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Title).IsRequired().HasMaxLength(256);
                e.Property(x => x.Description).IsRequired().HasMaxLength(2000);
                e.Property(x => x.MovieSourceUrl).HasMaxLength(1024);
                e.Property(x => x.AdminResponse).HasMaxLength(2000);
                e.HasIndex(x => x.UserId);
                e.HasIndex(x => x.Status);
                e.HasIndex(x => x.CreatedAt);
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Movie).WithMany().HasForeignKey(x => x.MovieId).OnDelete(DeleteBehavior.SetNull);
                e.HasOne(x => x.Admin).WithMany().HasForeignKey(x => x.AdminId).OnDelete(DeleteBehavior.SetNull);
            });

            // ApiKey
            builder.Entity<webxemphim.Models.ApiKey>(e =>
            {
                e.ToTable("ApiKeys");
                e.HasKey(x => x.Id);
                e.Property(x => x.Key).HasMaxLength(256).IsRequired();
                e.Property(x => x.Name).HasMaxLength(256).IsRequired();
                e.Property(x => x.AllowedIps).HasMaxLength(512);
                e.HasIndex(x => x.Key).IsUnique();
                e.HasIndex(x => x.UserId);
                e.HasIndex(x => x.IsActive);
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
                // Store AllowedEndpoints as JSON string
                e.Property(x => x.AllowedEndpoints)
                    .HasConversion(
                        v => string.Join(",", v),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
            });

            // ApplicationUser extensions
            builder.Entity<ApplicationUser>(e =>
            {
                e.Property(x => x.ThemePreference).HasMaxLength(16);
                e.Property(x => x.AvatarUrl).HasMaxLength(512);
                e.Property(x => x.Bio).HasMaxLength(500);
            });
        }
    }
}


