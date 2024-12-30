using Clipser.Components.Models;
using Clipser.Data;
using Microsoft.AspNetCore.Identity;
using SurrealDb.Net;
using System.Text.Json;


namespace Clipser.Components.Repositories;
public class SearchMovie{
    private readonly ISurrealDbClient _applicationDbContext;
    private readonly ILogger<SearchMovie> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    public SearchMovie(ISurrealDbClient applicationDbContext, ILogger<SearchMovie> logger, UserManager<ApplicationUser> userManager)
    {
        _applicationDbContext = applicationDbContext;
        _logger = logger;
        _userManager = userManager;
    }
    public async Task<List<Movie>> GetMovies(string textIn){
        try{
            string text = textIn.ToLower();
            List<Movie> movies = new();
            int start = 1;
            bool ended = false;
            while(!ended){
                var result =  await _applicationDbContext.RawQuery($"SELECT * FROM movie LIMIT 90 START {start}");
                var anyMovie = result.GetValue<List<MovieRecord>>(0);
                start += 90;
                if(anyMovie?.Count > 0) movies.AddRange(anyMovie.Select(p => p.movie));
                else ended = true;
            }
            movies = movies.Where(p =>
                p.Name.ToLower().Contains(textIn) ||
                p.Type.ToLower().Contains(textIn) ||
                p.Language.ToLower().Contains(textIn) ||
                p.Premiered.ToLower().Contains(textIn) ||   
                p.Summary.ToLower().Contains(textIn) ||
                p.Externals.Imdb.ToLower().Contains(textIn)
            ).ToList();
            return movies;
        }catch(Exception ex){
            _logger.LogError(ex.Message);
        }
        return new();
    }
    public async Task<List<Movie>> SearchMovieByApi(string query){
        try{
            using HttpClient client = new();
            query = query.Contains(" ") ? query.Replace(" ", "+") : query;
            // Deserialize with case-insensitive settings
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // This makes the property matching case-insensitive
            };
            var response = await client.GetAsync($"https://api.tvmaze.com/search/shows?q={query}");
            if(response.IsSuccessStatusCode){
                var raw = await response.Content.ReadAsStringAsync();
                var movies = JsonSerializer.Deserialize<List<MovieRoot>>(raw, options);
                if(movies?.Count > 0){
                    _logger.LogInformation($"Movies fetched {movies.Count}");
                    //List<MovieRecord> moviesReturn = movies.Where(p => p != null).Select(p => new MovieRecord(){ movie = p.Show }).ToList();
                    //moviesReturn.ForEach(p => p.Id = ("movie", p.movie.Id));
                    //await _applicationDbContext.Create(moviesReturn.First());
                    List<Movie> moviesReturn = movies.Select(p => p.Show).ToList();
                    movies.ForEach(async (p) => await RegisterMovie(p.Show));
                    return moviesReturn;
                }
                _logger.LogWarning("The search return null");
            }
            _logger.LogWarning("Something fail while fetching data");
        }catch(Exception ex){
            _logger.LogWarning(ex.Message);
        }
        return new();
    }
    public async Task<bool> RegisterMovie(Movie movie){
        try{
            MovieRecord record = new(){
                movie = movie
            };
            record.Id = ("movie", movie.Id);
            string query = $"SELECT id FROM movie WHERE id = movie:{movie.Id} LIMIT 1";
            var result =  await _applicationDbContext.RawQuery(query);
            List<MovieRecord>? anyMovie = result.GetValue<List<MovieRecord>>(0) ;
            if(anyMovie?.Count == 0){
                record.Id = ("movie", movie.Id);
                await _applicationDbContext.Create(record);
            }else{
                record.Id = ("movie", record.movie.Id);
                await _applicationDbContext.Update(record);
            }
            return true;
        }catch(Exception ex){
            _logger.LogError(ex.Message);
        }
        return false;
    }
    public async Task<bool> AddToFavorite(Movie movie, string UserEmail){
        try{
            var user = await _userManager.FindByEmailAsync(UserEmail);
            if(user != null){
                var result = await _applicationDbContext.RawQuery($"SELECT * FROM movieFavorite WHERE id = movieFavorite:⟨{movie.Id}{user.Id}⟩ LIMIT 1");
                var response = result.GetValue<List<Favorite>>(0);
                if(response?.Count == 0 && user != null){
                    user.PasswordHash = "";
                    Favorite fav = new(){
                        User = user,
                        Movie = movie
                    };
                    fav.Id = ("movieFavorite", $"{movie.Id}{user.Id}");
                    await _applicationDbContext.Create(fav);
                }else{
                    _logger.LogWarning("Book already added to favorites");
                }
            }else{
                _logger.LogWarning("User not found");
            }
        }catch(Exception ex){
            _logger.LogError(ex.Message);
        }
        return false;
    }
    public async Task<bool> RemoveFromFavorite(Favorite favorite){
        try{
            if(favorite != null){
                _logger.LogInformation("Removing from favorites");
                await _applicationDbContext.RawQuery($"DELETE FROM movieFavorite WHERE id = movieFavorite:⟨{favorite.Movie?.Id}{favorite.User.Id}⟩");
            }
        }catch(Exception ex){
            _logger.LogError(ex.Message);
        }
        return false;
    }
    public async Task<List<Favorite>> GetFavoritesAsync(string UserId){
        try{
            var response = await _applicationDbContext.Select<Favorite>("movieFavorite");
            response = response.Where(p => p.User.Id.Equals(UserId));
            return response.ToList();
        }catch(Exception ex){
            _logger.LogError(ex.Message);
        }
        return new();
    }
}