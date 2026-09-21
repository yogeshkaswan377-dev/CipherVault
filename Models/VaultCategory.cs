namespace CipherVault.Models;

/// <summary>
/// Allowed vault categories. Stored as strings in the DB so that
/// renaming a C# identifier can never orphan existing rows.
/// </summary>
public static class VaultCategory
{
    public const string Password   = "Password";
    public const string Note       = "Note";
    public const string ApiKey     = "API Key";
    public const string CreditCard = "Credit Card";
    public const string Other      = "Other";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Password, Note, ApiKey, CreditCard, Other
    };

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && All.Contains(value, StringComparer.Ordinal);
}