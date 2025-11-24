using webxemphim.Data;

namespace webxemphim.Models
{
    public class UserFollow
    {
        public int Id { get; set; }
        public string FollowerId { get; set; } = string.Empty; // Người follow
        public string FollowingId { get; set; } = string.Empty; // Người được follow
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ApplicationUser? Follower { get; set; }
        public ApplicationUser? Following { get; set; }
    }

    public class UserShare
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int MovieId { get; set; }
        public SharePlatform Platform { get; set; }
        public DateTime SharedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ApplicationUser? User { get; set; }
        public Movie? Movie { get; set; }
    }

    public enum SharePlatform
    {
        Facebook,
        Twitter,
        Instagram,
        WhatsApp,
        Email,
        CopyLink
    }
}

