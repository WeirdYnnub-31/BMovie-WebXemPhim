namespace webxemphim.Models.DTOs
{
    public class MovieDto
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
        public string ContentType { get; set; } = "Movie";
        public List<string> Genres { get; set; } = new();
        public long ViewCount { get; set; }
        public double? AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public string? Director { get; set; }
        public string? Cast { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public int? TMDbId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MovieListItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? PosterUrl { get; set; }
        public double? Imdb { get; set; }
        public int? Year { get; set; }
        public string? AgeRating { get; set; }
        public bool IsSeries { get; set; }
        public string ContentType { get; set; } = "Movie";
        public List<string> Genres { get; set; } = new();
        public long ViewCount { get; set; }
        public double? AverageRating { get; set; }
    }

    public class CreateMovieDto
    {
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
        public List<int> GenreIds { get; set; } = new();
        public string? Director { get; set; }
        public string? Cast { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public int? TMDbId { get; set; }
        /// <summary>
        /// Optional: Movie sources (m3u8, mpd, embed links) to add with the movie
        /// </summary>
        public List<CreateMovieSourceDto>? Sources { get; set; }
    }

    public class UpdateMovieDto
    {
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
        public List<int> GenreIds { get; set; } = new();
        public string? Director { get; set; }
        public string? Cast { get; set; }
        public DateTime? ReleaseDate { get; set; }
    }

    public class PagedResultDto<T>
    {
        public List<T> Data { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
}

