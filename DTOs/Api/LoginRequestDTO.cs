using System.ComponentModel.DataAnnotations;

namespace CipherVault.DTOs.Api;

public class LoginRequestDTO
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
