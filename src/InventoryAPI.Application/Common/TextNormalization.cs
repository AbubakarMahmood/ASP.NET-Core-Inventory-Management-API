namespace InventoryAPI.Application.Common;

/// <summary>
/// Canonical text normalization used before uniqueness checks and persistence.
/// Validation remains responsible for length and required-field rules.
/// </summary>
public static class TextNormalization
{
    public static string Sku(string value) => value.Trim().ToUpperInvariant();
    public static string Email(string value) => value.Trim().ToLowerInvariant();
    public static string Code(string value) => value.Trim().ToUpperInvariant();
    public static string Required(string value) => value.Trim();
    public static string Optional(string? value) => value?.Trim() ?? string.Empty;
    public static string? OptionalOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
