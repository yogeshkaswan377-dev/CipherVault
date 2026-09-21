using System.Security.Claims;
using System.Threading;
using CipherVault.DTOs;
using CipherVault.Services.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CipherVault.Controllers.Api;

[ApiController]
[Route("api/v1/vault")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class VaultApiController : ControllerBase
{
    private readonly IVaultItemService _vault;

    public VaultApiController(IVaultItemService vault) => _vault = vault;

    private string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException(
            "Missing nameidentifier claim on authenticated principal."
        );

    // ------------------------------------------------------------- list

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VaultItemDisplayDTO>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] bool decrypt = false,
        CancellationToken ct = default
    )
    {
        if (decrypt)
            return BadRequest(new { error = "decryption_not_supported" });

        var items = await _vault.SearchItemsAsync(UserId, search, category);
        return Ok(items);
    }

    // ---------------------------------------------------------- get by id

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VaultItemDisplayDTO>> GetById(
        int id,
        [FromQuery] bool decrypt = false,
        CancellationToken ct = default
    )
    {
        if (decrypt)
            return BadRequest(new { error = "decryption_not_supported" });

        var item = await _vault.GetItemAsync(id, UserId, ct);
        if (item is null)
            return NotFound();
        return Ok(item);
    }

    // ------------------------------------------------------------- create

    [HttpPost]
    public async Task<ActionResult<VaultItemDisplayDTO>> Create(
        [FromBody] VaultItemCreateDTO dto,
        CancellationToken ct
    )
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        // Service returns the new id — fetch the display DTO for the response body.
        var id = await _vault.CreateItemAsync(dto, UserId, ct);
        var created = await _vault.GetItemAsync(id, UserId, ct);
        if (created is null)
            return Problem("Item was created but could not be re-read.");

        return CreatedAtAction(nameof(GetById), new { id }, created);
    }

    // ------------------------------------------------------------- update

    [HttpPut("{id:int}")]
    public async Task<ActionResult<VaultItemDisplayDTO>> Update(
        int id,
        [FromBody] VaultItemUpdateDTO dto,
        CancellationToken ct
    )
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var ok = await _vault.UpdateItemAsync(id, dto, UserId, ct);
        if (!ok)
            return NotFound();

        // Blank-preserve rule already applied inside the service.
        var updated = await _vault.GetItemAsync(id, UserId, ct);
        if (updated is null)
            return NotFound();
        return Ok(updated);
    }

    // ------------------------------------------------------------- delete

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var ok = await _vault.DeleteItemAsync(id, UserId, ct);
        return ok ? NoContent() : NotFound();
    }
}
