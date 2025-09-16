using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MvcMovies.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MvcMovies.Controllers
{
    public class MoviesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiKey;

        public MoviesController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _apiKey = configuration["OmdbApi:ApiKey"];
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
                    model.SearchResults = searchResult.Search;
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

            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"https://www.omdbapi.com/?i={id}&apikey={_apiKey}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var movie = JsonSerializer.Deserialize<Movie>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (movie != null)
                {
                    return View(movie);
                }
            }
            
            return NotFound();
        }
    }

    public class OmdbSearchResult
    {
        public List<Movie> Search { get; set; } = new List<Movie>();
        public string TotalResults { get; set; }
        public string Response { get; set; }
    }
}
