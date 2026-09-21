using System.ComponentModel.DataAnnotations;
using CipherVault.Models;

namespace CipherVault.Validation;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class ValidCategoryAttribute : ValidationAttribute
{
    public ValidCategoryAttribute()
    {
        ErrorMessage = "Category must be one of: " + string.Join(", ", VaultCategory.All) + ".";
    }

    public override bool IsValid(object? value) => value is string s && VaultCategory.IsValid(s);
}
