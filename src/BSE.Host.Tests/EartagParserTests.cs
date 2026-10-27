using BSE.Host.Helpers;

namespace BSE.Host.Tests;

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
    [InlineData("826 703412 70001", "826", "703412", "70001")]
    [InlineData("8260703412 70001", "8260", "703412", "70001")]
    [InlineData("826_703412_70001", "826", "703412", "70001")]
    public void Parse_IsoNumeric_SplitsIntoComponents(string raw, string country, string herdmark, string eartag)
    {
        var result = EartagParser.Parse(raw);

        result.IsValid.Should().BeTrue();
        result.EartagCountry.Should().Be(country);
        result.EartagHerdmark.Should().Be(herdmark);
        result.Eartag.Should().Be(eartag);
    }

    [Theory]
    [InlineData("GB0 703412 70001", "GB0", "703412", "70001")]
    [InlineData("gb 703412 70001", "GB", "703412", "70001")]
    public void Parse_IsoAlpha_UppercasesCountryAndSplits(string raw, string country, string herdmark, string eartag)
    {
        var result = EartagParser.Parse(raw);

        result.IsValid.Should().BeTrue();
        result.EartagCountry.Should().Be(country);
        result.EartagHerdmark.Should().Be(herdmark);
        result.Eartag.Should().Be(eartag);
    }

    [Fact]
    public void Parse_BarimoTwelveDigits_SplitsSixAndSix()
    {
        var result = EartagParser.Parse("uk987654321098");

        result.IsValid.Should().BeTrue();
        result.EartagCountry.Should().Be("UK");
        result.EartagHerdmark.Should().Be("987654");
        result.Eartag.Should().Be("321098");
    }

    [Theory]
    [InlineData("-AB123-234", "AB123", "234")]
    [InlineData("*AB123-234", "AB123", "234")]
    public void Parse_LegacyPreBarimoPrefix_HasNoCountry(string raw, string herdmark, string eartag)
    {
        var result = EartagParser.Parse(raw);

        result.IsValid.Should().BeTrue();
        result.EartagCountry.Should().BeNull();
        result.EartagHerdmark.Should().Be(herdmark);
        result.Eartag.Should().Be(eartag);
    }

    [Theory]
    [InlineData("-")]
    [InlineData("*  ")]
    public void Parse_PrefixWithoutBody_IsInvalid(string raw)
    {
        EartagParser.Parse(raw).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Parse_PreBarimoWithoutDelimiter_IsInvalid()
    {
        var result = EartagParser.Parse("-AB123");

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("delimiter");
    }

    [Fact]
    public void Parse_KnownCountryPrefixWithDelimiter_ExtractsCountry()
    {
        var result = EartagParser.Parse("UKAB123-234");

        result.IsValid.Should().BeTrue();
        result.EartagCountry.Should().Be("UK");
        result.EartagHerdmark.Should().Be("AB123");
        result.Eartag.Should().Be("234");
    }

    [Theory]
    [InlineData("FR123-456")]
    [InlineData("de123-456")]
    public void Parse_AmbiguousLegacyPrefixWithoutMarker_IsInvalid(string raw)
    {
        var result = EartagParser.Parse(raw);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("prefix it with");
    }

    [Fact]
    public void Parse_DelimitedWithoutCountry_SplitsOnDash()
    {
        var result = EartagParser.Parse("AB123-234");

        result.IsValid.Should().BeTrue();
        result.EartagCountry.Should().BeNull();
        result.EartagHerdmark.Should().Be("AB123");
        result.Eartag.Should().Be("234");
    }

    [Fact]
    public void Parse_PureNumericWithoutDelimiter_IsFreeNumericEartag()
    {
        var result = EartagParser.Parse("12345");

        result.IsValid.Should().BeTrue();
        result.EartagCountry.Should().BeNull();
        result.EartagHerdmark.Should().BeNull();
        result.Eartag.Should().Be("12345");
    }

    [Fact]
    public void Parse_NonNumericWithoutDelimiter_IsInvalid()
    {
        var result = EartagParser.Parse("ABCDEF");

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Unable to determine eartag format");
    }

    [Fact]
    public void Parse_LongerThanTwentyFiveCharacters_FallsBackToFreeFormat()
    {
        var raw = new string('A', 26);

        var result = EartagParser.Parse(raw);

        result.IsValid.Should().BeTrue();
        result.Eartag.Should().Be(raw);
        result.EartagHerdmark.Should().BeNull();
    }

    [Fact]
    public void Parse_LongPathologicalInput_CompletesWithinRegexTimeout()
    {
        var raw = new string('1', 5000) + "!";

        var act = () => EartagParser.Parse(raw);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null, null, "123", "123")]
    [InlineData("UK", null, "123456", "UK123456")]
    [InlineData("UK", "AB123", "234", "UKAB123-234")]
    [InlineData(null, "AB123", "234", "AB123-234")]
    [InlineData("UK", "AB123", null, "UKAB123")]
    [InlineData(null, null, null, "")]
    public void Combine_RebuildsDisplayEartag(string? country, string? herdmark, string? eartag, string expected)
    {
        EartagParser.Combine(country, herdmark, eartag).Should().Be(expected);
    }
}
