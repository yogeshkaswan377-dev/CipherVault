using System.ComponentModel.DataAnnotations;

namespace CipherVault.Validation;

/// <summary>
/// Like [Url], but treats null / whitespace as valid so optional URL fields
/// don't fail validation when the user leaves the input blank.
/// Only http/https absolute URLs are accepted (blocks javascript:, data:, file:).
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class OptionalUrlAttribute : ValidationAttribute
{
    public OptionalUrlAttribute()
    {
        ErrorMessage = "Enter a valid absolute http(s) URL (e.g. https://example.com).";
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;
        if (value is not string s)
            return false;
        if (string.IsNullOrWhiteSpace(s))
            return true;

        return Uri.TryCreate(s.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
