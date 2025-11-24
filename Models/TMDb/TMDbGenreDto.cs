using System.Text.Json.Serialization;

namespace webxemphim.Models.TMDb
{
    public class TMDbGenreDto
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }
}
