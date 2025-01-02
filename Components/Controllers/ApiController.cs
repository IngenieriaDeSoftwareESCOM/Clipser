using System.Security.Claims;
using Clipser.Components.Repositories;
using Clipser.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurrealDb.Net;

namespace Clipser.Components.Controllers;
[ApiController]
[Route("/api")]
[Authorize]
public class ApiController : ControllerBase{
    private readonly ISurrealDbClient _surrealDbClient;
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly SearchBook _searchBook;
    private readonly SearchMovie _searchMovie;
    private readonly ClaimsPrincipal? _userClaim;
    private readonly ILogger<ApiController> _logger;
    private readonly string _idClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";

    private ApplicationUser? _user;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public ApiController(ISurrealDbClient surrealDbClient, ApplicationDbContext applicationDbContext, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager
    , SearchBook searchBook, IHttpContextAccessor httpContextAccessor, SearchMovie searchMovie, ILogger<ApiController> logger)
    {
        _applicationDbContext = applicationDbContext;
        _surrealDbClient = surrealDbClient;
        _logger = logger;
        _userManager = userManager;
        _signInManager = signInManager;
        _searchBook = searchBook;
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        var httpContext = _httpContextAccessor.HttpContext;
        _userClaim = httpContext?.User;
        _searchMovie = searchMovie;
    }
    private string GetCurrentUserId(){
        try{
            if(_user != null){
                return _user.Id;
            }else{
                var claims = _userClaim?.Claims;
                if(claims != null){
                    var claim = claims.AsEnumerable().FirstOrDefault(p => p.Type.Equals(_idClaimType));
                    if(claim != null){
                        string? userId = claim.Value;
                        return userId ?? string.Empty;
                    }
                }
            }
        }catch(Exception ex){
        }
        return string.Empty;
    }
    [HttpPost]
    [Route("Login")]
    public async Task<IActionResult> LogIn(string username, string password){
        try{
            var user = await _userManager.FindByEmailAsync(username);
            if(user == null) return BadRequest("User or password wrong");
            var result = await _signInManager.PasswordSignInAsync(user, password, true, false);
            if(result.Succeeded){
                return Ok();
            }else{
                return BadRequest("Something fail, try again");
            }
        }catch(Exception ex){
            return StatusCode(500);
        }
    }
    [HttpGet]
    [Route("Get/Books")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBooks(string query){
        try{
            var books = await _searchBook.GetBooks(query);
            return Ok(books);
        }catch(Exception ex){
            return StatusCode(500);
        }
    }
    [HttpGet]
    [Route("Get/Favorite/Books")]
    public async Task<IActionResult> GetFavoriteBooks(){
        try{
            var userId = GetCurrentUserId();
            if(String.IsNullOrEmpty(userId)) return BadRequest("Id not valid");
            var user = await _userManager.FindByIdAsync(userId);
            if(user == null) return BadRequest("User not found");
            var favorites = await _searchBook.GetFavoritesAsync(user.Email ?? "");
            return Ok(favorites.Where(p => p != null && p.Book != null).Select(p => p.Book).ToList());
        }catch(Exception ex){
            return StatusCode(500);
        }
    }
    [HttpGet]
    [Route("Get/Movies")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMovies(string query){
        try{
            var movies = await _searchMovie.GetMovies(query);
            return Ok(movies);
        }catch(Exception ex){
            return StatusCode(500);
        }
    }
    [HttpGet]
    [Route("Get/Favorite/Movies")]
    public async Task<IActionResult> GetFavoriteMovies(){
        try{
            var userId = GetCurrentUserId();
            if(String.IsNullOrEmpty(userId)) return BadRequest("Id not valid");
            var user = await _userManager.FindByIdAsync(userId);
            if(user == null) return BadRequest("User not found");
            var favorites = await _searchMovie.GetFavoritesAsync(user.Email ?? "");
            return Ok(favorites.Where(p => p != null && p.Movie != null).Select(p => p.Movie).ToList());
        }catch(Exception ex){
            return StatusCode(500);
        }
    }
}