using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CipherVault.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services, IWebHostEnvironment env)
    {
#if DEBUG
        // Double guard: only Development + Debug build
        if (!env.IsDevelopment())
            return;

        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        // Apply pending migrations (dev only)
        await context.Database.MigrateAsync();

        // Phase 2 will add test vault items here.
        // For now, no seed data.

        // Example for Phase 2 (leave commented):
        // var testUser = await userManager.FindByNameAsync("testuser");
        // if (testUser == null) { ... create test user ... }
#else
        await Task.CompletedTask;
#endif
    }
}
