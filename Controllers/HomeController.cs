using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MvcMovies.Models;
using MvcMovies.Services;

namespace MvcMovies.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMovieCacheService _movieCache;

        public HomeController(IMovieCacheService movieCache)
        {
            _movieCache = movieCache;
        }

        public async Task<IActionResult> Index()
        {
            const string defaultMovieId = "tt28996126";
            var movie = await _movieCache.GetOrAddMovieAsync(defaultMovieId);
            return View(movie);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
