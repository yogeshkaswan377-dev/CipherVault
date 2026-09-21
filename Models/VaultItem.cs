using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CipherVault.Models;

/// <summary>
/// A single encrypted vault entry.
/// Only <see cref="EncryptedSecret"/> and <see cref="EncryptedNotes"/> hold
/// sensitive data; everything else is non-sensitive metadata that supports
/// search / filtering (Phase 3).
/// </summary>
public class VaultItem
{
    public int Id { get; set; }

    /// <summary>FK to AspNetUsers.Id. Ownership is enforced in the service layer.</summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Title { get; set; } = string.Empty;

    /// <summary>One of <see cref="VaultCategory.All"/>.</summary>
    [Required, StringLength(50)]
    public string Category { get; set; } = VaultCategory.Password;

    [StringLength(100)]
    public string? Username { get; set; }

    /// <summary>Data-Protection ciphertext. NEVER plaintext.</summary>
    [Required]
    public string EncryptedSecret { get; set; } = string.Empty;

    /// <summary>Data-Protection ciphertext, or null when the item has no notes.</summary>
    public string? EncryptedNotes { get; set; }

    [StringLength(500)]
    public string? Url { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public IdentityUser? User { get; set; }
}
