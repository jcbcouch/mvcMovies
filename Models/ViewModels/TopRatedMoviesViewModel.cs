using System.Collections.Generic;
using MvcMovies.Models;

namespace MvcMovies.Models.ViewModels
{
    public class TopRatedMoviesViewModel
    {
        public List<MovieRatingViewModel> TopRatedMovies { get; set; } = new();
    }

    public class MovieRatingViewModel
    {
        public Movie Movie { get; set; }
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
    }
}