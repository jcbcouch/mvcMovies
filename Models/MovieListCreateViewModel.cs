using System.ComponentModel.DataAnnotations;

namespace MvcMovies.Models
{
    public class MovieListCreateViewModel
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        public bool IsPublic { get; set; }
    }
}
