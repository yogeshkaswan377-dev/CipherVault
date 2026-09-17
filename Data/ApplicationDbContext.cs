using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CipherVault.Data;

/// <summary>
/// Application database context. Inherits <see cref="IdentityDbContext{TUser}"/>
/// so all ASP.NET Core Identity tables (Users, Roles, Claims, Logins, Tokens)
/// are created automatically by EF Core migrations.
///
/// Phase 2 will add: DbSet&lt;VaultItem&gt; and its relationship to IdentityUser.
/// </summary>
public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Phase 2 will configure the VaultItem -> IdentityUser relationship here.
    }
}
