using CipherVault.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CipherVault.Repositories;
using CipherVault.Repositories.Contracts;
using CipherVault.Services;
using CipherVault.Services.Contracts;

var builder = WebApplication.CreateBuilder(args);

// ---------- Database ----------
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)
);

// Phase 2 — Vault
builder.Services.AddScoped<IVaultItemRepository, VaultItemRepository>();
builder.Services.AddScoped<IVaultItemService, VaultItemService>();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ---------- Identity ----------
builder
    .Services.AddDefaultIdentity<IdentityUser>(options =>
    {
        // Sign-in requirements
        options.SignIn.RequireConfirmedAccount = false; // set true once email sender is wired up

        // Password policy
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;

        // Lockout policy
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // User policy
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

// ---------- Cookie authentication ----------
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ReturnUrlParameter = "returnUrl";

    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;

    // Do NOT use the __Host- prefix. It requires Secure + Path=/ + no Domain,
    // and is silently rejected by the browser if any of those are not present.
    options.Cookie.Name = "CipherVault.Auth";

    // Always require HTTPS for the cookie in BOTH dev and prod.
    // This guarantees the cookie is only ever sent over TLS.
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

    // Cookie is only sent to the app's own paths.
    options.Cookie.Path = "/";

    // Do NOT set options.Cookie.Domain at all — leave it null so the cookie
    // is scoped to the exact host that issued it.

    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
});

// ---------- Data Protection (persistent key ring) ----------
// The path is configurable via "DataProtection:KeyDirectory" in appsettings.json
// or environment variable DataProtection__KeyDirectory.
var keyDirectory = builder.Configuration["DataProtection:KeyDirectory"] ?? "App_Data/keys";
Directory.CreateDirectory(keyDirectory);

builder
    .Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory))
    .SetApplicationName("CipherVault");

// ---------- MVC + Razor Pages (Razor Pages needed for Default Identity UI) ----------
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// ---------- Dev-only seed ----------
using (var scope = app.Services.CreateScope())
{
    await DbInitializer.SeedAsync(scope.ServiceProvider, app.Environment);
}

// ---------- Pipeline ----------
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); // Default Identity UI endpoints

app.Run();
