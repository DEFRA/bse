namespace BSE.SharedKernel.Tests;

public class EartagValidatorFormatTests
{
    // Finds the check digit the validator accepts so the happy path does not depend on re-implementing the checksum.
    private static string WithAcceptedCheckDigit(string country, string herd, string animalBody, bool checkDigitLast)
    {
        for (var d = 0; d <= 9; d++)
        {
            var animal = checkDigitLast ? animalBody + d : d + animalBody;
            if (EartagValidator.Validate(country, herd, animal) is null)
                return animal;
        }

        throw new InvalidOperationException($"No accepted check digit for {country} {herd} {animalBody}");
    }

    private static string RejectedCheckDigit(string country, string herd, string animalBody, bool checkDigitLast)
    {
        var accepted = WithAcceptedCheckDigit(country, herd, animalBody, checkDigitLast);
        var acceptedDigit = checkDigitLast ? accepted[^1] : accepted[0];
        var wrong = (char)('0' + ((acceptedDigit - '0' + 1) % 10));
        return checkDigitLast ? animalBody + wrong : wrong + animalBody;
    }

    // ── Dispatch / free / pre-BARIMO ─────────────────────────────────────────

    [Theory]
    [InlineData(null, null, null)]
    [InlineData("", "", "")]
    public void Validate_NullsAndBlanks_AreTreatedAsEmptyFreeFormat(string? country, string? herd, string? animal)
    {
        EartagValidator.Validate(country, herd, animal)
            .Should().Be("Animal component is invalid: It should contain 1 to 22 characters");
    }

    [Fact]
    public void Validate_FreeFormat_AcceptsUpToTwentyTwoCharacters()
    {
        EartagValidator.Validate("", "", new string('A', 22)).Should().BeNull();
        EartagValidator.Validate("", "", new string('A', 23))
            .Should().Be("Animal component is invalid: It should contain 1 to 22 characters");
    }

    [Fact]
    public void Validate_UnknownCountryWithoutHerd_RejectsCountry()
    {
        EartagValidator.Validate("XX", "", "123")
            .Should().Be("Country code should be empty for Free format eartags");
    }

    [Fact]
    public void Validate_UnknownCountryWithHerd_RejectsCountryForPreBarimo()
    {
        EartagValidator.Validate("XX", "AB12", "123")
            .Should().Be("Country code should be empty for Pre-BARIMO eartags");
    }

    [Fact]
    public void Validate_PreBarimo_AcceptsValidEartag()
    {
        EartagValidator.Validate("", "AB123", "12345A").Should().BeNull();
    }

    [Theory]
    [InlineData("ab123", "123", "Herd component is invalid: It should contain 1 or 2 uppercase alphabetical characters followed by 1 to 4 numeric characters")]
    [InlineData("AB0", "123", "Herd component is invalid: All digits in position 3 onwards are zero")]
    [InlineData("AB12", "123456", "Animal component is invalid: It should contain 1 to 5 numeric characters, optionally followed by 1 uppercase character")]
    [InlineData("AB12", "0", "The animal component is set to zero")]
    public void Validate_PreBarimo_RejectsInvalidComponents(string herd, string animal, string expected)
    {
        EartagValidator.Validate("", herd, animal).Should().Be(expected);
    }

    // ── EC non-UK ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("FR", "ABC123", null)]
    [InlineData("DE", "123456789012", null)]
    [InlineData("FR", "abc", "Animal component is invalid: It should contain 1 to 12 numerical or uppercase alphabetical characters.")]
    [InlineData("IE", "1234567890123", "Animal component is invalid: It should contain 1 to 12 numerical or uppercase alphabetical characters.")]
    public void Validate_EcCountries_ChecksAnimalComponent(string country, string animal, string? expected)
    {
        EartagValidator.Validate(country, "", animal).Should().Be(expected);
    }

    // ── ISO EID ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("826", "123456", "12345", null)]
    [InlineData("8260", "123456", "12345", null)]
    [InlineData("826_", "123456", "12345", null)]
    [InlineData("GBR", "123456", "12345", null)]
    [InlineData("8263", "123456", "12345", "Country component is invalid: It should contain 3 numerical digits followed by 0, 1, 2, _ or space character.")]
    [InlineData("826", "12345", "12345", "Herd component is invalid: It should contain 6 numerical digits.")]
    [InlineData("826", "123456", "1234", "Animal component is invalid: It should contain 5 numerical digits.")]
    public void Validate_IsoCountryCodes_ChecksAllComponents(string country, string herd, string animal, string? expected)
    {
        EartagValidator.Validate(country, herd, animal).Should().Be(expected);
    }

    // ── UK: GB numeric ───────────────────────────────────────────────────────

    [Fact]
    public void Validate_GbNumeric_AcceptsValidChecksum()
    {
        var animal = WithAcceptedCheckDigit("UK", "580001", "00001", checkDigitLast: false);

        EartagValidator.Validate("UK", "580001", animal).Should().BeNull();
    }

    [Fact]
    public void Validate_GbNumeric_PadsShortHerdAndAnimalBeforeValidating()
    {
        var accepted = WithAcceptedCheckDigit("UK", "580001", "00001", checkDigitLast: false);

        // "5800" + "01" reformats to 58 + 0001; the animal "x1" reformats to x + 00001.
        EartagValidator.Validate("UK", "5801", accepted[0] + "1").Should().BeNull();
    }

    [Fact]
    public void Validate_GbNumeric_RejectsBadChecksum()
    {
        var animal = RejectedCheckDigit("UK", "580001", "00001", checkDigitLast: false);

        EartagValidator.Validate("UK", "580001", animal).Should().Be("Eartag is invalid: The eartag checksum failed");
    }

    [Theory]
    [InlineData("400001", "100001", "Herd component is invalid: UK geographic code not recognised")]
    [InlineData("5", "100001", "Herd component is invalid: Digits after the geographic code are all zero")]
    [InlineData("580001", "123456789", "Animal component is invalid: It should contain 2 to 6 numerical digits")]
    public void Validate_GbNumeric_RejectsInvalidComponents(string herd, string animal, string expected)
    {
        EartagValidator.Validate("UK", herd, animal).Should().Be(expected);
    }

    [Fact]
    public void Validate_GbNumeric_RejectsAnimalWithZeroSerial()
    {
        var results = Enumerable.Range(0, 10)
            .Select(d => EartagValidator.Validate("UK", "580001", d + "00000"))
            .ToList();

        results.Should().Contain("Animal component is invalid: All digits in position 2 onwards are zero");
    }

    // ── UK: GB alpha-numeric ─────────────────────────────────────────────────

    [Theory]
    [InlineData("AB1", "12345", null)]
    [InlineData("A1", "12345", null)]
    [InlineData("X1", "X1234", null)]
    [InlineData("AB1", "A1234", "Animal component is invalid: The first character must be numeric, 'X' or 'R'")]
    [InlineData("AB1", "1234I", "Animal component is invalid: The last character cannot be I, O, P, R, U or X")]
    [InlineData("AB1", "000000", "Animal component is invalid: It should consist of an optional alphabetical character, then 1 to 5 numerical digits, then an optional alphabetical character")]
    [InlineData("AB0", "12345", "Herd component is invalid: All digits in position 2 onwards are zero")]
    [InlineData("ABC1", "12345", "Herd component is not valid: It should consist of 1 or 2 alphabetical characters followed by 1 to 4 numerical digits")]
    public void Validate_GbAlphaNumeric_ChecksHerdAndAnimal(string herd, string animal, string? expected)
    {
        EartagValidator.Validate("UK", herd, animal).Should().Be(expected);
    }

    // ── UK: Northern Ireland ─────────────────────────────────────────────────

    [Fact]
    public void Validate_NiNumeric_AcceptsValidChecksum()
    {
        var animal = WithAcceptedCheckDigit("UK", "918001", "1", checkDigitLast: true);

        EartagValidator.Validate("UK", "918001", animal).Should().BeNull();
    }

    [Fact]
    public void Validate_NiNumeric_RejectsBadChecksum()
    {
        var animal = RejectedCheckDigit("UK", "918001", "1", checkDigitLast: true);

        EartagValidator.Validate("UK", "918001", animal).Should().Be("Eartag is invalid: The eartag checksum failed");
    }

    [Theory]
    [InlineData("9", "11", "Herd component is invalid: It should consist of '9' followed by 3 to 6 numerical digits")]
    [InlineData("990001", "11", "Herd component is invalid: NI Electoral Code not recognised")]
    [InlineData("918000", "11", "Herd component is invalid: All digits in position 4 onwards are zero")]
    [InlineData("918001", "1", "Animal component is invalid: It should consist of 2 to 5 numerical digits")]
    public void Validate_NiNumeric_RejectsInvalidComponents(string herd, string animal, string expected)
    {
        EartagValidator.Validate("UK", herd, animal).Should().Be(expected);
    }

    [Fact]
    public void Validate_NiAlphaNumeric_AcceptsValidCheckCharacter()
    {
        string? accepted = null;
        for (var c = 'A'; c <= 'Z' && accepted is null; c++)
        {
            if (EartagValidator.Validate("UK", "180001", "1" + c) is null)
                accepted = "1" + c;
        }

        accepted.Should().NotBeNull();
    }

    [Theory]
    [InlineData("180001", "1a", "Animal component is invalid: It should consist of 1 to 4 numerical digits followed by one uppercase character")]
    [InlineData("990001", "1A", "Herd component is invalid: NI Electoral Code not recognised")]
    [InlineData("180000", "1A", "Herd component is invalid: All digits in position 3 onwards are zero")]
    [InlineData("18", "1A", "Herd component is invalid: It should consist of 3 to 6 numerical digits")]
    public void Validate_NiAlphaNumeric_RejectsInvalidComponents(string herd, string animal, string expected)
    {
        EartagValidator.Validate("UK", herd, animal).Should().Be(expected);
    }

    [Fact]
    public void Validate_NiAlphaNumeric_RejectsWrongCheckCharacter()
    {
        var results = Enumerable.Range('A', 26)
            .Select(c => EartagValidator.Validate("UK", "180001", "1" + (char)c))
            .ToList();

        results.Should().Contain("Eartag is invalid: The eartag checksum failed");
    }

    // ── UK: Crown Dependencies, numeric ──────────────────────────────────────

    [Theory]
    [InlineData("0101")]
    [InlineData("0201")]
    [InlineData("0301")]
    public void Validate_CrownDependencyNumeric_AcceptsValidChecksum(string herd)
    {
        var animal = WithAcceptedCheckDigit("UK", herd, "00001", checkDigitLast: false);

        EartagValidator.Validate("UK", herd, animal).Should().BeNull();
    }

    [Theory]
    [InlineData("0101")]
    [InlineData("0201")]
    [InlineData("0301")]
    public void Validate_CrownDependencyNumeric_RejectsBadChecksum(string herd)
    {
        var animal = RejectedCheckDigit("UK", herd, "00001", checkDigitLast: false);

        EartagValidator.Validate("UK", herd, animal).Should().Be("Eartag is invalid: The eartag checksum failed");
    }

    [Theory]
    [InlineData("0100", "Herd component is invalid: All digits in position 3 onwards are zero")]
    [InlineData("0200", "Herd component is invalid: All digits in position 3 onwards are zero")]
    [InlineData("0300", "Herd component is invalid: All digits in position 3 onwards are zero")]
    public void Validate_CrownDependencyNumeric_RejectsZeroHerdSerial(string herd, string expected)
    {
        EartagValidator.Validate("UK", herd, "100001").Should().Be(expected);
    }

    [Theory]
    [InlineData("0101")]
    [InlineData("0201")]
    [InlineData("0301")]
    public void Validate_CrownDependencyNumeric_RejectsAnimalWithTooManyDigits(string herd)
    {
        EartagValidator.Validate("UK", herd, "123456789")
            .Should().StartWith("Animal component is invalid:");
    }

    // ── UK: Crown Dependencies, alpha-numeric ────────────────────────────────

    [Theory]
    [InlineData("MN0", "5", "Herd component is invalid: All digits in position 3 onwards are zero")]
    [InlineData("MN1", "123456", "Animal component is invalid: It should consist of 1 to 5 numerical digits")]
    [InlineData("MN1", "5", null)]
    [InlineData("GY0", "5", "Herd component is invalid: Digit at position 3 should not be zero")]
    [InlineData("GY12", "5", "Herd component is invalid: It should consist of 'GY' followed by 1 numeric digit")]
    [InlineData("GY1", "5", null)]
    [InlineData("JY0", "5", "Herd component is invalid: All digits in position 3 onwards are zero")]
    [InlineData("JY1", "5", null)]
    public void Validate_CrownDependencyAlphaNumeric_ChecksHerdAndAnimal(string herd, string animal, string? expected)
    {
        EartagValidator.Validate("UK", herd, animal).Should().Be(expected);
    }
}
