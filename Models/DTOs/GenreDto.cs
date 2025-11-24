namespace webxemphim.Models.DTOs
{
    public class GenreDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public int MovieCount { get; set; }
    }

    public class CreateGenreDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
    }

    public class UpdateGenreDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
    }
}

