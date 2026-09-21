using CipherVault.DTOs;

namespace CipherVault.Services.Contracts;

public interface IVaultItemService
{
    Task<IReadOnlyList<VaultItemDisplayDTO>> GetAllItemsAsync(
        string userId,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<VaultItemDisplayDTO>> SearchItemsAsync(
        string userId,
        string? searchTerm,
        string? category
    );

    /// <summary>Returns null if the item does not exist OR is not owned by userId.</summary>
    Task<VaultItemDisplayDTO?> GetItemAsync(int id, string userId, CancellationToken ct = default);

    Task<int> CreateItemAsync(
        VaultItemCreateDTO dto,
        string userId,
        CancellationToken ct = default
    );

    /// <summary>Returns false if the item does not exist OR is not owned by userId.</summary>
    Task<bool> UpdateItemAsync(
        int id,
        VaultItemUpdateDTO dto,
        string userId,
        CancellationToken ct = default
    );

    /// <summary>Returns false if the item does not exist OR is not owned by userId.</summary>
    Task<bool> DeleteItemAsync(int id, string userId, CancellationToken ct = default);

    /// <summary>Returns decrypted secret, or null if not found / not owned.</summary>
    Task<string?> RevealSecretAsync(int id, string userId, CancellationToken ct = default);

    /// <summary>Returns decrypted notes, or null if not found / not owned / no notes.</summary>
    Task<string?> RevealNotesAsync(int id, string userId, CancellationToken ct = default);
}
