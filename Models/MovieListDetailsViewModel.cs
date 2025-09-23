using System.Collections.Generic;

namespace MvcMovies.Models
{
    public class MovieListDetailsViewModel
    {
        public int ListId { get; set; }
        public string Title { get; set; }
        public bool IsPublic { get; set; }
        public List<Movie> Movies { get; set; } = new List<Movie>();
        public string Referrer { get; set; } = "mylists"; // Default to "mylists" for backward compatibility
    }
}
