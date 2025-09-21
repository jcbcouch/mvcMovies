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

        public DbSet<MovieList> MovieLists { get; set; }
        public DbSet<MovieListItem> MovieListItems { get; set; }
        public DbSet<MovieStore> Movies { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Default: lists are private unless explicitly made public
            builder.Entity<MovieList>()
                .Property(ml => ml.IsPublic)
                .HasDefaultValue(false);

            // Configure the relationship between ApplicationUser and MovieList
            builder.Entity<MovieList>()
                .HasOne(ml => ml.User)
                .WithMany(u => u.MovieLists)
                .HasForeignKey(ml => ml.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure the relationship between MovieList and MovieListItem
            builder.Entity<MovieListItem>()
                .HasOne(mli => mli.MovieList)
                .WithMany(ml => ml.MovieListItems)
                .HasForeignKey(mli => mli.MovieListId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure MovieStore to use ImdbID as primary key
            builder.Entity<MovieStore>()
                .HasKey(m => m.ImdbID);
        }
    }
}
