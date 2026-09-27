using System.Text.RegularExpressions;

namespace BSE.SharedKernel;

/// <summary>
/// Faithful C# port of legacy BSELib.EartagValidation (Eartag.vb + EartagFormat/*.vb) covering
/// the UK-prefixed formats (GB/Northern Ireland/Isle of Man/Guernsey/Jersey, numeric and
/// alpha-numeric) and the no-country-code formats (Free format, Pre-BARIMO) — the formats
/// reachable when creating a non-GB case for a Channel Islands/Isle of Man farm.
/// EC and ISO country-code eartag formats are NOT ported (not realistically entered on this
/// page); those inputs are only checked for being non-blank, matching legacy's base
/// EartagFormatBase.Validate() no-op.
/// </summary>
public static class EartagValidator
{
    // Mid(HerdComponent, 1, 2) allow-list from GBNumericEartagFormat.vb.
    private static readonly string[] UkGeographicCodes =
    {
        "58","59","22","23","70","71","72","73","10","11","74","75","24","25","36","37","56","57",
        "32","33","50","51","52","53","12","13","20","21","14","15","54","55","18","19","28","29",
        "26","27","16","17","34","35","38","39","30","31"
    };

    // NIEartagFormat.vb electoral code allow-list.
    private static readonly string[] NiElectoralCodes =
    {
        "18","20","21","25","30","12","16","17","14","15","49","50","51","56","58","59","61","45",
        "46","47","48","02","04","06","10","52","53","54","55","24","27","40","41","42","43","31",
        "33","35","37","38","39","57","63","64","65","66"
    };

    // NIAlphaNumericEartagFormat.vb check-digit alphabet (modulus 23).
    private static readonly char[] NiCheckCharacters =
        ['A','B','C','D','E','F','H','I','K','L','M','N','O','P','R','S','T','U','V','W','X','Y','Z'];

    /// <summary>Returns null when valid, otherwise the legacy validation error message.</summary>
    public static string? Validate(string? countryCode, string? herdComponent, string? animalComponent)
    {
        var country = (countryCode ?? string.Empty).Trim().ToUpperInvariant();
        var herd = (herdComponent ?? string.Empty).Trim().ToUpperInvariant();
        var animal = (animalComponent ?? string.Empty).Trim().ToUpperInvariant();

        if (country.Length == 0)
            return herd.Length == 0 ? ValidateFree(animal) : ValidatePreBarimo(herd, animal);

        if (country == "UK")
            return ValidateUk(herd, animal);

        // EC/ISO country-code formats not ported — accept as long as the caller-level
        // "at least one part entered" check already passed.
        return null;
    }

    // ── No country (NoCountryEartag.vb) ─────────────────────────────────────

    private static string? ValidateFree(string animal)
    {
        if (animal.Length is 0 or > 22)
            return "Animal component is invalid: It should contain 1 to 22 characters";
        return null;
    }

    private static string? ValidatePreBarimo(string herd, string animal)
    {
        if (!Regex.IsMatch(herd, "^[A-Z]{1,2}[0-9]{1,4}$"))
            return "Herd component is invalid: It should contain 1 or 2 uppercase alphabetical characters followed by 1 to 4 numeric characters";
        if (herd.Length >= 3 && herd[2..] == "0")
            return "Herd component is invalid: All digits in position 3 onwards are zero";
        if (!Regex.IsMatch(animal, "^[0-9]{1,5}[A-Z]{0,1}$"))
            return "Animal component is invalid: It should contain 1 to 5 numeric characters, optionally followed by 1 uppercase character";
        if (animal == "0")
            return "The animal component is set to zero";
        return null;
    }

    // ── UK dispatch (mirrors UKEartag.GetFormat) ────────────────────────────

    private static string? ValidateUk(string herd, string animal)
    {
        var herdNumeric = herd.Length > 0 && herd.All(char.IsDigit);
        var animalNumeric = animal.Length > 0 && animal.All(char.IsDigit);

        if (herdNumeric && animalNumeric)
        {
            if (herd.StartsWith('9'))
                return ValidateNiNumeric(herd, animal);
            if (herd.StartsWith("01"))
                return ValidateIsleOfManNumeric(ReformatNumericHerd(herd), ReformatNumericAnimal(animal));
            if (herd.StartsWith("02"))
                return ValidateGuernseyNumeric(ReformatNumericHerd(herd), ReformatNumericAnimal(animal));
            if (herd.StartsWith("03"))
                return ValidateJerseyNumeric(ReformatNumericHerd(herd), ReformatNumericAnimal(animal));
            return ValidateGbNumeric(ReformatNumericHerd(herd), ReformatNumericAnimal(animal));
        }

        if (herdNumeric)
            return ValidateNiAlphaNumeric(herd, animal);

        if (herd.StartsWith("MN"))
            return ValidateIsleOfManAlphaNumeric(ReformatIsleOfManAlphaHerd(herd), ReformatAlphaAnimal(animal));
        if (herd.StartsWith("GY"))
            return ValidateGuernseyAlphaNumeric(herd, ReformatAlphaAnimal(animal)); // Guernsey overrides herd reformat to a no-op
        if (herd.StartsWith("JY"))
            return ValidateJerseyAlphaNumeric(ReformatAlphaHerd(herd), ReformatAlphaAnimal(animal));
        return ValidateGbAlphaNumeric(ReformatAlphaHerd(herd), ReformatAlphaAnimal(animal));
    }

    // ── Shared reformatting (UKNonNINumericEartagFormat / UKNonNIAlphaNumericEartagFormat) ──

    private static string ReformatNumericHerd(string herd)
    {
        var partA = herd.Length >= 2 ? herd[..2] : herd;
        var partB = herd.Length > 2 ? herd[2..] : string.Empty;
        return partA + partB.PadLeft(4, '0');
    }

    private static string ReformatNumericAnimal(string animal)
    {
        var partA = animal.Length >= 1 ? animal[..1] : string.Empty;
        var partB = animal.Length > 1 ? animal[1..] : string.Empty;
        return partA + partB.PadLeft(5, '0');
    }

    private static string ReformatAlphaHerd(string herd)
    {
        if (Regex.IsMatch(herd.Length >= 2 ? herd[..2] : herd, "^[A-Z]{2}$"))
        {
            var partA = herd[..2];
            var partB = herd.Length > 2 ? herd[2..] : string.Empty;
            return partA + partB.PadLeft(4, '0');
        }
        else
        {
            var partA = herd.Length >= 1 ? herd[..1] : string.Empty;
            var partB = herd.Length > 1 ? herd[1..] : string.Empty;
            return partA + partB.PadLeft(4, '0');
        }
    }

    private static string ReformatIsleOfManAlphaHerd(string herd)
    {
        var partA = herd.Length >= 2 ? herd[..2] : herd;
        var partB = herd.Length > 2 ? herd[2..] : string.Empty;
        return partA + partB.PadLeft(3, '0');
    }

    private static string ReformatAlphaAnimal(string animal)
    {
        if (Regex.IsMatch(animal, "^[A-Z]{1}[0-9]{1,5}[A-Z]{1}$"))
            return animal[..1] + animal[1..].PadLeft(6, '0');
        if (Regex.IsMatch(animal, "^[A-Z]{1}[0-9]{1,5}$"))
            return animal[..1] + animal[1..].PadLeft(5, '0');
        if (Regex.IsMatch(animal, "^[0-9]{1,5}[A-Z]{1}$"))
            return animal.PadLeft(6, '0');
        return animal.Length < 5 ? animal.PadLeft(5, '0') : animal;
    }

    // ── GB (GBNumericEartagFormat.vb / GBAlphaNumericEartagFormat.vb) ───────

    private static string? ValidateGbNumeric(string herd, string animal)
    {
        if (!Regex.IsMatch(herd, @"^\d{3,6}$"))
            return "Herd component is invalid: It should contain 3 to 6 numerical digits";
        if (!UkGeographicCodes.Contains(herd[..2]))
            return "Herd component is invalid: UK geographic code not recognised";
        if (long.Parse(herd[2..]) <= 0)
            return "Herd component is invalid: Digits after the geographic code are all zero";
        if (!Regex.IsMatch(animal, @"^\d{2,6}$"))
            return "Animal component is invalid: It should contain 2 to 6 numerical digits";

        var modulus = CalculateModulus(herd + animal[1..], 7);
        if (animal[..1] != (modulus + 1).ToString())
            return "Eartag is invalid: The eartag checksum failed";
        if (long.Parse(animal[1..]) == 0)
            return "Animal component is invalid: All digits in position 2 onwards are zero";

        return null;
    }

    private static string? ValidateGbAlphaNumeric(string herd, string animal)
    {
        const string invalidLastChars = "IOPRUX";

        if (!Regex.IsMatch(herd, "^[A-Z]{1,2}[0-9]{1,4}$"))
            return "Herd component is not valid: It should consist of 1 or 2 alphabetical characters followed by 1 to 4 numerical digits";
        var herdDigits = new string(herd.SkipWhile(char.IsLetter).ToArray());
        if (herdDigits.Length == 0 || long.Parse(herdDigits) <= 0)
            return "Herd component is invalid: All digits in position 2 onwards are zero";
        if (!Regex.IsMatch(animal, "^[A-Z]{0,1}[0-9]{1,5}[A-Z]{0,1}$"))
            return "Animal component is invalid: It should consist of an optional alphabetical character, then 1 to 5 numerical digits, then an optional alphabetical character";
        if (Regex.IsMatch(animal[..1], "^[A-Z]{1}$") && animal[..1] != "X" && animal[..1] != "R")
            return "Animal component is invalid: The first character must be numeric, 'X' or 'R'";
        if (invalidLastChars.Contains(animal[^1]))
            return "Animal component is invalid: The last character cannot be I, O, P, R, U or X";
        if (Regex.IsMatch(animal, "^[A-Z]{0,1}[0]{1,5}[A-Z]{0,1}$"))
            return "Animal component is invalid: The numerical part contains only zeros";

        return null;
    }

    // ── Northern Ireland (NINumericEartagFormat.vb / NIAlphaNumericEartagFormat.vb) ──

    private static string? ValidateNiNumeric(string herd, string animal)
    {
        if (!Regex.IsMatch(herd, "^9[0-9]{3,6}$"))
            return "Herd component is invalid: It should consist of '9' followed by 3 to 6 numerical digits";
        if (long.Parse(herd[3..]) == 0)
            return "Herd component is invalid: All digits in position 4 onwards are zero";
        if (!NiElectoralCodes.Contains(herd.Substring(1, 2)))
            return "Herd component is invalid: NI Electoral Code not recognised";
        if (!Regex.IsMatch(animal, @"^\d{2,5}$"))
            return "Animal component is invalid: It should consist of 2 to 5 numerical digits";
        if (!IsNiCheckDigitFormat10(herd, animal))
            return "Eartag is invalid: The eartag checksum failed";
        if (animal[..^1] == "0")
            return "Animal component is invalid: If it contains 2 characters, the first should not be zero";

        return null;
    }

    private static bool IsNiCheckDigitFormat10(string herd, string animal)
    {
        var herdMark = herd[..3];
        var herdNumber = NiPadZero(herd[3..]);
        var animalNumber = NiPadZero(animal[..^1]);
        var modulus = CalculateModulus(herdMark + herdNumber + animalNumber, 7) + 1;
        return modulus.ToString() == animal[^1..];
    }

    private static string? ValidateNiAlphaNumeric(string herd, string animal)
    {
        if (!Regex.IsMatch(herd, @"^\d{3,6}$"))
            return "Herd component is invalid: It should consist of 3 to 6 numerical digits";
        if (!NiElectoralCodes.Contains(herd[..2]))
            return "Herd component is invalid: NI Electoral Code not recognised";
        if (long.Parse(herd[2..]) == 0)
            return "Herd component is invalid: All digits in position 3 onwards are zero";
        if (!Regex.IsMatch(animal, "^[0-9]{1,4}[A-Z]{1}$"))
            return "Animal component is invalid: It should consist of 1 to 4 numerical digits followed by one uppercase character";
        if (!IsNiCheckDigitFormat9(herd, animal))
            return "Eartag is invalid: The eartag checksum failed";
        if (animal[..^1] == "0")
            return "Animal component is invalid: If it contains 2 characters, the first should not be zero";

        return null;
    }

    private static bool IsNiCheckDigitFormat9(string herd, string animal)
    {
        var electoralId = herd[..2];
        var herdNumber = NiPadZero(herd[2..]);
        var animalNumber = animal[..^1];
        var checkNumber = double.Parse(electoralId + herdNumber) * 10000 + int.Parse(animalNumber);
        var modulus = CalculateModulus(checkNumber.ToString("F0"), 23);
        return animal[^1] == NiCheckCharacters[modulus];
    }

    private static string NiPadZero(string part) => part.Length < 4 ? part.PadLeft(4, '0') : part;

    // ── Isle of Man (IsleOfManNumericEartagFormat.vb / IsleOfManAlphaNumericEartagFormat.vb) ──

    private static string? ValidateIsleOfManNumeric(string herd, string animal)
    {
        if (!Regex.IsMatch(herd, "^01[0-9]{1,4}$"))
            return "Herd component invalid: It should consist of 3 to 6 numerical digits, starting with '01'";
        if (long.Parse(herd[2..]) <= 0)
            return "Herd component is invalid: All digits in position 3 onwards are zero";
        if (!Regex.IsMatch(animal, @"^\d{2,6}$"))
            return "Animal component is invalid: It should contain 2 to 6 numerical digits";

        var modulus = CalculateModulus(herd + animal[1..], 7);
        if (animal[..1] != (modulus + 1).ToString())
            return "Eartag is invalid: The eartag checksum failed";
        if (long.Parse(animal[1..]) == 0)
            return "Animal component is invalid: All digits in position 2 onwards are zero";

        return null;
    }

    private static string? ValidateIsleOfManAlphaNumeric(string herd, string animal)
    {
        if (!Regex.IsMatch(herd, "^MN[0-9]{1,3}$"))
            return "Herd component is invalid: It should consist of 'MN' followed by 1 to 3 numerical digits";
        if (long.Parse(herd[2..]) <= 0)
            return "Herd component is invalid: All digits in position 3 onwards are zero";
        if (!Regex.IsMatch(animal, @"^\d{1,5}$"))
            return "Animal component is invalid: It should consist of 1 to 5 numerical digits";
        if (Regex.IsMatch(animal, "^[A-Z]{0,1}[0]{1,5}[A-Z]{0,1}$"))
            return "Animal component is invalid: The numerical part contains only zeros";

        return null;
    }

    // ── Guernsey (GuernseyNumericEartagFormat.vb / GuernseyAlphaNumericEartagFormat.vb) ──

    private static string? ValidateGuernseyNumeric(string herd, string animal)
    {
        if (!Regex.IsMatch(herd, "^02[0-9]{1,4}$"))
            return "Herd component is invalid: It should consist of 3 to 6 numerical digits, starting with '01'";
        if (long.Parse(herd[2..]) <= 0)
            return "Herd component is invalid: All digits in position 3 onwards are zero";
        if (!Regex.IsMatch(animal, @"^\d{2,6}$"))
            return "Animal component is invalid: It should consist of 2 to 6 numerical digits";

        var modulus = CalculateModulus(herd + animal[1..], 7);
        if (animal[..1] != (modulus + 1).ToString())
            return "Eartag is invalid: The eartag checksum failed";
        if (long.Parse(animal[1..]) == 0)
            return "Animal component is invalid: All digits in position 2 onwards are zero";

        return null;
    }

    private static string? ValidateGuernseyAlphaNumeric(string herd, string animal)
    {
        if (!Regex.IsMatch(herd, "^GY[0-9]{1}$"))
            return "Herd component is invalid: It should consist of 'GY' followed by 1 numeric digit";
        if (herd[2..] == "0")
            return "Herd component is invalid: Digit at position 3 should not be zero";
        if (!Regex.IsMatch(animal, @"^\d{1,5}$"))
            return "Animal component is invalid: It should consist of 1 to 5 numerical digits";
        if (Regex.IsMatch(animal, "^[A-Z]{0,1}[0]{1,5}[A-Z]{0,1}$"))
            return "Animal component is invalid: The numerical part contains only zeros";

        return null;
    }

    // ── Jersey (JerseyNumericEartagFormat.vb / JerseyAlphaNumericEartagFormat.vb) ──

    private static string? ValidateJerseyNumeric(string herd, string animal)
    {
        if (!Regex.IsMatch(herd, "^03[0-9]{1,4}$"))
            return "Herd component is invalid: It should consist of 3 to 6 numerical digits, starting with '03'";
        if (long.Parse(herd[2..]) <= 0)
            return "Herd component is invalid: All digits in position 3 onwards are zero";
        if (!Regex.IsMatch(animal, @"^\d{2,6}$"))
            return "Animal component is invalid: It should consist of 2 to 6 numerical digits";

        var modulus = CalculateModulus(herd + animal[1..], 7);
        if (animal[..1] != (modulus + 1).ToString())
            return "Eartag is invalid: The eartag checksum failed";
        if (long.Parse(animal[1..]) == 0)
            return "Animal component is invalid: All digits in position 2 onwards are zero";

        return null;
    }

    private static string? ValidateJerseyAlphaNumeric(string herd, string animal)
    {
        if (!Regex.IsMatch(herd, "^JY[0-9]{1,4}$"))
            return "Herd component is invalid: It should consist of 'JY' followed by 1 to 4 numeric digits";
        if (long.Parse(herd[2..]) <= 0)
            return "Herd component is invalid: All digits in position 3 onwards are zero";
        if (!Regex.IsMatch(animal, @"^\d{1,5}$"))
            return "Animal component is invalid: It should consist of 1 to 5 numerical digits";
        if (Regex.IsMatch(animal, "^[A-Z]{0,1}[0]{1,5}[A-Z]{0,1}$"))
            return "Animal component is invalid: The numerical part contains only zeros";

        return null;
    }

    // Mirrors clsFormat.CalculateModulus: Int(numer) - denom * Fix(numer/denom).
    private static int CalculateModulus(string numerator, int denominator)
        => (int)(long.Parse(numerator) % denominator);
}
