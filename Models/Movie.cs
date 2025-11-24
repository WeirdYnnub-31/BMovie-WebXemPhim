using Microsoft.AspNetCore.Identity;
using webxemphim.Data;

namespace webxemphim.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? PosterUrl { get; set; }
        public string? TrailerUrl { get; set; }
        public double? Imdb { get; set; }
        public string? AgeRating { get; set; }
        public int? DurationMinutes { get; set; }
        public int? Year { get; set; }
        public string? Country { get; set; }
        public string? Description { get; set; }
        public bool IsSeries { get; set; }
        public ContentType ContentType { get; set; } = ContentType.Movie; // Movie, TVShow, Anime, Trailer, BehindTheScenes
        public bool RequiresCoins { get; set; } = false; // Phim yêu cầu coin để xem
        public int CoinCost { get; set; } = 0; // Số coin cần để xem
        public bool IsDownloadable { get; set; } = false; // Cho phép download
        public DateTime? UpcomingReleaseDate { get; set; } // Ngày phát hành sắp tới
        public List<MovieGenre> MovieGenres { get; set; } = new();
        public long ViewCount { get; set; }
        public double? AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public string? Director { get; set; }
        public string? Cast { get; set; } // Comma-separated actors
        public DateTime? ReleaseDate { get; set; }
        public int? TMDbId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public List<Subtitle> Subtitles { get; set; } = new();
        public List<WatchParty> WatchParties { get; set; } = new();
    }

    public enum ContentType
    {
        Movie,              // Phim lẻ
        TVShow,             // TV series
        Anime,              // Anime
        Trailer,            // Trailer
        BehindTheScenes,    // Behind the scenes
        Documentary,        // Phim tài liệu
        ShortFilm           // Phim ngắn
    }

    public class Genre
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public List<MovieGenre> MovieGenres { get; set; } = new();
    }

    public class MovieGenre
    {
        public int MovieId { get; set; }
        public Movie Movie { get; set; } = null!;
        public int GenreId { get; set; }
        public Genre Genre { get; set; } = null!;
    }

    public enum InventoryItemType
    {
        Favorite,
        Watched,
        Voucher,
        Badge
    }

    public class UserInventoryItem
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int? MovieId { get; set; }
        public InventoryItemType Type { get; set; }
        public string? Payload { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public Movie? Movie { get; set; }
    }

    public class Rating
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int MovieId { get; set; }
        public int Score { get; set; } // 1-5 stars
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public ApplicationUser? User { get; set; }
        public Movie? Movie { get; set; }
    }
}


