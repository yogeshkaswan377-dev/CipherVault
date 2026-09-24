using System.Security.Cryptography;
using CipherVault.DTOs;
using CipherVault.Models;
using CipherVault.Repositories.Contracts;
using CipherVault.Services.Contracts;
using Microsoft.AspNetCore.DataProtection;

namespace CipherVault.Services;

public class VaultItemService : IVaultItemService
{
    // ---------------------------------------------------------------------
    // COMPATIBILITY CONTRACT
    // These purpose strings are baked into every ciphertext already written
    // by this app. Changing them makes ALL existing data undecryptable.
    // Treat them as immutable as soon as production data exists.
    // ---------------------------------------------------------------------
    public const string SecretPurpose = "CipherVault.Secret";
    public const string NotesPurpose = "CipherVault.Notes";

    private const string Mask = "••••••••";

    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 10;

    private readonly IVaultItemRepository _repository;
    private readonly IDataProtector _secretProtector;
    private readonly IDataProtector _notesProtector;
    private readonly ILogger<VaultItemService> _logger;

    public VaultItemService(
        IVaultItemRepository repository,
        IDataProtectionProvider protectionProvider,
        ILogger<VaultItemService> logger
    )
    {
        _repository = repository;
        _secretProtector = protectionProvider.CreateProtector(SecretPurpose);
        _notesProtector = protectionProvider.CreateProtector(NotesPurpose);
        _logger = logger;
    }

    // ------------------------------------------------------------------ read

    public async Task<IReadOnlyList<VaultItemDisplayDTO>> GetAllItemsAsync(
        string userId,
        CancellationToken ct = default
    )
    {
        var items = await _repository.GetAllByUserAsync(userId, ct);
        return items.Select(ToDisplay).ToList();
    }

    public async Task<VaultItemDisplayDTO?> GetItemAsync(
        int id,
        string userId,
        CancellationToken ct = default
    )
    {
        var item = await _repository.GetByIdAsync(id, ct);
        // Ownership check: return null (=> 404 upstream) for missing OR foreign items.
        if (item is null || !IsOwner(item, userId))
            return null;
        return ToDisplay(item);
    }

    public async Task<IReadOnlyList<VaultItemDisplayDTO>> SearchItemsAsync(
        string userId,
        string? searchTerm,
        string? category
    )
    {
        string? safeCategory =
            !string.IsNullOrWhiteSpace(category) && VaultCategory.IsValid(category)
                ? category
                : null;

        var items = await _repository.SearchAsync(userId, searchTerm, safeCategory);
        return items.Select(ToDisplay).ToList();
    }

    public async Task<(List<VaultItemDisplayDTO> Items, int TotalCount)> SearchItemsPagedAsync(
        string userId,
        string? search,
        string? category,
        int page,
        int pageSize
    )
    {
        if (string.IsNullOrWhiteSpace(userId))
            return (new List<VaultItemDisplayDTO>(), 0);

        // --- Clamp (business rule — belongs in service, NOT controller) ---
        if (page < 1)
            page = 1;
        if (pageSize < MinPageSize)
            pageSize = DefaultPageSize;
        if (pageSize > MaxPageSize)
            pageSize = MaxPageSize;

        // --- Whitelist category so caller can't smuggle arbitrary strings ---
        string? safeCategory = null;
        if (!string.IsNullOrWhiteSpace(category) && VaultCategory.IsValid(category))
        {
            safeCategory = category;
        }

        var skip = (page - 1) * pageSize;

        var (items, totalCount) = await _repository.SearchPagedAsync(
            userId,
            search,
            safeCategory,
            skip,
            pageSize
        );

        // Map to display DTOs — NO decryption happens on list paths.
        var dtos = items.Select(MapToDisplayDto).ToList();

        // Audit trail — no values, no user input echo
        _logger.LogInformation(
            "Vault search performed. UserId={UserId} Page={Page} PageSize={PageSize} Total={Total}",
            userId,
            page,
            pageSize,
            totalCount
        );

        return (dtos, totalCount);
    }

    // Adjust this to match your existing private mapper name/shape.
    private static VaultItemDisplayDTO MapToDisplayDto(VaultItem item) =>
        new()
        {
            Id = item.Id,
            Title = item.Title,
            Category = item.Category,
            Username = item.Username,
            Url = item.Url,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            MaskedSecret = "••••••••", // fixed mask — never plaintext
            HasNotes = !string.IsNullOrEmpty(item.EncryptedNotes),
        };

    // ---------------------------------------------------------------- write

    public async Task<int> CreateItemAsync(
        VaultItemCreateDTO dto,
        string userId,
        CancellationToken ct = default
    )
    {
        var now = DateTime.UtcNow;

        var item = new VaultItem
        {
            UserId = userId,
            Title = dto.Title.Trim(),
            Category = dto.Category,
            Username = NullIfBlank(dto.Username),
            Url = NullIfBlank(dto.Url),
            EncryptedSecret = _secretProtector.Protect(dto.Secret),
            EncryptedNotes = string.IsNullOrEmpty(dto.Notes)
                ? null
                : _notesProtector.Protect(dto.Notes),
            CreatedAt = now,
            UpdatedAt = now,
        };

        var id = await _repository.CreateAsync(item, ct);

        // Audit log: action + ids + timestamp ONLY. No values.
        _logger.LogInformation(
            "Vault audit. Action={Action} UserId={UserId} ItemId={ItemId}",
            "Create",
            userId,
            id
        );

        return id;
    }

    public async Task<bool> UpdateItemAsync(
        int id,
        VaultItemUpdateDTO dto,
        string userId,
        CancellationToken ct = default
    )
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null || !IsOwner(item, userId))
            return false;

        item.Title = dto.Title.Trim();
        item.Category = dto.Category;
        item.Username = NullIfBlank(dto.Username);
        item.Url = NullIfBlank(dto.Url);

        // --- Secret: null OR empty  =>  PRESERVE existing ciphertext ---------
        if (!string.IsNullOrEmpty(dto.Secret))
        {
            item.EncryptedSecret = _secretProtector.Protect(dto.Secret);
        }

        // --- Notes: three-way rule ------------------------------------------
        if (dto.Notes is null)
        {
            // Preserve existing notes (ciphertext untouched).
        }
        else if (dto.Notes.Length == 0)
        {
            // Explicit clear.
            item.EncryptedNotes = null;
        }
        else
        {
            item.EncryptedNotes = _notesProtector.Protect(dto.Notes);
        }

        item.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(item, ct);

        _logger.LogInformation(
            "Vault audit. Action={Action} UserId={UserId} ItemId={ItemId}",
            "Update",
            userId,
            id
        );

        return true;
    }

    public async Task<bool> DeleteItemAsync(int id, string userId, CancellationToken ct = default)
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null || !IsOwner(item, userId))
            return false;

        await _repository.DeleteAsync(item, ct);

        _logger.LogInformation(
            "Vault audit. Action={Action} UserId={UserId} ItemId={ItemId}",
            "Delete",
            userId,
            id
        );

        return true;
    }

    // --------------------------------------------------------------- reveal

    public async Task<string?> RevealSecretAsync(
        int id,
        string userId,
        CancellationToken ct = default
    )
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null || !IsOwner(item, userId))
            return null;

        _logger.LogInformation(
            "Vault audit. Action={Action} UserId={UserId} ItemId={ItemId}",
            "Reveal",
            userId,
            id
        );

        return Unprotect(_secretProtector, item.EncryptedSecret, id, "secret");
    }

    public async Task<string?> RevealNotesAsync(
        int id,
        string userId,
        CancellationToken ct = default
    )
    {
        var item = await _repository.GetByIdAsync(id, ct);
        if (item is null || !IsOwner(item, userId))
            return null;
        if (item.EncryptedNotes is null)
            return null;

        _logger.LogInformation(
            "Vault audit. Action={Action} UserId={UserId} ItemId={ItemId}",
            "Reveal",
            userId,
            id
        );

        return Unprotect(_notesProtector, item.EncryptedNotes, id, "notes");
    }

    // ---------------------------------------------------------------- helpers

    private static bool IsOwner(VaultItem item, string userId) =>
        string.Equals(item.UserId, userId, StringComparison.Ordinal);

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static VaultItemDisplayDTO ToDisplay(VaultItem item) =>
        new()
        {
            Id = item.Id,
            Title = item.Title,
            Category = item.Category,
            Username = item.Username,
            Url = item.Url,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt,
            MaskedSecret = Mask,
            HasNotes = item.EncryptedNotes is not null,
        };

    private string Unprotect(IDataProtector protector, string ciphertext, int itemId, string field)
    {
        try
        {
            return protector.Unprotect(ciphertext);
        }
        catch (CryptographicException ex)
        {
            // Log the *fact*, never the ciphertext or any decrypted value.
            _logger.LogError(ex, "Decryption failed. ItemId={ItemId} Field={Field}", itemId, field);

            // Do not leak crypto details to the caller.
            throw new InvalidOperationException("Unable to decrypt the requested value.");
        }
    }
}
