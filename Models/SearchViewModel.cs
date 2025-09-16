using System.Collections.Generic;

namespace MvcMovies.Models
{
    public class SearchViewModel
    {
        public string SearchQuery { get; set; }
        public List<Movie> SearchResults { get; set; } = new List<Movie>();
    }
}
