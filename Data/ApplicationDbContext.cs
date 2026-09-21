using CipherVault.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CipherVault.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<VaultItem> VaultItems => Set<VaultItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<VaultItem>(entity =>
        {
            entity.HasKey(v => v.Id);

            entity.Property(v => v.UserId).IsRequired();
            entity.Property(v => v.Title).IsRequired().HasMaxLength(100);
            entity.Property(v => v.Category).IsRequired().HasMaxLength(50);
            entity.Property(v => v.Username).HasMaxLength(100);
            entity.Property(v => v.EncryptedSecret).IsRequired();
            entity.Property(v => v.Url).HasMaxLength(500);

            // No navigation collection on IdentityUser -> WithMany() with no argument.
            entity
                .HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Every query filters by UserId; category filter lands here in Phase 3.
            entity.HasIndex(v => v.UserId);
            entity.HasIndex(v => new { v.UserId, v.Category });
        });
    }
}
