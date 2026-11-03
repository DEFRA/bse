namespace BSE.SharedKernel.Tests;

public class EartagValidatorFormatTests
{
    private const string NiChars = "ABCDEFHIKLMNOPRSTUVWXYZ";

    private static string Check7(string herd, string animalBody) =>
        ((long.Parse(herd + animalBody) % 7) + 1).ToString();

    [Theory]
    [InlineData("", "", "", "Animal component is invalid: It should contain 1 to 22 characters")]
    [InlineData("", "", "ABC123", null)]
    [InlineData("", "", "12345678901234567890123", "Animal component is invalid: It should contain 1 to 22 characters")]
    [InlineData("X", "", "123", "Country code should be empty for Free format eartags")]
    [InlineData("", "AB12", "123", null)]
    [InlineData("", "ab12", "123", "Herd component is invalid: It should contain 1 or 2 uppercase alphabetical characters followed by 1 to 4 numeric characters")]
    [InlineData("", "AB12", "123456", "Animal component is invalid: It should contain 1 to 5 numeric characters, optionally followed by 1 uppercase character")]
    [InlineData("", "AB12", "0", "The animal component is set to zero")]
    [InlineData("X", "AB12", "1", "Country code should be empty for Pre-BARIMO eartags")]
    public void Validate_NoCountryFormats(string country, string herd, string animal, string? expected)
    {
        EartagValidator.Validate(country, herd, animal).Should().Be(expected);
    }

    [Theory]
    [InlineData("AT", "", "ABC123", null)]
    [InlineData("FR", "", "abc", "Animal component is invalid: It should contain 1 to 12 numerical or uppercase alphabetical characters.")]
    [InlineData("DE", "", "1234567890123", "Animal component is invalid: It should contain 1 to 12 numerical or uppercase alphabetical characters.")]
    public void Validate_EcFormats(string country, string herd, string animal, string? expected)
    {
        EartagValidator.Validate(country, herd, animal).Should().Be(expected);
    }

    [Theory]
    [InlineData("826", "123456", "12345", null)]
    [InlineData("8263", "123456", "12345", "Country component is invalid: It should contain 3 numerical digits followed by 0, 1, 2, _ or space character.")]
    [InlineData("8260", "12345", "12345", "Herd component is invalid: It should contain 6 numerical digits.")]
    [InlineData("8260", "123456", "1234", "Animal component is invalid: It should contain 5 numerical digits.")]
    [InlineData("GBR", "123456", "12345", null)]
    public void Validate_IsoFormats(string country, string herd, string animal, string? expected)
    {
        EartagValidator.Validate(country, herd, animal).Should().Be(expected);
    }

    [Fact]
    public void Validate_NullInputs_AreTreatedAsEmpty()
    {
        EartagValidator.Validate(null, null, null)
            .Should().Be("Animal component is invalid: It should contain 1 to 22 characters");
    }

    [Fact]
    public void Validate_GbNumeric_AcceptsValidChecksum()
    {
        EartagValidator.Validate("UK", "581234", Check7("581234", "12345") + "12345").Should().BeNull();
    }

    [Fact]
    public void Validate_GbNumeric_RejectsUnknownGeographicCode()
    {
        EartagValidator.Validate("UK", "601234", "112345")
            .Should().Be("Herd component is invalid: UK geographic code not recognised");
    }

    [Fact]
    public void Validate_GbNumeric_RejectsZeroHerdSuffix()
    {
        EartagValidator.Validate("UK", "580000", "112345")
            .Should().Be("Herd component is invalid: Digits after the geographic code are all zero");
    }

    [Fact]
    public void Validate_GbNumeric_RejectsBadChecksum()
    {
        var wrong = (((long.Parse("58123412345") % 7) + 1) % 7 + 1).ToString();
        EartagValidator.Validate("UK", "581234", wrong + "12345")
            .Should().Be("Eartag is invalid: The eartag checksum failed");
    }

    [Fact]
    public void Validate_GbNumeric_RejectsAllZeroAnimalBody()
    {
        EartagValidator.Validate("UK", "581234", Check7("581234", "00000") + "00000")
            .Should().Be("Animal component is invalid: All digits in position 2 onwards are zero");
    }

    [Theory]
    [InlineData("010012")]
    [InlineData("020012")]
    [InlineData("030012")]
    public void Validate_CrownDependencyNumeric_AcceptsValidChecksum(string herd)
    {
        EartagValidator.Validate("UK", herd, Check7(herd, "00123") + "00123").Should().BeNull();
    }

    [Theory]
    [InlineData("010012")]
    [InlineData("020012")]
    [InlineData("030012")]
    public void Validate_CrownDependencyNumeric_RejectsBadChecksum(string herd)
    {
        var wrong = ((long.Parse(herd + "00123") % 7 + 1) % 7 + 1).ToString();
        EartagValidator.Validate("UK", herd, wrong + "00123")
            .Should().Be("Eartag is invalid: The eartag checksum failed");
    }

    [Theory]
    [InlineData("010000")]
    [InlineData("020000")]
    [InlineData("030000")]
    public void Validate_CrownDependencyNumeric_RejectsZeroHerdSuffix(string herd)
    {
        EartagValidator.Validate("UK", herd, "100123")
            .Should().Be("Herd component is invalid: All digits in position 3 onwards are zero");
    }

    [Fact]
    public void Validate_NiNumeric_AcceptsValidChecksum()
    {
        const string herd = "9181234";
        var digit = ((long.Parse("918" + "1234" + "0567") % 7) + 1).ToString();
        EartagValidator.Validate("UK", herd, "567" + digit).Should().BeNull();
    }

    [Fact]
    public void Validate_NiNumeric_RejectsUnknownElectoralCode()
    {
        EartagValidator.Validate("UK", "9991234", "5671")
            .Should().Be("Herd component is invalid: NI Electoral Code not recognised");
    }

    [Fact]
    public void Validate_NiNumeric_RejectsBadChecksum()
    {
        var good = (long.Parse("918" + "1234" + "0567") % 7) + 1;
        var bad = (good % 7) + 1;
        EartagValidator.Validate("UK", "9181234", "567" + bad)
            .Should().Be("Eartag is invalid: The eartag checksum failed");
    }

    [Fact]
    public void Validate_NiAlphaNumeric_AcceptsValidCheckCharacter()
    {
        var modulus = (int)((long.Parse("18" + "1234") * 10000 + 567) % 23);
        EartagValidator.Validate("UK", "181234", "567" + NiChars[modulus]).Should().BeNull();
    }

    [Fact]
    public void Validate_NiAlphaNumeric_RejectsBadCheckCharacter()
    {
        var modulus = (int)((long.Parse("18" + "1234") * 10000 + 567) % 23);
        var wrong = NiChars[(modulus + 1) % NiChars.Length];
        EartagValidator.Validate("UK", "181234", "567" + wrong)
            .Should().Be("Eartag is invalid: The eartag checksum failed");
    }

    [Fact]
    public void Validate_NiAlphaNumeric_RejectsUnknownElectoralCode()
    {
        EartagValidator.Validate("UK", "991234", "567A")
            .Should().Be("Herd component is invalid: NI Electoral Code not recognised");
    }

    [Theory]
    [InlineData("AB12", "X123", null)]
    [InlineData("AB12", "123", null)]
    [InlineData("AB12", "123A", null)]
    [InlineData("AB0", "123", "Herd component is invalid: All digits in position 2 onwards are zero")]
    [InlineData("AB12", "Q123", "Animal component is invalid: The first character must be numeric, 'X' or 'R'")]
    [InlineData("AB12", "123P", "Animal component is invalid: The last character cannot be I, O, P, R, U or X")]
    public void Validate_GbAlphaNumeric(string herd, string animal, string? expected)
    {
        EartagValidator.Validate("UK", herd, animal).Should().Be(expected);
    }

    [Theory]
    [InlineData("MN12", "5", null)]
    [InlineData("MN0", "5", "Herd component is invalid: All digits in position 3 onwards are zero")]
    [InlineData("GY0", "5", "Herd component is invalid: Digit at position 3 should not be zero")]
    [InlineData("JY12", "5", null)]
    [InlineData("JY0", "5", "Herd component is invalid: All digits in position 3 onwards are zero")]
    public void Validate_CrownDependencyAlphaNumeric(string herd, string animal, string? expected)
    {
        EartagValidator.Validate("UK", herd, animal).Should().Be(expected);
    }
}
