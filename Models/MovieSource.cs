namespace webxemphim.Models
{
    public class MovieSource
    {
        public int Id { get; set; }
        public int MovieId { get; set; }
        public string ServerName { get; set; } = string.Empty; // e.g., Server 1
        public string Quality { get; set; } = "1080p"; // 4K/1080p/720p
        public string Language { get; set; } = "Vietsub"; // Vietsub/Thuyết minh/Lồng tiếng
        public string Url { get; set; } = string.Empty; // HLS/DASH URL
        public bool IsDefault { get; set; }
        public int? EpisodeNumber { get; set; } // Null => tập đơn (phim lẻ)

        public Movie Movie { get; set; } = null!;
    }
}


