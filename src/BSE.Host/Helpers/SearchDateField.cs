using System.Globalization;

namespace BSE.Host.Helpers;

/// <summary>
/// Validates the optional date-range fields on the search pages. Legacy required these fields
/// be either blank or a real date; business has confirmed the same rule applies post-migration,
/// with a GDS-compliant error message shown when an invalid date is entered.
/// </summary>
internal static class SearchDateField
{
    public const string InvalidDateMessage = "Enter a valid date";

    /// <summary>
    /// Parses a date-range field value — either the MOJ date-picker's free-text d/M/yyyy /
    /// dd/MM/yyyy format, or the legacy native date input's yyyy-MM-dd (ISO). A blank value is
    /// valid (the field is optional). Returns false with <paramref name="error"/> set when
    /// non-blank but not a real date.
    /// </summary>
    public static bool TryParse(string? raw, out DateTime? value, out string? error)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            value = null;
            error = null;
            return true;
        }

        if (DateTime.TryParseExact(raw.Trim(), ["yyyy-MM-dd", "d/M/yyyy", "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy"],
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            && parsed.Year >= 1900)
        {
            value = parsed;
            error = null;
            return true;
        }

        value = null;
        error = InvalidDateMessage;
        return false;
    }
}
