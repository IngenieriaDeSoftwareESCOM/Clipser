using Clipser.Components.Models;
using SurrealDb.Net;
using Newtonsoft.Json;
using Clipser.Data;
using Microsoft.AspNetCore.Identity;


namespace Clipser.Components.Repositories;
public class SearchBook{
    private readonly ISurrealDbClient _applicationDbContext;
    private readonly ILogger<SearchBook> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    public SearchBook(ISurrealDbClient applicationDbContext, ILogger<SearchBook> logger, UserManager<ApplicationUser> userManager)
    {
        _applicationDbContext = applicationDbContext;
        _logger = logger;
        _userManager = userManager;
    }
    public async Task<List<Book>> GetBooks(string textIn){
        try{
            string text = textIn.ToLower().Replace("-", "").Replace(".", "").Trim();
            List<Book> books = new();
            bool ended = false;
            int start = 1;
            while(!ended){
                var result =  await _applicationDbContext.RawQuery($"SELECT * FROM book LIMIT 90 START {start}");
                var anybook = result.GetValue<List<Book>>(0);
                start += 90;
                if(anybook?.Count > 0) books.AddRange(anybook);
                else ended = true;
            }
            books = books.Where(p => 
                (p.author_name != null && p.author_name.Any(x => x != null && x.ToLower().Contains(text))) || 
                (!string.IsNullOrEmpty(p.title) && p.title.ToLower().Contains(text)) || 
                (!string.IsNullOrEmpty(p.title_sort) && p.title_sort.ToLower().Contains(text)) || 
                (!string.IsNullOrEmpty(p.title_suggest) && p.title_suggest.ToLower().Contains(text)) ||
                (p.author_key != null && p.author_key.Any(x => x != null && x.ToLower().Contains(text))) ||
                (p.edition_key != null && p.edition_key.Any(x => x != null && x.ToLower().Contains(text))) ||
                (!string.IsNullOrEmpty(p.key) && p.key.ToLower().Contains(text)) ||
                (p.publisher != null && p.publisher.Any(x => x != null && x.ToLower().Contains(text))) ||
                (!string.IsNullOrEmpty(p.subtitle) && p.subtitle.ToLower().Contains(text))
            ).ToList();
            return books;
        }catch(Exception ex){
            _logger.LogWarning(ex.Message);
        }
        return new();
    }
    public async Task<List<Book>> SearchBookByApi(string query){
        try{
            using HttpClient client = new();
            query = query.Contains(" ") ? query.Replace(" ", "+") : query;
            var response = await client.GetAsync($"https://openlibrary.org/search.json?title={query}");
            if(response.IsSuccessStatusCode){
                var raw = await response.Content.ReadAsStringAsync();
                var books = JsonConvert.DeserializeObject<RootBook>(raw);
                if(books?.docs != null){
                    books.docs.ForEach(p => p.Id = ("book", p.key));
                    await _applicationDbContext.Insert("book", books.docs);
                    return books.docs;
                }
                _logger.LogWarning("The search return null");
            }
            _logger.LogWarning("Something fail while fetching data");
        }catch(Exception ex){
            _logger.LogWarning(ex.Message);
        }
        return new();
    }
    public async Task<bool> RegisterBook(Book book){
        try{
            var result =  await _applicationDbContext.RawQuery($"SELECT id FROM book WHERE id = book:⟨{book.key}⟩ LIMIT 1");
            var anybook = result.GetValue<List<Book>>(0);
            if(anybook == null && anybook?.Count == 0){
                book.Id = ("book", book.key);
                await _applicationDbContext.Create(book);
            }else{
                book.Id = ("book", book.key);
                await _applicationDbContext.Update(book);
            }
            return true;
        }catch(Exception ex){
            _logger.LogError(ex.Message);
        }
        return false;
    }
    public async Task<bool> AddToFavorite(Book book, string UserEmail){
        try{
            var user = await _userManager.FindByEmailAsync(UserEmail);
            if(user != null){
                var result = await _applicationDbContext.RawQuery($"SELECT * FROM bookFavorite WHERE id = bookFavorite:⟨{book.key}{user.Id}⟩ LIMIT 1");
                var response = result.GetValue<List<Favorite>>(0);
                if(response?.Count == 0 && user != null){
                    user.PasswordHash = "";
                    Favorite fav = new(){
                        User = user,
                        Book = book
                    };
                    fav.Id = ("bookFavorite", $"{book.key}{user.Id}");
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
                await _applicationDbContext.RawQuery($"DELETE FROM bookFavorite WHERE id = bookFavorite:⟨{favorite.Book?.key}{favorite.User.Id}⟩");
            }
        }catch(Exception ex){
            _logger.LogError(ex.Message);
        }
        return false;
    }
    public async Task<List<Favorite>> GetFavoritesAsync(string UserId){
        try{
            var response = await _applicationDbContext.Select<Favorite>("bookFavorite");
            response = response.Where(p => p.User.Id.Equals(UserId));
            return response.ToList();
        }catch(Exception ex){
            _logger.LogError(ex.Message);
        }
        return new();
    }
}