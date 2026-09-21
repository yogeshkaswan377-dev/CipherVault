using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CipherVault.DTOs.Api;
using CipherVault.Services.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace CipherVault.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config) => _config = config;

    public TokenResponseDTO CreateToken(IdentityUser user)
    {
        var key =
            _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = _config["Jwt:Issuer"] ?? "CipherVault";
        var aud = _config["Jwt:Audience"] ?? "CipherVault.Api";
        var mins = int.TryParse(_config["Jwt:ExpiryMinutes"], out var m) ? m : 60;

        // Only non-sensitive claims. Never put anything derived from the vault here.
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256
        );

        var expiresAt = DateTime.UtcNow.AddMinutes(mins);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: aud,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: creds
        );

        return new TokenResponseDTO
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            TokenType = "Bearer",
            ExpiresIn = mins * 60,
            ExpiresAt = expiresAt,
        };
    }
}
