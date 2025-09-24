using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MvcMovies.Data;
using MvcMovies.Models;
using MvcMovies.Models.ViewModels;
using MvcMovies.Services;

namespace MvcMovies.Controllers
{
    public class MoviesController : Controller
    {
        private readonly IMovieCacheService _movieCache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiKey;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MoviesController(
            IMovieCacheService movieCache,
            IHttpClientFactory httpClientFactory, 
            IConfiguration configuration, 
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager)
        {
            _movieCache = movieCache;
            _httpClientFactory = httpClientFactory;
            _apiKey = configuration["OmdbApi:ApiKey"];
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Search()
        {
            return View(new SearchViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Search(SearchViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.SearchQuery))
            {
                return View(model);
            }

            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"https://www.omdbapi.com/?s={model.SearchQuery}&apikey={_apiKey}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var searchResult = JsonSerializer.Deserialize<OmdbSearchResult>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (searchResult?.Search != null)
                {
                    // For each movie in search results, check cache first
                    var cachedMovies = new List<Movie>();
                    foreach (var movie in searchResult.Search)
                    {
                        var cachedMovie = await _movieCache.GetOrAddMovieAsync(movie.ImdbID);
                        if (cachedMovie != null)
                        {
                            cachedMovies.Add(cachedMovie);
                        }
                    }
                    model.SearchResults = cachedMovies;
                }
            }

            return View(model);
        }

        public async Task<IActionResult> Details(string id) 
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            // This will get from cache or fetch from API if not found
            var movie = await _movieCache.GetOrAddMovieAsync(id);
            
            if (movie == null)
            {
                return NotFound();
            }

            var viewModel = new MovieDetailsViewModel
            {
                Movie = movie,
                UserRating = null
            };

            // If the user is authenticated, load their movie lists and check for existing rating
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);
                
                // Load user's movie lists
                var userLists = await _context.MovieLists
                    .AsNoTracking()
                    .Where(ml => ml.UserId == userId)
                    .OrderByDescending(ml => ml.CreatedAt)
                    .ToListAsync();

                ViewBag.UserMovieLists = userLists;

                // Check if user has already rated this movie
                var existingRating = await _context.MovieRatings
                    .FirstOrDefaultAsync(r => r.MovieId == id && r.UserId == userId);

                if (existingRating != null)
                {
                    viewModel.UserRating = existingRating.Rating;
                }
            }

            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RateMovie(string movieId, int rating)
        {
            if (string.IsNullOrWhiteSpace(movieId) || rating < 1 || rating > 10)
            {
                return BadRequest("Invalid rating data");
            }

            var userId = _userManager.GetUserId(User);
            var existingRating = await _context.MovieRatings
                .FirstOrDefaultAsync(r => r.MovieId == movieId && r.UserId == userId);

            if (existingRating != null)
            {
                // Update existing rating
                existingRating.Rating = rating;
                existingRating.UpdatedAt = DateTime.UtcNow;
                _context.MovieRatings.Update(existingRating);
            }
            else
            {
                // Create new rating
                var movieRating = new MovieRating
                {
                    MovieId = movieId,
                    UserId = userId,
                    Rating = rating,
                    CreatedAt = DateTime.UtcNow
                };
                _context.MovieRatings.Add(movieRating);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = movieId });
        }

        public async Task<IActionResult> Random()
        {
            // Get a random movie from the database
            var randomMovie = await _context.Movies
                .OrderBy(m => Guid.NewGuid())
                .FirstOrDefaultAsync();

            if (randomMovie == null)
            {
                // If no movies in the database, redirect to search or show a message
                TempData["ErrorMessage"] = "No movies found in the database. Try searching for some movies first.";
                return RedirectToAction(nameof(Search));
            }

            // Map MovieStore to Movie
            var movie = new Movie
            {
                Title = randomMovie.Title,
                Year = randomMovie.Year,
                Rated = randomMovie.Rated,
                Released = randomMovie.Released,
                Runtime = randomMovie.Runtime,
                Genre = randomMovie.Genre,
                Director = randomMovie.Director,
                Writer = randomMovie.Writer,
                Actors = randomMovie.Actors,
                Plot = randomMovie.Plot,
                Language = randomMovie.Language,
                Country = randomMovie.Country,
                Poster = randomMovie.Poster,
                ImdbRating = randomMovie.ImdbRating,
                ImdbID = randomMovie.ImdbID,
                Type = randomMovie.Type
            };

            var viewModel = new MovieDetailsViewModel
            {
                Movie = movie,
                UserRating = null
            };

            // If the user is authenticated, load their movie lists and check for existing rating
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);
                
                // Load user's movie lists
                var userLists = await _context.MovieLists
                    .AsNoTracking()
                    .Where(ml => ml.UserId == userId)
                    .OrderByDescending(ml => ml.CreatedAt)
                    .ToListAsync();

                ViewBag.UserMovieLists = userLists;

                // Check if user has already rated this movie
                var existingRating = await _context.MovieRatings
                    .FirstOrDefaultAsync(r => r.MovieId == movie.ImdbID && r.UserId == userId);

                if (existingRating != null)
                {
                    viewModel.UserRating = existingRating.Rating;
                }
            }

            return View("Details", viewModel);
        }

        public async Task<IActionResult> TopRated()
{
    // Get movies with their average ratings where at least one user has rated
    var topRatedMovies = await _context.MovieRatings
        .GroupBy(r => r.MovieId)
        .Select(g => new
        {
            MovieId = g.Key,
            AverageRating = g.Average(r => r.Rating),
            RatingCount = g.Count()
        })
        .OrderByDescending(x => x.AverageRating)
        .ThenByDescending(x => x.RatingCount) // More ratings will break ties
        .Take(100)
        .Join(_context.Movies,
            rating => rating.MovieId,
            movieStore => movieStore.ImdbID,
            (rating, movieStore) => new MovieRatingViewModel
            {
                Movie = new Movie
                {
                    Title = movieStore.Title,
                    Year = movieStore.Year,
                    Rated = movieStore.Rated,
                    Released = movieStore.Released,
                    Runtime = movieStore.Runtime,
                    Genre = movieStore.Genre,
                    Director = movieStore.Director,
                    Writer = movieStore.Writer,
                    Actors = movieStore.Actors,
                    Plot = movieStore.Plot,
                    Language = movieStore.Language,
                    Country = movieStore.Country,
                    Poster = movieStore.Poster,
                    ImdbRating = movieStore.ImdbRating,
                    ImdbID = movieStore.ImdbID,
                    Type = movieStore.Type
                },
                AverageRating = Math.Round(rating.AverageRating, 1),
                RatingCount = rating.RatingCount
            })
        .ToListAsync();

    var viewModel = new TopRatedMoviesViewModel
    {
        TopRatedMovies = topRatedMovies
    };

    return View(viewModel);
}
    }

    public class OmdbSearchResult
    {
        public List<Movie> Search { get; set; } = new List<Movie>();
        public string TotalResults { get; set; }
        public string Response { get; set; }
    }
}
