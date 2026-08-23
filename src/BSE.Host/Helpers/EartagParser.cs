using System.Text.RegularExpressions;

namespace BSE.Host.Helpers;

/// <summary>
/// ITD eartag validation and parsing logic ported from the legacy BSE application.
///
/// The DB stores eartag data split across three columns:
///   EartagCountry  VARCHAR(4)   – country prefix, e.g. "UK", "GB", "826"
///   EartagHerdmark VARCHAR(8)   – herd identifier component
///   Eartag         VARCHAR(25)  – individual animal ID
///
/// This parser accepts a single user-entered string, determines its format, validates it,
/// and produces the three split components ready for persistence.
/// </summary>
public static class EartagParser
{
    // EU country codes whose two-letter prefix coincides with valid legacy British
    // pre-Barimo herdmark prefixes.  Such eartags must be entered with a leading
    // '-' or '*' to signal "no country code".
    private static readonly HashSet<string> LegacyAmbiguousPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "AT", "BE", "DE", "DK", "EL", "ES", "FR", "IE", "LU", "NL", "PT", "SE"
    };

    // Two-letter ISO 3166-1 alpha-2 country codes used as eartag country prefixes.
    private static readonly HashSet<string> KnownCountryCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "UK", "GB", "IE", "FR", "DE", "BE", "NL", "LU", "DK", "AT", "SE", "FI",
        "PT", "ES", "IT", "GR", "EL", "CY", "MT", "EE", "LV", "LT", "PL", "CZ",
        "SK", "HU", "SI", "RO", "BG", "HR"
    };

    /// <summary>
    /// Validates and parses a raw eartag string entered by the user.
    /// Returns a <see cref="EartagParseResult"/> with the three split components
    /// and a validation error message (null when valid).
    /// </summary>
    public static EartagParseResult Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return EartagParseResult.Empty;

        var input = raw.Trim();

        // ── Legacy pre-Barimo British eartag (prefixed with '-' or '*') ──────────
        if (input.StartsWith('-') || input.StartsWith('*'))
        {
            var body = input[1..];
            if (string.IsNullOrWhiteSpace(body))
                return EartagParseResult.Invalid("Enter an eartag value after the '-' or '*' prefix.");

            // Must contain a delimiter between herdmark and individual ID
            var preBarimoSplit = SplitOnDelimiter(body);
            if (preBarimoSplit is null)
                return EartagParseResult.Invalid(
                    "Pre-Barimo eartag must contain a '-' delimiter between herdmark and individual animal ID. " +
                    $"For example: -{body}-12345");

            return new EartagParseResult(
                EartagCountry: null,
                EartagHerdmark: preBarimoSplit.Value.herdmark,
                Eartag: preBarimoSplit.Value.animalId,
                ErrorMessage: null);
        }

        // ── ISO numeric format: 3 digits + separator + 6-digit herd + 5-digit ID ─
        // Pattern: [3 numeric][0|1|2|_|space][Hn6][An5]  e.g. "826 703412 70001"
        var isoNumeric = TryParseIsoNumeric(input);
        if (isoNumeric is not null)
            return isoNumeric;

        // ── ISO alpha-numeric format: 2 alpha + separator + 6-digit herd + 5-digit ID ─
        // Pattern: [2 alpha][0|1|2|_|space][Hn6][An5]  e.g. "GB0 703412 70001"
        var isoAlpha = TryParseIsoAlpha(input);
        if (isoAlpha is not null)
            return isoAlpha;

        // ── Barimo / UK 12-digit numeric: country(2) + 10 digits ─────────────────
        // e.g. "UK123456789012"  → country=UK, herdmark=123456, eartag=789012 (last 6)
        // Full UK format: UK + 6 herd digits + 6 animal digits
        var barimo = TryParseBarimo(input);
        if (barimo is not null)
            return barimo;

        // ── Identifiable non-numeric (herdmark-delimiter-animalId) ───────────────
        // e.g. "AB123-234"
        if (input.Length <= 25)
        {
            // Check if it looks like it has a country code prefix
            string? country = null;
            string body = input;

            if (input.Length >= 2 && IsAlpha(input[..2]))
            {
                var possibleCountry = input[..2].ToUpperInvariant();
                if (KnownCountryCodes.Contains(possibleCountry))
                {
                    // Reject ambiguous legacy prefixes without '-'/'*' marker
                    if (LegacyAmbiguousPrefixes.Contains(possibleCountry))
                        return EartagParseResult.Invalid(
                            $"Eartags beginning with '{possibleCountry}' may be interpreted as a country code. " +
                            $"If this is a legacy pre-Barimo British eartag with no country code, " +
                            $"prefix it with '-' or '*' (e.g. '-{input}').");

                    country = possibleCountry;
                    body = input[2..];
                }
            }

            var split = SplitOnDelimiter(body);
            if (split is not null)
            {
                return new EartagParseResult(
                    EartagCountry: country,
                    EartagHerdmark: split.Value.herdmark,
                    Eartag: split.Value.animalId,
                    ErrorMessage: null);
            }

            // No delimiter found – check if it's purely numeric (free numeric eartag)
            if (IsNumeric(input))
            {
                return new EartagParseResult(
                    EartagCountry: null,
                    EartagHerdmark: null,
                    Eartag: input,
                    ErrorMessage: null);
            }

            // Non-numeric without delimiter → invalid (cannot determine format)
            return EartagParseResult.Invalid(
                "Unable to determine eartag format. " +
                "For non-numeric eartags, include a '-' delimiter between herdmark and individual animal ID " +
                "(e.g. AB123-234). For numeric eartags, enter digits only.");
        }

        // ── Free-format fallback ─────────────────────────────────────────────────
        return new EartagParseResult(
            EartagCountry: null,
            EartagHerdmark: null,
            Eartag: input,
            ErrorMessage: null);
    }

    /// <summary>
    /// Reconstructs the combined display eartag from the three stored DB components,
    /// matching the legacy application's display format.
    /// </summary>
    public static string Combine(string? eartagCountry, string? eartagHerdmark, string? eartag)
    {
        if (string.IsNullOrWhiteSpace(eartagCountry) && string.IsNullOrWhiteSpace(eartagHerdmark))
            return eartag ?? string.Empty;

        if (string.IsNullOrWhiteSpace(eartagHerdmark))
            return (eartagCountry ?? string.Empty) + (eartag ?? string.Empty);

        // Country + herdmark + '-' + animalId
        return (eartagCountry ?? string.Empty)
             + eartagHerdmark
             + (string.IsNullOrWhiteSpace(eartag) ? string.Empty : "-" + eartag);
    }

    // ── Private helpers ──────────────────────────────────────────────────────────

    // ISO numeric: 3 digits + [0|1|2|_|space] + 6 digits + 5 digits (with optional spaces)
    // e.g. "826 703412 70001" or "8260703412 70001"
    private static EartagParseResult? TryParseIsoNumeric(string input)
    {
        // Strip internal spaces and underscores for matching
        var normalised = Regex.Replace(input, @"[\s_]", "");
        // 3 digit country + separator char position 3 + 6 herd + 5 animal = 15 chars (with sep) or 14
        var m = Regex.Match(normalised, @"^(\d{3})([012]?)(\d{6})(\d{5})$");
        if (!m.Success) return null;

        return new EartagParseResult(
            EartagCountry: m.Groups[1].Value + m.Groups[2].Value,
            EartagHerdmark: m.Groups[3].Value,
            Eartag: m.Groups[4].Value,
            ErrorMessage: null);
    }

    // ISO alpha-numeric: 2 alpha + [0|1|2|_|space] + 6 digits + 5 digits
    // e.g. "GB0 703412 70001"
    private static EartagParseResult? TryParseIsoAlpha(string input)
    {
        var normalised = Regex.Replace(input, @"[\s_]", "");
        var m = Regex.Match(normalised, @"^([A-Za-z]{2})([012]?)(\d{6})(\d{5})$");
        if (!m.Success) return null;

        return new EartagParseResult(
            EartagCountry: m.Groups[1].Value.ToUpperInvariant() + m.Groups[2].Value,
            EartagHerdmark: m.Groups[3].Value,
            Eartag: m.Groups[4].Value,
            ErrorMessage: null);
    }

    // Barimo UK format: 2-letter country code + exactly 12 digits → split 6+6
    // e.g. "UK123456789012" → country=UK, herdmark=123456, eartag=789012
    private static EartagParseResult? TryParseBarimo(string input)
    {
        var m = Regex.Match(input, @"^([A-Za-z]{2})(\d{6})(\d{6})$");
        if (!m.Success) return null;

        var country = m.Groups[1].Value.ToUpperInvariant();
        // Ambiguous prefix check – Barimo format with country code is unambiguous
        return new EartagParseResult(
            EartagCountry: country,
            EartagHerdmark: m.Groups[2].Value,
            Eartag: m.Groups[3].Value,
            ErrorMessage: null);
    }

    private static (string herdmark, string animalId)? SplitOnDelimiter(string value)
    {
        var idx = value.IndexOf('-');
        if (idx > 0 && idx < value.Length - 1)
        {
            return (value[..idx], value[(idx + 1)..]);
        }
        return null;
    }

    private static bool IsAlpha(string s) =>
        s.All(c => char.IsLetter(c));

    private static bool IsNumeric(string s) =>
        s.All(c => char.IsDigit(c));
}

/// <summary>
/// Result of an eartag parse operation.
/// When <see cref="IsValid"/> is true, the three split components are populated.
/// When false, <see cref="ErrorMessage"/> describes the validation failure.
/// </summary>
public sealed record EartagParseResult(
    string? EartagCountry,
    string? EartagHerdmark,
    string? Eartag,
    string? ErrorMessage)
{
    public bool IsValid => ErrorMessage is null;
    public bool IsEmpty => EartagCountry is null && EartagHerdmark is null && Eartag is null && ErrorMessage is null;

    public static EartagParseResult Empty { get; } =
        new(EartagCountry: null, EartagHerdmark: null, Eartag: null, ErrorMessage: null);

    public static EartagParseResult Invalid(string message) =>
        new(EartagCountry: null, EartagHerdmark: null, Eartag: null, ErrorMessage: message);
}
