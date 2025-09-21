using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MvcMovies.Models
{
    [Table("Movies")] // This will be the table name in the database
    public class MovieStore
    {
        [Key] // Marks this as the primary key
        [StringLength(20)] // Reasonable length for IMDB IDs
        [Column("imdbID")] // Explicit column name to match property
        public string ImdbID { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(10)]
        public string? Year { get; set; }
        
        [StringLength(10)]
        public string? Rated { get; set; }
        
        [StringLength(20)]
        public string? Released { get; set; }
        
        [StringLength(20)]
        public string? Runtime { get; set; }
        
        [StringLength(100)]
        public string? Genre { get; set; }
        
        [StringLength(200)]
        public string? Director { get; set; }
        
        [StringLength(500)]
        public string? Writer { get; set; }
        
        [StringLength(500)]
        public string? Actors { get; set; }
        
        [StringLength(1000)]
        public string? Plot { get; set; }
        
        [StringLength(100)]
        public string? Language { get; set; }
        
        [StringLength(100)]
        public string? Country { get; set; }
        
        [StringLength(500)]
        public string? Poster { get; set; }
        
        [StringLength(10)]
        public string? ImdbRating { get; set; }
        
        [StringLength(50)]
        public string? Type { get; set; }

        // Timestamp for concurrency control
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
