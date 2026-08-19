using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PlayerCards.Entities;
using PlayerCards.Data;
using PlayerCards.Services;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Razor;
using PlayerCards.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Languages");

// Add MVC with localization
builder.Services
    .AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("en"),
        new CultureInfo("ar")
    };
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;

    // Cookie first, then QueryString (both enabled)
    options.RequestCultureProviders.Clear();
    options.RequestCultureProviders.Add(new CookieRequestCultureProvider());
    options.RequestCultureProviders.Add(new QueryStringRequestCultureProvider());
});

// Database connection
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("PlayerCardsDb"));

// Email
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter;
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToReturnUrl = context =>
            {
                if (string.IsNullOrEmpty(context.RedirectUri) || context.RedirectUri.Equals("/", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Redirect("/");
                }
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

app.UseRequestLocalization();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseFileValidation(); // Custom middleware to validate uploaded files
app.UseRouting();

app.UseSession();

app.UseAuthentication();

// DEMO MODE: auto-sign every visitor in as SuperAdmin so the portfolio app
// opens straight on the dashboard without a login page.
// NOTE: This MUST run AFTER UseAuthentication (which would otherwise reset
// context.User to anonymous) and BEFORE UseAuthorization.
app.Use(async (context, next) =>
{
    // Force full-access demo identity for anyone who isn't already a SuperAdmin
    // (also upgrades stale guest cookies). Grants SuperAdmin + Admin + User roles.
    if (!context.User.IsInRole("SuperAdmin"))
    {
        // Look up the seeded SuperAdmin so we use its REAL id in the NameIdentifier
        // claim. Controllers like HomeController read this id and redirect to Login
        // if it is 0, so hardcoding "0" would break the cards/home/cart pages.
        var db = context.RequestServices.GetRequiredService<AppDbContext>();
        var admin = db.UserAccounts.FirstOrDefault(u => u.Role == "SuperAdmin");
        var adminId = admin?.Id ?? 0;
        var adminEmail = admin?.Email ?? "super@admin.com";
        var adminName = admin?.FirstName ?? "Root";

        // Keep the session UserId in sync too (some actions read it from session).
        context.Session.SetInt32("UserId", adminId);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, adminId.ToString()),
            new Claim(ClaimTypes.Name, adminEmail),
            new Claim("Name", adminName),
            // Grant every role so all [Authorize(Roles=...)] pages work (dashboard + cards/home).
            new Claim(ClaimTypes.Role, "SuperAdmin"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "User"),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        context.User = new ClaimsPrincipal(identity);
    }
    await next();
});

app.UseAuthorization();

// Default route -> open the SuperAdmin dashboard directly
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=SuperAdmin}/{action=Dashboard}/{id?}");

// Ensure at least one SuperAdmin exists
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();

    if (!context.UserAccounts.Any(u => u.Role == "SuperAdmin"))
    {
        context.UserAccounts.Add(new UserAccount
        {
            FirstName = "Root",
            LastName = "Admin",
            Email = "super@admin.com",
            UserName = "superadmin",
            Password = "superpass",
            Role = "SuperAdmin",
            IsActive = true
        });
        context.SaveChanges();
    }
}

app.Run();
