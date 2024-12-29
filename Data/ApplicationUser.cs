using Microsoft.AspNetCore.Identity;

namespace Clipser.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public string ProfilePicture {get; set;} = "";
}

