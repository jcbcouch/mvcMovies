using System.Text.Json.Serialization;

namespace MvcMovies.Models
{
    public class Movie
    {
        [JsonPropertyName("Title")]
        public string? Title { get; set; }
        
        [JsonPropertyName("Year")]
        public string? Year { get; set; }
        
        [JsonPropertyName("Rated")]
        public string? Rated { get; set; }
        
        [JsonPropertyName("Released")]
        public string? Released { get; set; }
        
        [JsonPropertyName("Runtime")]
        public string? Runtime { get; set; }
        
        [JsonPropertyName("Genre")]
        public string? Genre { get; set; }
        
        [JsonPropertyName("Director")]
        public string? Director { get; set; }
        
        [JsonPropertyName("Plot")]
        public string? Plot { get; set; }
        
        [JsonPropertyName("Poster")]
        public string? Poster { get; set; }
        
        [JsonPropertyName("imdbRating")]
        public string? ImdbRating { get; set; }
        
        [JsonPropertyName("imdbID")]
        public string? ImdbID { get; set; }
    }
}
