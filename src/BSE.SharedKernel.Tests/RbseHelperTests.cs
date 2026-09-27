namespace BSE.SharedKernel.Tests;

public class RbseHelperTests
{
    [Theory]
    [InlineData("002600001", "00/26/00001")]
    [InlineData("123456789", "12/34/56789")]
    public void Format_NineDigitRaw_ReturnsSlashFormat(string raw, string expected)
    {
        RbseHelper.Format(raw).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567890")]
    public void Format_InvalidLength_ReturnsOriginal(string? raw)
    {
        RbseHelper.Format(raw).Should().Be(raw);
    }

    [Theory]
    [InlineData("2600001", "26/00001")]
    public void FormatDbse_SevenDigitRaw_ReturnsSlashFormat(string raw, string expected)
    {
        RbseHelper.FormatDbse(raw).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("12345678")]
    public void FormatDbse_InvalidLength_ReturnsOriginal(string? raw)
    {
        RbseHelper.FormatDbse(raw).Should().Be(raw);
    }

    [Theory]
    [InlineData("00/26/00001", "002600001")]
    [InlineData("002600001", "002600001")]
    [InlineData(" 00/26/00001 ", "002600001")]
    [InlineData(null, "")]
    [InlineData("", "")]
    public void Normalize_StripsSlashesAndWhitespace(string? input, string expected)
    {
        RbseHelper.Normalize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("002600001", true)]
    [InlineData("00/26/00001", true)]
    [InlineData("12345", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("ABCDEFGHI", false)]
    public void IsValid_ValidatesNineDigitRbse(string? input, bool expected)
    {
        RbseHelper.IsValid(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("002600001", "002600001")]
    [InlineData("00/26/00001", "002600001")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    public void ParseToRaw_HandlesRawAndFullSlashFormats(string? input, string expected)
    {
        RbseHelper.ParseToRaw(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("9/87", "000900087")]
    [InlineData("26/1", "002600001")]
    public void ParseToRaw_HandlesPartialLegacyFormats(string input, string expected)
    {
        RbseHelper.ParseToRaw(input).Should().Be(expected);
    }

    [Fact]
    public void ParseToRaw_UnrecognisedFormat_ReturnsOriginalUnchanged()
    {
        const string input = "not-a-valid-rbse";
        RbseHelper.ParseToRaw(input).Should().Be(input);
    }
}
