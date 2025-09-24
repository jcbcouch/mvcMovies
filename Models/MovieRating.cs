using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace MvcMovies.Models
{
    public class MovieRating
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string MovieId { get; set; } = string.Empty;
        
        [Required]
        [Range(1, 10)]
        public int Rating { get; set; }
        
        [StringLength(1000)]
        public string? Review { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;
        
        [ForeignKey(nameof(MovieId))]
        public virtual MovieStore Movie { get; set; } = null!;
    }
}
