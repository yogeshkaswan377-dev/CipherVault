using CipherVault.DTOs.Api;
using CipherVault.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CipherVault.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly SignInManager<IdentityUser> _signIn;
    private readonly UserManager<IdentityUser> _users;
    private readonly ITokenService _tokens;

    public AuthApiController(
        SignInManager<IdentityUser> signIn,
        UserManager<IdentityUser> users,
        ITokenService tokens
    )
    {
        _signIn = signIn;
        _users = users;
        _tokens = tokens;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponseDTO>> Login([FromBody] LoginRequestDTO dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await _users.FindByEmailAsync(dto.Email);
        if (user is null)
        {
            // Uniform error — do not reveal whether the account exists.
            return Unauthorized(new { error = "invalid_credentials" });
        }

        // lockoutOnFailure:true respects the Phase 1 lockout policy (5 tries / 15 min).
        var result = await _signIn.CheckPasswordSignInAsync(
            user,
            dto.Password,
            lockoutOnFailure: true
        );
        if (result.IsLockedOut)
        {
            return StatusCode(StatusCodes.Status423Locked, new { error = "account_locked" });
        }
        if (!result.Succeeded)
        {
            return Unauthorized(new { error = "invalid_credentials" });
        }

        return Ok(_tokens.CreateToken(user));
    }
}
    