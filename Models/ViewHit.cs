namespace webxemphim.Models
{
    public class ViewHit
    {
        public long Id { get; set; }
        public int MovieId { get; set; }
        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
        public string? UserId { get; set; }
        public double? WatchProgress { get; set; } // Thời gian xem (giây) - để resume watching
        public double? Duration { get; set; } // Tổng thời lượng video (giây)
        public int? EpisodeNumber { get; set; } // Số tập (cho phim bộ)
        
        // Navigation properties
        public Movie? Movie { get; set; }
    }
}


