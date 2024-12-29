using Microsoft.AspNetCore.Identity;

namespace Clipser.Components.Models;
public class User : IdentityUser {
    public string ProfilePicture {get; set;} = "";
}