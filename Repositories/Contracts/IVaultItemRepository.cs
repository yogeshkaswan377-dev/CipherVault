using CipherVault.Models;

namespace CipherVault.Repositories.Contracts;

/// <summary>
/// Pure data access for VaultItem. Contains NO ownership or encryption logic —
/// that is the service's job. Do not add userId filters here; a single
/// ownership-check location is easier to audit for IDOR.
/// </summary>
public interface IVaultItemRepository
{
    Task<VaultItem?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<VaultItem>> GetAllByUserAsync(string userId, CancellationToken ct = default);
    Task<int> CreateAsync(VaultItem item, CancellationToken ct = default);
    Task UpdateAsync(VaultItem item, CancellationToken ct = default);
    Task DeleteAsync(VaultItem item, CancellationToken ct = default);
}
