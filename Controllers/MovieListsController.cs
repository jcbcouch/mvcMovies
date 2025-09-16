using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvcMovies.Data;
using MvcMovies.Models;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace MvcMovies.Controllers
{
    [Authorize]
    public class MovieListsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<MovieListsController> _logger;

        public MovieListsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<MovieListsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
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
    }
}
