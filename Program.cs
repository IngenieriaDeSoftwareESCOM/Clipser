using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Clipser.Components;
using Clipser.Components.Account;
using Clipser.Data;
using Clipser.Components.Models;
using Clipser.Components.Repositories;
using SurrealDb.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();
builder.Services.AddAuthentication()
   .AddGoogle(options =>
   {
       IConfigurationSection googleAuthNSection =
       builder.Configuration.GetSection("Authentication:Google");
       options.ClientId = builder.Configuration["ExternalAuth:Google:ClientId"];
       options.ClientSecret = builder.Configuration["ExternalAuth:Google:ClientSecret"];
   }).AddMicrosoftAccount(microsoftOptions =>
   {
       microsoftOptions.ClientId = builder.Configuration["ExternalAuth:Microsoft:ClientId"];
       microsoftOptions.ClientSecret = builder.Configuration["ExternalAuth:Microsoft:ClientSecret"];
   });

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => {
        options.SignIn.RequireConfirmedAccount = true;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<SearchBook>((provider) => {
    var context = provider.GetRequiredService<ISurrealDbClient>();
    var logger = provider.GetRequiredService<ILogger<SearchBook>>();
    var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
    return new(context, logger, userManager);
});
builder.Services.AddScoped<SearchMovie>((provider) => {
    var context = provider.GetRequiredService<ISurrealDbClient>();
    var logger = provider.GetRequiredService<ILogger<SearchMovie>>();
    var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
    return new(context, logger, userManager);
});
builder.Services.AddScoped<SearchArtisiansAndMusic>((provider) => {
    var context = provider.GetRequiredService<ISurrealDbClient>();
    var logger = provider.GetRequiredService<ILogger<SearchArtisiansAndMusic>>();
    var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
    string apiKey = builder.Configuration["ExternalAuth:Shazam:ApiKey"] ?? "";
    return new(context, logger, userManager, apiKey);
});
builder.Services.AddScoped<Recommendations>((provider) => {
    var context = provider.GetRequiredService<ISurrealDbClient>();
    return new(context);
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddSurreal(builder.Configuration.GetConnectionString("SurrealDB") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."));

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5000); // Listen on all IPs
});

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();
using var scope = app.Services.CreateScope();
using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
try{
    dbContext.Database.Migrate();
}catch(Exception ex){
    
}

dbContext.Dispose();
scope.Dispose();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.UseStaticFiles();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
