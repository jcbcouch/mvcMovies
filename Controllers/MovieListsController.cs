using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcMovies.Data;
using MvcMovies.Models;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System;

namespace MvcMovies.Controllers
{
    [Authorize]
    public class MovieListsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<MovieListsController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiKey;

        public MovieListsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<MovieListsController> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _apiKey = configuration["OmdbApi:ApiKey"];
        }

        // GET: /MovieLists/Create
        public IActionResult Create()
        {
            return View(new MovieListCreateViewModel());
        }

        // POST: /MovieLists/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovieListCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            var movieList = new MovieList
            {
                Title = model.Title,
                IsPublic = model.IsPublic,
                UserId = userId
            };

            _context.MovieLists.Add(movieList);
            await _context.SaveChangesAsync();

            // Redirect to home page
            return RedirectToAction("Index", "Home");
        }

        // GET: /MovieLists/MyLists - lists owned by the current user
        public async Task<IActionResult> MyLists()
        {
            var userId = _userManager.GetUserId(User);
            var lists = await _context.MovieLists
                .AsNoTracking()
                .Where(ml => ml.UserId == userId)
                .OrderByDescending(ml => ml.CreatedAt)
                .ToListAsync();

            return View(lists);
        }

        // GET: /MovieLists/Public - optional: show public lists from all users
        [AllowAnonymous]
        public async Task<IActionResult> Public()
        {
            var lists = await _context.MovieLists
                .AsNoTracking()
                .Where(ml => ml.IsPublic)
                .OrderByDescending(ml => ml.CreatedAt)
                .ToListAsync();

            return View(lists);
        }

        // POST: /MovieLists/AddItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(int movieListId, string imdbId)
        {
            if (string.IsNullOrWhiteSpace(imdbId))
            {
                return BadRequest();
            }

            var userId = _userManager.GetUserId(User);

            // Ensure the list exists and belongs to the current user
            var list = await _context.MovieLists
                .AsNoTracking()
                .FirstOrDefaultAsync(ml => ml.Id == movieListId && ml.UserId == userId);

            if (list == null)
            {
                return Forbid();
            }

            // Prevent duplicates
            var alreadyExists = await _context.MovieListItems
                .AsNoTracking()
                .AnyAsync(i => i.MovieListId == movieListId && i.ImdbID == imdbId);

            if (!alreadyExists)
            {
                _context.MovieListItems.Add(new MovieListItem
                {
                    MovieListId = movieListId,
                    ImdbID = imdbId
                });

                await _context.SaveChangesAsync();
                TempData["Message"] = "Movie added to your list.";
            }
            else
            {
                TempData["Message"] = "This movie is already in the selected list.";
            }

            // Redirect back to the movie details page
            return RedirectToAction("Details", "Movies", new { id = imdbId });
        }

        // GET: /MovieLists/Details/{id} - show all movies in a particular list
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var list = await _context.MovieLists.AsNoTracking().FirstOrDefaultAsync(ml => ml.Id == id);
            if (list == null)
            {
                return NotFound();
            }

            // Enforce visibility: show if public or owned by current user
            var currentUserId = _userManager.GetUserId(User);
            var canView = list.IsPublic || (User?.Identity?.IsAuthenticated == true && list.UserId == currentUserId);
            if (!canView)
            {
                return Forbid();
            }

            var items = await _context.MovieListItems
                .AsNoTracking()
                .Where(i => i.MovieListId == id)
                .ToListAsync();

            var movies = new List<Movie>();
            if (items.Count > 0)
            {
                var client = _httpClientFactory.CreateClient();
                foreach (var item in items)
                {
                    try
                    {
                        var resp = await client.GetAsync($"https://www.omdbapi.com/?i={item.ImdbID}&apikey={_apiKey}");
                        if (resp.IsSuccessStatusCode)
                        {
                            var content = await resp.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(content);
                            if (doc.RootElement.TryGetProperty("Response", out var respProp) && string.Equals(respProp.GetString(), "False", StringComparison.OrdinalIgnoreCase))
                            {
                                continue; // skip items OMDb did not find
                            }
                            var movie = JsonSerializer.Deserialize<Movie>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (movie != null)
                            {
                                movies.Add(movie);
                            }
                        }
                    }
                    catch
                    {
                        // swallow individual item errors to keep the page functional
                    }
                }
            }

            var vm = new MovieListDetailsViewModel
            {
                ListId = list.Id,
                Title = list.Title,
                IsPublic = list.IsPublic,
                Movies = movies
            };

            return View(vm);
        }
    }
}
