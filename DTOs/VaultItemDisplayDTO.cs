using CipherVault.Models;

namespace CipherVault.DTOs;

public class VaultItemDisplayDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Url { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string MaskedSecret { get; set; } = string.Empty;
    public bool HasNotes { get; set; }

    // Single source of truth for the mask — never leaks plaintext.
    public static VaultItemDisplayDTO FromEntity(VaultItem v) =>
        new()
        {
            Id = v.Id,
            Title = v.Title,
            Category = v.Category,
            Username = v.Username,
            Url = v.Url,
            CreatedAt = v.CreatedAt,
            UpdatedAt = v.UpdatedAt,
            MaskedSecret = "••••••••",
            HasNotes = v.EncryptedNotes != null,
        };
}
