using System.Text;
using CipherVault.Data;
using CipherVault.Repositories;
using CipherVault.Repositories.Contracts;
using CipherVault.Services;
using CipherVault.Services.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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

// --- Existing AddDefaultIdentity (unchanged) ---

// Explicit scheme separation: cookie for MVC, Bearer for API.
// AddAuthentication() with no arguments extends the AuthenticationBuilder
// created by AddDefaultIdentity — it does NOT replace the Identity cookies.
builder
    .Services.AddAuthentication()
    .AddJwtBearer(
        JwtBearerDefaults.AuthenticationScheme,
        options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        builder.Configuration["Jwt:Key"]
                            ?? throw new InvalidOperationException(
                                "Jwt:Key is not configured. Set it via user-secrets (dev) or the Jwt__Key environment variable (prod)."
                            )
                    )
                ),
                ClockSkew = TimeSpan.FromMinutes(1),
            };
            // Do not redirect API requests to a login page.
            options.Events = new JwtBearerEvents
            {
                OnChallenge = ctx =>
                {
                    // Suppress the default redirect-to-login behaviour; return 401.
                    ctx.HandleResponse();
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    ctx.Response.ContentType = "application/json";
                    return ctx.Response.WriteAsync("{\"error\":\"unauthorized\"}");
                },
            };
        }
    );

// Phase 3 services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

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

    if (app.Environment.IsProduction())
    {
        app.Logger.LogInformation("HSTS middleware enabled for Production.");
        app.UseHsts();
    }
}   

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); // Default Identity UI endpoints

app.Run();
