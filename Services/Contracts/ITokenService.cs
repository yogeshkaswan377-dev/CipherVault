using CipherVault.DTOs.Api;
using Microsoft.AspNetCore.Identity;

namespace CipherVault.Services.Contracts;

public interface ITokenService
{
    TokenResponseDTO CreateToken(IdentityUser user);
}
