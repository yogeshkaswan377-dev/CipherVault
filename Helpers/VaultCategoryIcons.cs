namespace CipherVault.Helpers
{
    public static class VaultCategoryIcons
    {
        public static string For(string? category) =>
            category switch
            {
                "Password" => "bi-key-fill",
                "Note" => "bi-journal-text",
                "API Key" => "bi-shield-lock-fill",
                "Credit Card" => "bi-credit-card-fill",
                _ => "bi-folder-fill",
            };
    }
}
