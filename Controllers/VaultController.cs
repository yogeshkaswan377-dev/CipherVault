using CipherVault.DTOs;
using CipherVault.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CipherVault.Controllers;

[Authorize]
public class VaultController : Controller
{
    private readonly IVaultItemService _vaultService;
    private readonly UserManager<IdentityUser> _userManager;

    public VaultController(IVaultItemService vaultService, UserManager<IdentityUser> userManager)
    {
        _vaultService = vaultService;
        _userManager = userManager;
    }

    // With [Authorize] on the controller this is always non-null.
    private string CurrentUserId =>
        _userManager.GetUserId(User)
        ?? throw new InvalidOperationException("Authenticated user id is not available.");

    // -------------------------------------------------------------- Index

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var items = await _vaultService.GetAllItemsAsync(CurrentUserId, ct);
        return View(items);
    }

    // ------------------------------------------------------------- Create

    [HttpGet]
    public IActionResult Create() => View(new VaultItemCreateDTO());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VaultItemCreateDTO dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var id = await _vaultService.CreateItemAsync(dto, CurrentUserId, ct);
        TempData["StatusMessage"] = "Vault item created.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // --------------------------------------------------------------- Edit

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var item = await _vaultService.GetItemAsync(id, CurrentUserId, ct);
        if (item is null)
            return NotFound(); // 404, never 403 (no existence leak)

        // Secret / Notes intentionally left blank: "leave blank to keep existing value".
        var dto = new VaultItemUpdateDTO
        {
            Title = item.Title,
            Category = item.Category,
            Username = item.Username,
            Url = item.Url,
            Secret = null,
            Notes = null,
        };

        ViewData["ItemId"] = item.Id;
        ViewData["HasNotes"] = item.HasNotes;
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        VaultItemUpdateDTO dto,
        bool clearNotes,
        CancellationToken ct
    )
    {
        if (!ModelState.IsValid)
        {
            ViewData["ItemId"] = id;
            return View(dto);
        }

        // Request-shaping only (NOT business logic):
        //   blank Secret            -> null  (service preserves ciphertext)
        //   blank Notes, no checkbox-> null  (service preserves ciphertext)
        //   "Clear notes" ticked    -> ""    (service clears ciphertext)
        dto.Secret = string.IsNullOrWhiteSpace(dto.Secret) ? null : dto.Secret;
        dto.Notes = clearNotes
            ? string.Empty
            : (string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes);

        var ok = await _vaultService.UpdateItemAsync(id, dto, CurrentUserId, ct);
        if (!ok)
            return NotFound();

        TempData["StatusMessage"] = "Vault item updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ------------------------------------------------------------- Details

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var item = await _vaultService.GetItemAsync(id, CurrentUserId, ct);
        if (item is null)
            return NotFound();
        return View(item);
    }

    // -------------------------------------------------------------- Delete

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var item = await _vaultService.GetItemAsync(id, CurrentUserId, ct);
        if (item is null)
            return NotFound();
        return View(item);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        var ok = await _vaultService.DeleteItemAsync(id, CurrentUserId, ct);
        if (!ok)
            return NotFound();

        TempData["StatusMessage"] = "Vault item deleted.";
        return RedirectToAction(nameof(Index));
    }

    // -------------------------------------------------------------- Reveal

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevealSecret(int id, CancellationToken ct)
    {
        var value = await _vaultService.RevealSecretAsync(id, CurrentUserId, ct);
        if (value is null)
            return NotFound();
        return Json(new { value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevealNotes(int id, CancellationToken ct)
    {
        var value = await _vaultService.RevealNotesAsync(id, CurrentUserId, ct);
        if (value is null)
            return NotFound();
        return Json(new { value });
    }
}
