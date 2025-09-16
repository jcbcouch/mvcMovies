using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MvcMovies.Models
{
    public class ApplicationUser : IdentityUser
    {
        // You can add additional properties for your user here
        // Example:
        // public string FirstName { get; set; }
        // public string LastName { get; set; }
        
        // Navigation property for the lists this user owns
        [ValidateNever]
        public ICollection<MovieList> MovieLists { get; set; } = new List<MovieList>();
    }
}
