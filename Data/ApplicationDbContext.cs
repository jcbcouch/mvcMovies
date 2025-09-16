using Microsoft.EntityFrameworkCore;
using MvcMovies.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace MvcMovies.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

       

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}
