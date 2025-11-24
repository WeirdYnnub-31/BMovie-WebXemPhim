using System.Text.Json.Serialization;

namespace webxemphim.Models.TMDb
{
    public class TMDbMovieDto
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("poster_path")] public string? PosterPath { get; set; }
        [JsonPropertyName("vote_average")] public double? VoteAverage { get; set; }
        [JsonPropertyName("adult")] public bool Adult { get; set; }
        [JsonPropertyName("overview")] public string? Overview { get; set; }
        [JsonPropertyName("release_date")] public string? ReleaseDate { get; set; }
        [JsonPropertyName("genres")] public List<TMDbGenreDto>? Genres { get; set; }
        [JsonPropertyName("runtime")] public int? Runtime { get; set; }
        [JsonPropertyName("production_countries")] public List<TMDbCountryDto>? ProductionCountries { get; set; }
        public List<string>? Cast { get; set; } // Cast names (not from JSON, populated from credits API)
        public string? Director { get; set; } // Director name (not from JSON, populated from credits API)
    }

    public class TMDbCountryDto
    {
        [JsonPropertyName("iso_3166_1")] public string? IsoCode { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    public class TMDbCastDto
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("character")] public string? Character { get; set; }
        [JsonPropertyName("order")] public int Order { get; set; }
        [JsonPropertyName("profile_path")] public string? ProfilePath { get; set; }
    }

    public class TMDbCrewDto
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("job")] public string? Job { get; set; }
        [JsonPropertyName("department")] public string? Department { get; set; }
    }

    public class TMDbCreditsDto
    {
        [JsonPropertyName("cast")] public List<TMDbCastDto>? Cast { get; set; }
        [JsonPropertyName("crew")] public List<TMDbCrewDto>? Crew { get; set; }
    }
}


