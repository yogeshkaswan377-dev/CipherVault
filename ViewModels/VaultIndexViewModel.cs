using CipherVault.DTOs;

namespace CipherVault.ViewModels;

public class VaultIndexViewModel
{
    public IReadOnlyList<VaultItemDisplayDTO> Items { get; set; } =
        Array.Empty<VaultItemDisplayDTO>();
    public string? SearchTerm { get; set; }
    public string? Category { get; set; }
    public IReadOnlyList<string> AvailableCategories { get; set; } = Array.Empty<string>();
}
