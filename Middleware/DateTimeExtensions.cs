namespace CipherVault.Helpers
{
    /// <summary>
    /// DateTime extensions for consistent UTC → browser-timezone rendering.
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Formats a DateTime as ISO 8601 with a 'Z' suffix (UTC).
        ///
        /// WHY: EF Core reads SQL Server datetime2 columns as DateTime with
        /// Kind=Unspecified. ToString("o") on such a value produces no timezone
        /// suffix, so JavaScript's `new Date(...)` would interpret it as LOCAL
        /// time and skip the UTC-to-local conversion. This helper forces Kind=Utc
        /// before formatting, guaranteeing the 'Z' suffix that JS expects.
        /// </summary>
        public static string ToIsoUtc(this DateTime dt)
        {
            var utc = dt.Kind switch
            {
                DateTimeKind.Utc => dt,
                DateTimeKind.Local => dt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            };
            return utc.ToString("o");
        }

        /// <summary>
        /// Same as ToIsoUtc but for nullable DateTime.
        /// Returns empty string for null.
        /// </summary>
        public static string ToIsoUtc(this DateTime? dt) =>
            dt.HasValue ? dt.Value.ToIsoUtc() : string.Empty;
    }
}
