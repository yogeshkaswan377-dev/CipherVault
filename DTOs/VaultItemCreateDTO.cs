using System.ComponentModel.DataAnnotations;
using CipherVault.Validation;

namespace CipherVault.DTOs;

public class VaultItemCreateDTO
{
    [Required, StringLength(100)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [Required, ValidCategory]
    [Display(Name = "Category")]
    public string Category { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Username")]
    public string? Username { get; set; }

    [Required(ErrorMessage = "Secret is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Secret")]
    public string Secret { get; set; } = string.Empty;

    [DataType(DataType.MultilineText)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    [OptionalUrl, StringLength(500)]
    [Display(Name = "URL")]
    public string? Url { get; set; }
}
