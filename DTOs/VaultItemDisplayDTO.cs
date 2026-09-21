namespace CipherVault.DTOs;

/// <summary>
/// Read-only projection. MUST NEVER carry plaintext secret or notes.
/// </summary>
public class VaultItemDisplayDTO
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Url { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Constant mask; never derived from the real secret length.</summary>
    public string MaskedSecret { get; set; } = "••••••••";

    public bool HasNotes { get; set; }
}
