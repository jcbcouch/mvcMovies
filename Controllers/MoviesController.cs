using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MvcMovies.Models;
using MvcMovies.Data;
using MvcMovies.Services;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Net.Http;

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

            // If the user is authenticated, load their movie lists and pass to the view
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);
                var userLists = await _context.MovieLists
                    .AsNoTracking()
                    .Where(ml => ml.UserId == userId)
                    .OrderByDescending(ml => ml.CreatedAt)
                    .ToListAsync();

                ViewBag.UserMovieLists = userLists;
            }

            return View(movie);
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

            // If the user is authenticated, load their movie lists and pass to the view
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);
                var userLists = await _context.MovieLists
                    .AsNoTracking()
                    .Where(ml => ml.UserId == userId)
                    .OrderByDescending(ml => ml.CreatedAt)
                    .ToListAsync();

                ViewBag.UserMovieLists = userLists;
            }

            return View("Details", movie);
        }
    }

    public class OmdbSearchResult
    {
        public List<Movie> Search { get; set; } = new List<Movie>();
        public string TotalResults { get; set; }
        public string Response { get; set; }
    }
}
