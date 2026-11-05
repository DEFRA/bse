namespace BSE.SharedKernel.Tests;

public class EartagValidatorBranchTests
{
    private const string NiChars = "ABCDEFHIKLMNOPRSTUVWXYZ";
    private const string AnimalZeroTail = "Animal component is invalid: All digits in position 2 onwards are zero";
    private const string HerdZeroTail = "Herd component is invalid: All digits in position 3 onwards are zero";

    private static string Check7(string herd, string animalBody) =>
        ((long.Parse(herd + animalBody) % 7) + 1).ToString();

    [Theory]
    [InlineData("", "AB0", "1", HerdZeroTail)]
    [InlineData("UK", "A12", "123", null)]
    [InlineData("UK", "AB12", "X123A", null)]
    [InlineData("UK", "1234567", "12345", "Herd component is invalid: It should contain 3 to 6 numerical digits")]
    [InlineData("UK", "581234", "12345678", "Animal component is invalid: It should contain 2 to 6 numerical digits")]
    [InlineData("UK", "ABC12", "123", "Herd component is not valid: It should consist of 1 or 2 alphabetical characters followed by 1 to 4 numerical digits")]
    [InlineData("UK", "AB12", "ABC", "Animal component is invalid: It should consist of an optional alphabetical character, then 1 to 5 numerical digits, then an optional alphabetical character")]
    [InlineData("UK", "91", "123", "Herd component is invalid: It should consist of '9' followed by 3 to 6 numerical digits")]
    [InlineData("UK", "9180000", "5671", "Herd component is invalid: All digits in position 4 onwards are zero")]
    [InlineData("UK", "9181234", "1", "Animal component is invalid: It should consist of 2 to 5 numerical digits")]
    [InlineData("UK", "12", "5A", "Herd component is invalid: It should consist of 3 to 6 numerical digits")]
    [InlineData("UK", "180", "5A", HerdZeroTail)]
    [InlineData("UK", "181234", "5a", "Animal component is invalid: It should consist of 1 to 4 numerical digits followed by one uppercase character")]
    [InlineData("UK", "0112345", "12345", "Herd component invalid: It should consist of 3 to 6 numerical digits, starting with '01'")]
    [InlineData("UK", "010012", "12345678", "Animal component is invalid: It should contain 2 to 6 numerical digits")]
    [InlineData("UK", "MN1234", "5", "Herd component is invalid: It should consist of 'MN' followed by 1 to 3 numerical digits")]
    [InlineData("UK", "0212345", "12345", "Herd component is invalid: It should consist of 3 to 6 numerical digits, starting with '01'")]
    [InlineData("UK", "020012", "12345678", "Animal component is invalid: It should consist of 2 to 6 numerical digits")]
    [InlineData("UK", "GY12", "5", "Herd component is invalid: It should consist of 'GY' followed by 1 numeric digit")]
    [InlineData("UK", "0312345", "12345", "Herd component is invalid: It should consist of 3 to 6 numerical digits, starting with '03'")]
    [InlineData("UK", "030012", "12345678", "Animal component is invalid: It should consist of 2 to 6 numerical digits")]
    [InlineData("UK", "JY12345", "5", "Herd component is invalid: It should consist of 'JY' followed by 1 to 4 numeric digits")]
    public void Validate_RejectsOrReformats(string country, string herd, string animal, string? expected)
    {
        EartagValidator.Validate(country, herd, animal).Should().Be(expected);
    }

    [Theory]
    [InlineData("010012")]
    [InlineData("020012")]
    [InlineData("030012")]
    public void Validate_CrownDependencyNumeric_RejectsAllZeroAnimalBody(string herd)
    {
        EartagValidator.Validate("UK", herd, Check7(herd, "00000") + "00000").Should().Be(AnimalZeroTail);
    }

    [Fact]
    public void Validate_NiNumeric_RejectsLeadingZeroAnimal()
    {
        var digit = ((long.Parse("9181234" + "0000") % 7) + 1).ToString();

        EartagValidator.Validate("UK", "9181234", "0" + digit)
            .Should().Be("Animal component is invalid: If it contains 2 characters, the first should not be zero");
    }

    [Fact]
    public void Validate_NiAlphaNumeric_RejectsLeadingZeroAnimal()
    {
        var check = NiChars[(int)((long.Parse("181234") * 10000) % 23)];

        EartagValidator.Validate("UK", "181234", "0" + check)
            .Should().Be("Animal component is invalid: If it contains 2 characters, the first should not be zero");
    }
}
