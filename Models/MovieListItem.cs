using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MvcMovies.Models
{
    public class MovieListItem
    {
        public int Id { get; set; }
        
        [Required]
        public int MovieListId { get; set; }
        
        [Required]
        public string ImdbID { get; set; }
        
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        [ForeignKey("MovieListId")]
        public MovieList MovieList { get; set; }
        
        // Note: This won't be a real foreign key since we're not storing movies in our DB
        // It's just for convenience in our application
        [NotMapped]
        public Movie Movie { get; set; }
    }
}
