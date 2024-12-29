using Clipser.Components.Models;
using SurrealDb.Net;
using System.Linq;

namespace Clipser.Components.Repositories;

public class Recommendations 
{
    private readonly ISurrealDbClient _applicationDbContext;
    private const int MAX_RECOMMENDATIONS = 10;
    
    public Recommendations(ISurrealDbClient applicationDbContext) 
    {
        _applicationDbContext = applicationDbContext;
    }

    public async Task<List<Book>> GetBookRecommendations(string userName)
    {
        // Get user preferences
        string query = $@"SELECT COUNT() AS Count, Book.subject AS Genre 
            FROM (SELECT * FROM bookFavorite SPLIT Book.subject) 
            WHERE User.UserName = '{userName}' 
                AND Book.subject IS NOT NONE 
                AND Book.subject IS NOT NULL 
            GROUP BY Genre;
            
            SELECT COUNT() AS Count, Book.author_alternative_name AS AuthorName 
            FROM (SELECT * FROM bookFavorite SPLIT Book.author_alternative_name) 
            WHERE User.UserName = '{userName}' 
                AND Book.author_alternative_name IS NOT NONE 
                AND Book.author_alternative_name IS NOT NULL 
            GROUP BY AuthorName;
            
            SELECT COUNT() AS Count, Book.language AS Language 
            FROM (SELECT * FROM bookFavorite SPLIT Book.language) 
            WHERE User.UserName = '{userName}' 
            GROUP BY Language";

        var response = await _applicationDbContext.RawQuery(query);
        var genrePreferences = response.GetValue<List<GenreRecomendation>>(0);
        var authorPreferences = response.GetValue<List<Author>>(1);
        var languagePreferences = response.GetValue<List<LanguageRecomendation>>(2);

        // Get all books
        List<Book> allBooks = await GetAllBooks();
        
        // Score each book
        var scoredBooks = allBooks.Select(book => new
        {
            Book = book,
            Score = CalculateBookScore(book, genrePreferences ?? new(), authorPreferences ?? new(), languagePreferences ?? new())
        })
        .OrderByDescending(x => x.Score)
        .Take(MAX_RECOMMENDATIONS)
        .Select(x => x.Book)
        .ToList();

        return scoredBooks;
    }

    public async Task<List<Movie>> GetMovieRecommendations(string userName)
    {
        string query = $@"SELECT COUNT() AS Count, Movie.Genres AS Genre 
            FROM (SELECT * FROM movieFavorite SPLIT Movie.Genres)
            WHERE User.UserName = '{userName}' 
            GROUP BY Genre;
            
            SELECT COUNT() AS Count, Movie.Network.Country.Name AS Country 
            FROM movieFavorite 
            WHERE User.UserName = '{userName}' 
            GROUP BY Country;
            
            SELECT COUNT() AS Count, Movie.Language AS Language 
            FROM movieFavorite 
            WHERE User.UserName = '{userName}' 
            GROUP BY Language";

        var response = await _applicationDbContext.RawQuery(query);
        var genrePreferences = response.GetValue<List<GenreRecomendation>>(0);
        var countryPreferences = response.GetValue<List<CountryRecomendation>>(1);
        var languagePreferences = response.GetValue<List<LanguageRecomendation>>(2);

        // Get all movies
        List<Movie> allMovies = await GetAllMovies();

        // Score each movie
        var scoredMovies = allMovies.Select(movie => new
        {
            Movie = movie,
            Score = CalculateMovieScore(movie, genrePreferences ?? new(), countryPreferences ?? new(), languagePreferences ?? new())
        })
        .OrderByDescending(x => x.Score)
        .Take(MAX_RECOMMENDATIONS)
        .Select(x => x.Movie)
        .ToList();

        return scoredMovies;
    }

    private double CalculateBookScore(Book book, 
        List<GenreRecomendation> genrePreferences, 
        List<Author> authorPreferences,
        List<LanguageRecomendation> languagePreferences)
    {
        double score = 0;

        // Base score from ratings
        score += (book.ratings_average * book.ratings_count) / 100.0;

        // Genre matching (40% weight)
        if (book.subject != null && genrePreferences != null)
        {
            foreach (var subject in book.subject)
            {
                var matchingGenre = genrePreferences?.FirstOrDefault(g => g.Genre?.Equals(subject ?? "", StringComparison.OrdinalIgnoreCase) ?? false);
                if (matchingGenre != null)
                {
                    score += (matchingGenre.Count * 0.4);
                }
            }
        }

        // Author matching (30% weight)
        if (book.author_alternative_name != null && authorPreferences != null)
        {
            foreach (var author in book.author_alternative_name)
            {
                var matchingAuthor = authorPreferences?.FirstOrDefault(a => a.AuthorName?.Equals(author ?? "", StringComparison.OrdinalIgnoreCase) ?? false);
                if (matchingAuthor != null)
                {
                    score += (matchingAuthor.Count * 0.3);
                }
            }
        }

        // Language matching (20% weight)
        if (book.language != null && languagePreferences != null)
        {
            foreach (var language in book.language)
            {
                var matchingLanguage = languagePreferences?.FirstOrDefault(l => l.Language?.Equals(language ?? "", StringComparison.OrdinalIgnoreCase) ?? false);
                if (matchingLanguage != null)
                {
                    score += (matchingLanguage.Count * 0.2);
                }
            }
        }

        // Popularity boost (10% weight)
        score += (book.want_to_read_count + book.currently_reading_count) * 0.1;

        return score;
    }

    private double CalculateMovieScore(Movie movie,
        List<GenreRecomendation> genrePreferences,
        List<CountryRecomendation> countryPreferences,
        List<LanguageRecomendation> languagePreferences)
    {
        double score = 0;

        // Base score from rating
        if (movie.Rating?.Average != null)
        {
            score += movie.Rating.Average.Value;
        }

        // Genre matching (40% weight)
        if (movie.Genres != null && genrePreferences != null)
        {
            foreach (var genre in movie.Genres)
            {
                var matchingGenre = genrePreferences?.FirstOrDefault(g => g.Genre?.Equals(genre ?? "", StringComparison.OrdinalIgnoreCase) ?? false);
                if (matchingGenre != null)
                {
                    score += (matchingGenre.Count * 0.4);
                }
            }
        }

        // Country matching (30% weight)
        if (movie.Network?.Country?.Name != null && countryPreferences != null)
        {
            var matchingCountry = countryPreferences?.FirstOrDefault(c => 
                c.Country?.Equals(movie.Network.Country.Name ?? "", StringComparison.OrdinalIgnoreCase) ?? false);
            if (matchingCountry != null)
            {
                score += (matchingCountry.Count * 0.3);
            }
        }

        // Language matching (20% weight)
        if (movie.Language != null && languagePreferences != null)
        {
            var matchingLanguage = languagePreferences?.FirstOrDefault(l => 
                l.Language?.Equals(movie.Language ?? "", StringComparison.OrdinalIgnoreCase) ?? false);
            if (matchingLanguage != null)
            {
                score += (matchingLanguage.Count * 0.2);
            }
        }

        // Weight/Popularity boost (10% weight)
        score += movie.Weight * 0.1;

        return score;
    }

    private async Task<List<Book>> GetAllBooks()
    {
        List<Book> books = new();
        bool ended = false;
        int start = 1;
        
        while (!ended)
        {
            var result = await _applicationDbContext.RawQuery($"SELECT * FROM book LIMIT 90 START {start}");
            var batchBooks = result.GetValue<List<Book>>(0);
            start += 90;
            
            if (batchBooks?.Count > 0)
                books.AddRange(batchBooks);
            else
                ended = true;
        }
        
        return books;
    }

    private async Task<List<Movie>> GetAllMovies()
    {
        List<Movie> movies = new();
        bool ended = false;
        int start = 1;
        
        while (!ended)
        {
            var result = await _applicationDbContext.RawQuery($"SELECT * FROM movie LIMIT 90 START {start}");
            var batchMovies = result.GetValue<List<MovieRecord>>(0);
            start += 90;
            
            if (batchMovies?.Count > 0)
                movies.AddRange(batchMovies.Select(p => p.movie));
            else
                ended = true;
        }
        
        return movies;
    }
}
public class Recomendation {
    public int Count { get; set; }
    public int Weight { get; set;}
}
public class Author : Recomendation {
    public string AuthorName { get; set; }
}
public class CountryRecomendation : Recomendation {
    public string Country {get; set;}
}
public class GenreRecomendation : Recomendation {
    public string Genre { get; set; }
}
public class LanguageRecomendation : Recomendation {
    public string Language {get; set;}
}