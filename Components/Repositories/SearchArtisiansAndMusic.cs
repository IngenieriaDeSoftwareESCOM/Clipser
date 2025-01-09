using Clipser.Data;
using Microsoft.AspNetCore.Identity;
using Clipser.Components.Models;
using SurrealDb.Net;
using Newtonsoft.Json;

namespace Clipser.Components.Repositories;
public class SearchArtisiansAndMusic{
    private readonly ISurrealDbClient _applicationDbContext;
    private readonly ILogger<SearchArtisiansAndMusic> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly string _apiKey;
    public SearchArtisiansAndMusic(ISurrealDbClient applicationDbContext, ILogger<SearchArtisiansAndMusic> logger, UserManager<ApplicationUser> userManager, string ApiKey)
    {
        _applicationDbContext = applicationDbContext;
        _logger = logger;
        _userManager = userManager;
        _apiKey = ApiKey;
    }
    public async Task<List<Artist2>> GetArtists(string textIn){
        try{
            string text = textIn.ToLower().Replace("-", "").Replace(".", "").Trim();
            List<Artist2> artists = new();
            bool ended = false;
            int start = 1;
            var result1 = await _applicationDbContext.Select<Artist2>("track");
            artists = result1.ToList();
            artists = artists.Where(p => 
                p.name.Contains(textIn) || p.adamid.Contains(textIn)
            ).ToList();
            return artists;
        }catch(Exception ex){
            _logger.LogWarning(ex.Message);
        }
        return new();
    }
    public async Task<List<Track>> GetTracks(string textIn){
        try{
            string text = textIn.ToLower().Replace("-", "").Replace(".", "").Trim();
            List<Track> tracks = new();
            bool ended = false;
            int start = 1;
            _logger.LogCritical("Trying to fetch data from db");
            var result1 = await _applicationDbContext.Select<Track>("track");
            tracks = result1.ToList();
            /*while(!ended){
                var result =  await _applicationDbContext.RawQuery($"SELECT * FROM track LIMIT 90 START {start}");
                var anyTrack = result.GetValue<List<Track>>(0);
                _logger.LogCritical($"List is null? {(anyTrack == null)} - {anyTrack?.Count}");
                start += 90;
                if(anyTrack?.Count > 0 && anyTrack != null) tracks.AddRange(anyTrack);
                else ended = true;
            }*/
            tracks = tracks.Where(p => 
                p.subtitle.Contains(textIn) ||
                p.key.Contains(textIn) ||
                p.title.Contains(textIn) ||
                p.share.subject.Contains(textIn) ||
                p.share.text.Contains(textIn) ||
                p.hub.providers.Any(x => x.type.Contains(textIn))
            ).ToList();
            return tracks;
        }catch(Exception ex){
            _logger.LogWarning(ex.Message);
        }
        return new();
    }
    public async Task<(List<Artist2>?, List<Track>?)> SearchByApi(string query){
        try{
            using HttpClient client = new();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://shazam.p.rapidapi.com/search?term={query}&offset=0&limit=5"),
                Headers =
                {
                    { "x-rapidapi-key", _apiKey },
                    { "x-rapidapi-host", "shazam.p.rapidapi.com" },
                },
            };
            var response = await client.SendAsync(request);
            if(response.IsSuccessStatusCode){
                try{
                    var raw = await response.Content.ReadAsStringAsync();
                    _logger.LogCritical(raw);
                    /*var data = JsonConvert.DeserializeObject<Root>(raw);
                    List<Artist2> artists = new();
                    List<Track> tracks = new();
                    if(data?.artists?.artists?.Count > 0){
                        foreach(var t in data.artists.artists){
                            foreach(var hit in t.hits){
                                artists.Add(hit.artist);
                            }
                        }
                        await _applicationDbContext.Insert("artist", artists);
                    }
                    if(data?.tracks?.hits?.Count > 0){
                        
                        foreach(var t in data.tracks.hits){
                            var x = t.track;
                            tracks.Add(x);
                        }
                        await _applicationDbContext.Insert("track", tracks);
                    }
                    if(artists == null && tracks == null){
                        _logger.LogWarning("The search return null");
                    }
                    return (artists, tracks);*/
                }catch(Exception ex){
                    _logger.LogCritical(ex.Message);
                }
            }
            _logger.LogWarning("Something fail while fetching data");
        }catch(Exception ex){
            _logger.LogWarning(ex.Message);
        }
        return new();
    }
    public async Task<bool> AddTrackToFavorite(Track track, string UserEmail){
        try{
            var user = await _userManager.FindByEmailAsync(UserEmail);
            if(user != null){
                var result = await _applicationDbContext.RawQuery($"SELECT * FROM bookFavorite WHERE id = ⟨{track.key}{user.Id}⟩ LIMIT 1");
                var response = result.GetValue<List<Track>>(0);
                if(response?.Count == 0 && user != null){
                    user.PasswordHash = "";
                    Favorite fav = new(){
                        User = user,
                        Track = track
                    };
                    fav.Id = ("bookFavorite", $"{track.key}{user.Id}");
                    await _applicationDbContext.Create(fav);
                }else{
                    _logger.LogWarning("Track already added to favorites");
                }
            }else{
                _logger.LogWarning("User not found");
            }
        }catch(Exception ex){
            _logger.LogError(ex.Message);
        }
        return false;
    }
    public async Task<bool> RemoveTrackFromFavorite(Favorite favorite){
        try{
            if(favorite != null){
                _logger.LogInformation("Removing from favorites");
                await _applicationDbContext.RawQuery($"DELETE FROM bookFavorite WHERE id = bookFavorite:⟨{favorite.Track?.key}{favorite.User.Id}⟩");
            }
        }catch(Exception ex){
            _logger.LogError(ex.Message);
        }
        return false;
    }
    public async Task<bool> AddArtistToFavorite(Artist2 artist, string UserEmail){
        try{
            var user = await _userManager.FindByEmailAsync(UserEmail);
            if(user != null){
                var result = await _applicationDbContext.RawQuery($"SELECT * FROM bookFavorite WHERE id = ⟨{artist.adamid}{user.Id}⟩ LIMIT 1");
                var response = result.GetValue<List<Artist2>>(0);
                if(response?.Count == 0 && user != null){
                    user.PasswordHash = "";
                    Favorite fav = new(){
                        User = user,
                        Artist = artist
                    };
                    fav.Id = ("bookFavorite", $"{artist.adamid}{user.Id}");
                    await _applicationDbContext.Create(fav);
                }else{
                    _logger.LogWarning("Artist already added to favorites");
                }
            }else{
                _logger.LogWarning("User not found");
            }
        }catch(Exception ex){
            _logger.LogError(ex.Message);
        }
        return false;
    }
    public async Task<bool> RemoveArtistFromFavorite(Favorite favorite){
        try{
            if(favorite != null){
                _logger.LogInformation("Removing from favorites");
                await _applicationDbContext.RawQuery($"DELETE FROM bookFavorite WHERE id = bookFavorite:⟨{favorite.Artist?.adamid}{favorite.User.Id}⟩");
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