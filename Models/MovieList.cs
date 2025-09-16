using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MvcMovies.Models
{
    public class MovieList
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Title { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Whether this list is publicly visible to other users
        public bool IsPublic { get; set; } = false;
        
        [Required]
        public string UserId { get; set; }
        
        [ForeignKey("UserId")]
        [ValidateNever]
        public ApplicationUser User { get; set; }
        
        [ValidateNever]
        public ICollection<MovieListItem> MovieListItems { get; set; } = new List<MovieListItem>();
    }
}
