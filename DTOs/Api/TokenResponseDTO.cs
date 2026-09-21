namespace CipherVault.DTOs.Api;

public class TokenResponseDTO
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; } // seconds
    public DateTime ExpiresAt { get; set; } // UTC
}
