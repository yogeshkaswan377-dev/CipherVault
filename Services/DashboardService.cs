using CipherVault.Data;
using CipherVault.DTOs;
using CipherVault.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CipherVault.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context) => _context = context;

    // Trivial read-only aggregates — no repository per the architecture rule.
    public async Task<DashboardStatsDTO> GetStatsAsync(string userId)
    {
        var total = await _context.VaultItems.AsNoTracking().CountAsync(v => v.UserId == userId);

        var byCategory = await _context
            .VaultItems.AsNoTracking()
            .Where(v => v.UserId == userId)
            .GroupBy(v => v.Category)
            .Select(g => new CategoryCountDTO { Category = g.Key, Count = g.Count() })
            .ToListAsync();

        var recentRaw = await _context
            .VaultItems.AsNoTracking()
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.CreatedAt)
            .Take(5)
            .ToListAsync();

        return new DashboardStatsDTO
        {
            TotalItems = total,
            CategoryCounts = byCategory,
            RecentItems = recentRaw.Select(VaultItemDisplayDTO.FromEntity).ToList(),
        };
    }
}
