namespace webxemphim.Models
{
    public class MovieViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? PosterPath { get; set; }
        public double? VoteAverage { get; set; }
        public string? Overview { get; set; }
        public string? ReleaseDate { get; set; }
        public string? PosterUrl { get; set; }
        public string? Slug { get; set; }
        public int Year { get; set; }
        public string? Description { get; set; }
        public List<string> Genres { get; set; } = new();
    }
}
