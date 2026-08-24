namespace BSE.Host.Helpers;

/// <summary>
/// Display formatting helpers for BSE identifiers, matching legacy application output.
/// </summary>
public static class BseFormat
{
    /// <summary>
    /// Formats a raw 9-character RBSE number (CHAR 9) as XX/XX/XXXXX.
    /// Returns the value unchanged if it does not have exactly 9 characters.
    /// </summary>
    public static string FormatRbse(string? value) =>
        value is { Length: 9 }
            ? $"{value[..2]}/{value[2..4]}/{value[4..]}"
            : value ?? string.Empty;

    /// <summary>
    /// Formats a raw 11-character CPHH number (CHAR 11) as XX/XXX/XXXX/XX.
    /// Returns the value unchanged if it does not have exactly 11 characters.
    /// </summary>
    public static string FormatCphh(string? value) =>
        value is { Length: 11 }
            ? $"{value[..2]}/{value[2..5]}/{value[5..9]}/{value[9..]}"
            : value ?? string.Empty;
}
