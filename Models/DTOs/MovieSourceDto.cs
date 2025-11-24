namespace webxemphim.Models.DTOs
{
    public class MovieSourceDto
    {
        public int Id { get; set; }
        public int MovieId { get; set; }
        public string ServerName { get; set; } = string.Empty;
        public string Quality { get; set; } = "1080p";
        public string Language { get; set; } = "Vietsub";
        public string Url { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }

    public class CreateMovieSourceDto
    {
        public string ServerName { get; set; } = "Server 1";
        public string Quality { get; set; } = "1080p";
        public string Language { get; set; } = "Vietsub";
        public string Url { get; set; } = string.Empty;
        public bool IsDefault { get; set; } = false;
    }
}

