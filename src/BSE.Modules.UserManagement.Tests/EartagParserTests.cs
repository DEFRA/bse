using BSE.Host.Helpers;
using FluentAssertions;
using Xunit;

namespace BSE.Modules.UserManagement.Tests;

public class EartagParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_BlankInput_ReturnsEmpty(string? raw)
    {
        var result = EartagParser.Parse(raw);

        result.IsEmpty.Should().BeTrue();
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("-")]
    [InlineData("*  ")]
    public void Parse_PrefixOnly_IsInvalid(string raw)
    {
        var result = EartagParser.Parse(raw);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("after the '-' or '*' prefix");
    }

    [Fact]
    public void Parse_PreBarimoWithoutDelimiter_IsInvalid()
    {
        var result = EartagParser.Parse("-AB123");

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("delimiter");
    }

    [Theory]
    [InlineData("-AB123-45", "AB123", "45")]
    [InlineData("*XY9-001", "XY9", "001")]
    public void Parse_PreBarimoWithDelimiter_SplitsWithoutCountry(string raw, string herd, string animal)
    {
        var result = EartagParser.Parse(raw);

        result.IsValid.Should().BeTrue();
        result.EartagCountry.Should().BeNull();
        result.EartagHerdmark.Should().Be(herd);
        result.Eartag.Should().Be(animal);
    }

    [Theory]
    [InlineData("826 703412 70001", "826", "703412", "70001")]
    [InlineData("8260703412 70001", "8260", "703412", "70001")]
    [InlineData("826_703412_70001", "826", "703412", "70001")]
    public void Parse_IsoNumeric_SplitsIntoComponents(string raw, string country, string herd, string animal)
    {
        var result = EartagParser.Parse(raw);

        result.EartagCountry.Should().Be(country);
        result.EartagHerdmark.Should().Be(herd);
        result.Eartag.Should().Be(animal);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("GB0 703412 70001", "GB0", "703412", "70001")]
    [InlineData("gb703412 70001", "GB", "703412", "70001")]
    public void Parse_IsoAlpha_UppercasesCountry(string raw, string country, string herd, string animal)
    {
        var result = EartagParser.Parse(raw);

        result.EartagCountry.Should().Be(country);
        result.EartagHerdmark.Should().Be(herd);
        result.Eartag.Should().Be(animal);
    }

    [Fact]
    public void Parse_BarimoTwelveDigits_SplitsSixAndSix()
    {
        var result = EartagParser.Parse("uk923456789012");

        result.EartagCountry.Should().Be("UK");
        result.EartagHerdmark.Should().Be("923456");
        result.Eartag.Should().Be("789012");
    }

    [Fact]
    public void Parse_UnknownPrefixWithDelimiter_HasNoCountry()
    {
        var result = EartagParser.Parse("AB123-234");

        result.EartagCountry.Should().BeNull();
        result.EartagHerdmark.Should().Be("AB123");
        result.Eartag.Should().Be("234");
    }

    [Fact]
    public void Parse_KnownCountryPrefixWithDelimiter_ExtractsCountry()
    {
        var result = EartagParser.Parse("UK12-34");

        result.EartagCountry.Should().Be("UK");
        result.EartagHerdmark.Should().Be("12");
        result.Eartag.Should().Be("34");
    }

    [Fact]
    public void Parse_AmbiguousLegacyPrefix_IsRejected()
    {
        var result = EartagParser.Parse("FR12-34");

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("'FR'");
    }

    [Fact]
    public void Parse_PureNumericWithoutDelimiter_IsFreeNumericEartag()
    {
        var result = EartagParser.Parse("12345");

        result.EartagCountry.Should().BeNull();
        result.EartagHerdmark.Should().BeNull();
        result.Eartag.Should().Be("12345");
    }

    [Theory]
    [InlineData("ABC123")]
    [InlineData("UK123")]
    public void Parse_NonNumericWithoutDelimiter_IsInvalid(string raw)
    {
        var result = EartagParser.Parse(raw);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Unable to determine eartag format");
    }

    [Fact]
    public void Parse_OverlongInput_FallsBackToFreeFormat()
    {
        var raw = new string('A', 30);

        var result = EartagParser.Parse(raw);

        result.IsValid.Should().BeTrue();
        result.Eartag.Should().Be(raw);
        result.EartagCountry.Should().BeNull();
    }

    [Fact]
    public void Parse_TrimsSurroundingWhitespace()
    {
        EartagParser.Parse("  12345  ").Eartag.Should().Be("12345");
    }

    [Theory]
    [InlineData(null, null, null, "")]
    [InlineData(null, null, "999", "999")]
    [InlineData("UK", null, "123", "UK123")]
    [InlineData("UK", "12345", "678", "UK12345-678")]
    [InlineData(null, "AB1", "234", "AB1-234")]
    [InlineData("UK", "12345", null, "UK12345")]
    [InlineData("UK", "12345", " ", "UK12345")]
    public void Combine_ReassemblesDisplayEartag(string? country, string? herd, string? eartag, string expected)
    {
        EartagParser.Combine(country, herd, eartag).Should().Be(expected);
    }

    [Fact]
    public void Combine_CountryOnlyWithoutEartag_ReturnsCountry()
    {
        EartagParser.Combine("UK", " ", null).Should().Be("UK");
    }

    [Fact]
    public void ParseResult_Invalid_CarriesMessageAndIsNotEmpty()
    {
        var result = EartagParseResult.Invalid("bad");

        result.IsValid.Should().BeFalse();
        result.IsEmpty.Should().BeFalse();
        result.ErrorMessage.Should().Be("bad");
    }
}
