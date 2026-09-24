using CipherVault.Data;
using CipherVault.Models;
using CipherVault.Repositories.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CipherVault.Repositories
{
    public class VaultItemRepository : IVaultItemRepository
    {
        private readonly ApplicationDbContext _db;

        public VaultItemRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        // Tracked: callers (update / delete) need change tracking.
        public Task<VaultItem?> GetByIdAsync(int id, CancellationToken ct = default) =>
            _db.VaultItems.FirstOrDefaultAsync(v => v.Id == id, ct);

        // AsNoTracking: list views are read-only and can be long.
        public async Task<IReadOnlyList<VaultItem>> GetAllByUserAsync(
            string userId,
            CancellationToken ct = default
        ) =>
            await _db
                .VaultItems.AsNoTracking()
                .Where(v => v.UserId == userId)
                .OrderByDescending(v => v.UpdatedAt)
                .ToListAsync(ct);

        public async Task<IReadOnlyList<VaultItem>> SearchAsync(
            string userId,
            string? searchTerm,
            string? category
        )
        {
            IQueryable<VaultItem> q = _db.VaultItems.AsNoTracking().Where(v => v.UserId == userId);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                q = q.Where(v =>
                    v.Title.Contains(term)
                    || (v.Username != null && v.Username.Contains(term))
                    || (v.Url != null && v.Url.Contains(term))
                );
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                q = q.Where(v => v.Category == category);
            }

            return await q.OrderByDescending(v => v.UpdatedAt).ToListAsync();
        }

        public async Task<(List<VaultItem> Items, int TotalCount)> SearchPagedAsync(
            string userId,
            string? search,
            string? category,
            int skip,
            int take
        )
        {
            if (string.IsNullOrWhiteSpace(userId))
                return (new List<VaultItem>(), 0);

            if (skip < 0)
                skip = 0;
            if (take <= 0)
                take = 10;

            IQueryable<VaultItem> query = _db
                .VaultItems.AsNoTracking()
                .Where(v => v.UserId == userId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                // SQL Server default collation is case-insensitive, so LIKE is CI.
                // If your DB uses a case-sensitive collation, swap to EF.Functions.Like(v.Title.ToLower(), ...)
                var pattern = $"%{term}%";
                query = query.Where(v =>
                    EF.Functions.Like(v.Title, pattern)
                    || (v.Username != null && EF.Functions.Like(v.Username, pattern))
                    || (v.Url != null && EF.Functions.Like(v.Url, pattern))
                );
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(v => v.Category == category);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(v => v.UpdatedAt) // primary sort
                .ThenByDescending(v => v.Id) // STABLE tie-breaker — do NOT remove
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<int> CreateAsync(VaultItem item, CancellationToken ct = default)
        {
            _db.VaultItems.Add(item);
            await _db.SaveChangesAsync(ct);
            return item.Id;
        }

        public async Task UpdateAsync(VaultItem item, CancellationToken ct = default)
        {
            _db.VaultItems.Update(item);
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(VaultItem item, CancellationToken ct = default)
        {
            _db.VaultItems.Remove(item);
            await _db.SaveChangesAsync(ct);
        }
    }
}
