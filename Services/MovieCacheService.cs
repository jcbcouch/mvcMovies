using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MvcMovies.Data;
using MvcMovies.Models;

namespace MvcMovies.Services
{
    public interface IMovieCacheService
    {
        Task<Movie> GetOrAddMovieAsync(string imdbId);
    }

    public class MovieCacheService : IMovieCacheService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiKey;

        public MovieCacheService(
            ApplicationDbContext context, 
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _apiKey = configuration["OmdbApi:ApiKey"] ?? throw new ArgumentNullException("OmdbApi:ApiKey");
        }

        public async Task<Movie> GetOrAddMovieAsync(string imdbId)
        {
            if (string.IsNullOrWhiteSpace(imdbId))
                throw new ArgumentException("IMDb ID cannot be empty", nameof(imdbId));

            // Try to get the movie from cache first
            var cachedMovie = await GetMovieFromCacheAsync(imdbId);
            if (cachedMovie != null)
            {
                return MapToMovie(cachedMovie);
            }

            // If not in cache, fetch from API
            var movie = await FetchMovieFromApiAsync(imdbId);
            if (movie != null)
            {
                await AddMovieToCacheAsync(movie);
            }
            
            return movie ?? new Movie { Title = "Movie not found" };
        }

        private async Task<MovieStore> GetMovieFromCacheAsync(string imdbId)
        {
            return await _context.Movies
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ImdbID == imdbId);
        }

        private async Task AddMovieToCacheAsync(Movie movie)
        {
            var movieToCache = new MovieStore
            {
                ImdbID = movie.ImdbID,
                Title = movie.Title,
                Year = movie.Year,
                Rated = movie.Rated,
                Released = movie.Released,
                Runtime = movie.Runtime,
                Genre = movie.Genre,
                Director = movie.Director,
                Writer = movie.Writer,
                Actors = movie.Actors,
                Plot = movie.Plot,
                Language = movie.Language,
                Country = movie.Country,
                Poster = movie.Poster,
                ImdbRating = movie.ImdbRating,
                Type = movie.Type
            };

            _context.Movies.Add(movieToCache);
            await _context.SaveChangesAsync();
        }

        private async Task<Movie> FetchMovieFromApiAsync(string imdbId)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"https://www.omdbapi.com/?i={imdbId}&apikey={_apiKey}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Movie>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            
            return null;
        }

        private static Movie MapToMovie(MovieStore movieStore)
        {
            if (movieStore == null) return null;
            
            return new Movie
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
            };
        }
    }
}
