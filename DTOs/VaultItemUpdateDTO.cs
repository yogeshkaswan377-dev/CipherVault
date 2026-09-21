using System.ComponentModel.DataAnnotations;
using CipherVault.Validation;

namespace CipherVault.DTOs;

/// <summary>
/// Update contract.
/// Secret: null OR empty  -> service preserves the existing ciphertext.
/// Notes : null           -> service preserves the existing ciphertext.
/// Notes : empty string   -> service sets EncryptedNotes = null (explicit clear).
/// </summary>
public class VaultItemUpdateDTO
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

    [DataType(DataType.Password)]
    [Display(Name = "Secret")]
    public string? Secret { get; set; }

    [DataType(DataType.MultilineText)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    [OptionalUrl, StringLength(500)]
    [Display(Name = "URL")]
    public string? Url { get; set; }
}
