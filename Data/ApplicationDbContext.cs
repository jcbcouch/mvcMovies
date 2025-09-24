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
        public DbSet<MovieRating> MovieRatings { get; set; }

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

            // Configure MovieRating relationships
            builder.Entity<MovieRating>()
                .HasOne(mr => mr.User)
                .WithMany(u => u.MovieRatings)
                .HasForeignKey(mr => mr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MovieRating>()
                .HasOne(mr => mr.Movie)
                .WithMany(m => m.Ratings)
                .HasForeignKey(mr => mr.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            // Create a unique index to ensure a user can only rate a movie once
            builder.Entity<MovieRating>()
                .HasIndex(mr => new { mr.UserId, mr.MovieId })
                .IsUnique();
        }
    }
}
