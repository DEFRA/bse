namespace BSE.SharedKernel;

/// <summary>Formatting and normalisation helpers for RBSE and DBSE identifiers.</summary>
public static class RbseHelper
{
    /// <summary>
    /// Formats a 9-digit raw RBSE (CCYYNNNNN) as the legacy display format CC/YY/NNNNN.
    /// Returns the original value unchanged if it is null, empty, or not 9 characters long.
    /// Example: "002600001" → "00/26/00001"
    /// </summary>
    public static string? Format(string? raw)
        => raw?.Length == 9 ? $"{raw[..2]}/{raw[2..4]}/{raw[4..]}" : raw;

    /// <summary>
    /// Formats a 7-digit raw DBSE (YYNNNNN) as the legacy display format YY/NNNNN.
    /// Returns the original value unchanged if it is null, empty, or not 7 characters long.
    /// Example: "2600001" → "26/00001"
    /// </summary>
    public static string? FormatDbse(string? raw)
        => raw?.Length == 7 ? $"{raw[..2]}/{raw[2..]}" : raw;

    /// <summary>
    /// Strips slashes and whitespace from user-supplied RBSE input.
    /// Accepts both "00/26/00001" (slash format) and "002600001" (raw 9-digit format).
    /// Returns the raw 9-digit form ready for database lookup or storage.
    /// </summary>
    public static string Normalize(string? input)
        => (input ?? "").Replace("/", "").Trim();

    /// <summary>
    /// Full legacy-compatible parse matching RBSE.ascx.vb FormatRBSE.
    /// Accepts partial slash-format input and auto-pads missing digits with zeros.
    /// Examples: "9/87" → "000900087", "26/1" → "002600001", "002600001" → "002600001".
    /// Returns the raw 9-digit form for database lookup.
    /// </summary>
    public static string ParseToRaw(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var s = input.Trim();

        // Already raw 9 digits — use directly
        if (s.Length == 9 && !s.Contains('/'))
            return s;

        return ApplyLegacyPaddingFormat(s).Replace("/", "");
    }

    /// <summary>
    /// Returns true when the value is a valid 9-digit RBSE after normalisation.
    /// </summary>
    public static bool IsValid(string? input)
    {
        var raw = Normalize(input);
        return raw.Length == 9 && raw.All(char.IsDigit);
    }

    // Applies legacy RBSE.ascx.vb FormatRBSE logic: zero-pads each part to CC/YY/NNNNN.
    // iFirstDash and iSecondDash use 1-based positions to match the original VB InStr behaviour.
    private static string ApplyLegacyPaddingFormat(string s)
    {
        // If 9 chars with no slash, insert slashes at positions 2 and 4
        if (!s.Contains('/') && s.Length == 9)
            return $"{s[..2]}/{s[2..4]}/{s[4..]}";

        // Must match at least one slash with surrounding digits
        if (!System.Text.RegularExpressions.Regex.IsMatch(s, @"^[0-9]{0,2}/?[0-9X]{0,2}/[0-9]{0,5}$"))
            return s;

        int iFirstDash = s.IndexOf('/') + 1; // 1-based, mirrors VB InStr
        int secondIdx  = s.IndexOf('/', iFirstDash); // 0-based search starting after first slash
        int iSecondDash = secondIdx < 0 ? 0 : secondIdx + 1; // 1-based; 0 means only one slash

        string sPartOne, sPartTwo;

        if (iSecondDash == 0) // Only one slash
        {
            sPartOne = iFirstDash <= 3 ? "00"
                     : iFirstDash == 4 ? "0" + s[..1]
                     : s[..2];
            iSecondDash = iFirstDash;
        }
        else // Two slashes
        {
            sPartOne = iFirstDash >= 3
                ? s.Substring(iFirstDash - 3, 2)   // Mid$(s, iFirstDash-2, 2)
                : "0" + s[..1];
        }

        if (iSecondDash >= 3)
        {
            sPartTwo = iSecondDash - iFirstDash == 2
                ? "0" + s.Substring(iSecondDash - 2, 1)    // Mid$(s, iSecondDash-1, 1)
                : s.Substring(iSecondDash - 3, 2);           // Mid$(s, iSecondDash-2, 2)
        }
        else
        {
            sPartTwo = "0" + s[..1];
        }

        // Right-justify sequence in 5 digits; Mid$(s, iSecondDash+1, Len(s)-iSecondDash)
        var afterSecond = s.Substring(iSecondDash, s.Length - iSecondDash);
        var sPartThree  = ("00000" + afterSecond)[^5..];

        return sPartOne + "/" + sPartTwo + "/" + sPartThree;
    }
}
