using MvcMovies.Models;

namespace MvcMovies.Models.ViewModels
{
    public class MovieDetailsViewModel
    {
        public Movie Movie { get; set; }
        public int? UserRating { get; set; }
    }
}
