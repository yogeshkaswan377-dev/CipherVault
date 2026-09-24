using CipherVault.DTOs;

namespace CipherVault.ViewModels
{
    /// <summary>
    /// View-model for the Vault index page.
    /// Wraps the display items plus filter + pagination metadata.
    /// </summary>
    public class VaultIndexViewModel
    {
        // ---- Results ----
        public List<VaultItemDisplayDTO> Items { get; set; } = new();

        // ---- Filter state (round-trip) ----
        public string? Search { get; set; }
        public string? Category { get; set; }

        // ---- Pagination metadata ----
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }

        public int TotalPages =>
            PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);

        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;

        public int FirstItemOnPage => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;

        public int LastItemOnPage => Math.Min(Page * PageSize, TotalCount);
    }
}
