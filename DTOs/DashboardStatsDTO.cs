namespace CipherVault.DTOs;

public class DashboardStatsDTO
{
    public int TotalItems { get; set; }
    public IReadOnlyList<CategoryCountDTO> CategoryCounts { get; set; } =
        Array.Empty<CategoryCountDTO>();
    public IReadOnlyList<VaultItemDisplayDTO> RecentItems { get; set; } =
        Array.Empty<VaultItemDisplayDTO>();
}

public class CategoryCountDTO
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}
