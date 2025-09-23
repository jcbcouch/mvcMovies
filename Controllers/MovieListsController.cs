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
using MvcMovies.Services;

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
        private readonly IMovieCacheService _movieCache;

        public MovieListsController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager, 
            ILogger<MovieListsController> logger, 
            IHttpClientFactory httpClientFactory, 
            IConfiguration configuration,
            IMovieCacheService movieCache)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _apiKey = configuration["OmdbApi:ApiKey"];
            _movieCache = movieCache;
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

        // GET: /MovieLists/Public - show public lists from all users with item counts and user info
        [AllowAnonymous]
        public async Task<IActionResult> Public()
        {
            var lists = await _context.MovieLists
                .AsNoTracking()
                .Where(ml => ml.IsPublic)
                .Include(ml => ml.User)  // Include the User navigation property
                .Include(ml => ml.MovieListItems) // Include the MovieListItems to count them
                .OrderByDescending(ml => ml.CreatedAt)
                .Select(ml => new 
                {
                    ml.Id,
                    ml.Title,
                    ml.IsPublic,
                    ml.CreatedAt,
                    ml.UserId,
                    UserName = ml.User.UserName,
                    ItemCount = ml.MovieListItems.Count
                })
                .ToListAsync();

            // Map to the view model
            var viewModel = lists.Select(l => new MovieList
            {
                Id = l.Id,
                Title = l.Title,
                IsPublic = l.IsPublic,
                CreatedAt = l.CreatedAt,
                UserId = l.UserId,
                User = new ApplicationUser { UserName = l.UserName },
                MovieListItems = new List<MovieListItem>(new MovieListItem[l.ItemCount])
            }).ToList();

            return View(viewModel);
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
        public async Task<IActionResult> Details(int id, string referrer = "mylists")
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
                foreach (var item in items)
                {
                    var movie = await _movieCache.GetOrAddMovieAsync(item.ImdbID);
                    if (movie != null)
                    {
                        movies.Add(movie);
                    }
                }
            }

            var vm = new MovieListDetailsViewModel
            {
                ListId = list.Id,
                Title = list.Title,
                IsPublic = list.IsPublic,
                Movies = movies,
                Referrer = referrer
            };

            return View(vm);
        }
    }
}
