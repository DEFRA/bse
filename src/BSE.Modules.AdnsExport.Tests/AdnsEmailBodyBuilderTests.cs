using BSE.Modules.AdnsExport.Email;
using BSE.Modules.AdnsExport.Models;

namespace BSE.Modules.AdnsExport.Tests;

/// <summary>
/// Output comparison harness — Slice 11 acceptance criterion 1.
///
/// These tests verify that <see cref="AdnsEmailBodyBuilder.Build"/> produces output that is
/// byte-for-byte identical to the legacy VB.NET <c>clsADNSReport.CreateEmailBody()</c>
/// for the same input data.
///
/// The legacy format (from clsADNSReport.vb) for each case is:
/// <code>
/// &lt;I&gt;CVETUNK1\r\n
/// &lt;C&gt;104&lt;V&gt;30\r\n
/// &lt;C&gt;110&lt;V&gt;{ADNSYear} / {ADNSNumber,5}\r\n
/// &lt;C&gt;112&lt;V&gt;{ADNSRegionID,5}\r\n
/// &lt;C&gt;117&lt;V&gt;2\r\n
/// &lt;C&gt;140&lt;V&gt;{DD}{MM}{YY}\r\n
/// </code>
/// </summary>
public sealed class AdnsEmailBodyBuilderTests
{
    /// <summary>
    /// Single case — reference cases taken from legacy system documentation.
    /// The confirmation date DDMMYY format is what the legacy Right$() call produced.
    /// </summary>
    [Fact]
    public void Build_SingleCase_MatchesLegacyFormat()
    {
        var cases = new List<AdnsCaseRecord>
        {
            new()
            {
                Id = 1, Rbse = "010000001",
                AdnsYear = 2024, AdnsNumber = 42,
                AdnsRegionId = 1001, AdnsRegionName = "South West",
                ConfirmationDate = new DateTime(2024, 3, 15),
                AdnsReference = "2024/00042"
            }
        };

        var body = AdnsEmailBodyBuilder.Build(cases);

        // Build the expected output using the same logic as the legacy VB.NET code
        var expected =
            "<I>CVETUNK1\r\n" +
            "<C>104<V>30\r\n" +
            "<C>110<V>2024 / 00042\r\n" +   // year + " / " + Right("0000" + ADNSNumber, 5)
            "<C>112<V>01001\r\n" +            // Right("0000" + ADNSRegionID, 5) → "01001"
            "<C>117<V>2\r\n" +
            "<C>140<V>150324\r\n";            // DDMMYY

        body.Should().Be(expected);
    }

    [Fact]
    public void Build_MultipleCases_EachCaseProducesSixLines()
    {
        var cases = new List<AdnsCaseRecord>
        {
            new() { Id=1, AdnsYear=2024, AdnsNumber=1, AdnsRegionId=1001, AdnsRegionName="SW",
                    ConfirmationDate=new DateTime(2024,1,5), AdnsReference="2024/00001" },
            new() { Id=2, AdnsYear=2024, AdnsNumber=2, AdnsRegionId=2002, AdnsRegionName="SE",
                    ConfirmationDate=new DateTime(2024,6,20), AdnsReference="2024/00002" }
        };

        var body = AdnsEmailBodyBuilder.Build(cases);
        var lines = body.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCount(12); // 6 lines per case × 2 cases
    }

    [Fact]
    public void Build_EmptyCaseList_ReturnsEmptyString()
    {
        var body = AdnsEmailBodyBuilder.Build(Array.Empty<AdnsCaseRecord>());
        body.Should().BeEmpty();
    }

    [Fact]
    public void Build_RegionIdPaddedToFiveDigits()
    {
        var cases = new List<AdnsCaseRecord>
        {
            new() { Id=1, AdnsYear=2024, AdnsNumber=1, AdnsRegionId=5,
                    ConfirmationDate=new DateTime(2024,1,1), AdnsReference="2024/00001" }
        };

        var body = AdnsEmailBodyBuilder.Build(cases);
        body.Should().Contain("<C>112<V>00005");
    }

    [Fact]
    public void Build_AdnsNumberPaddedToFiveDigits()
    {
        var cases = new List<AdnsCaseRecord>
        {
            new() { Id=1, AdnsYear=2024, AdnsNumber=7, AdnsRegionId=1001,
                    ConfirmationDate=new DateTime(2024,1,1), AdnsReference="2024/00007" }
        };

        var body = AdnsEmailBodyBuilder.Build(cases);
        body.Should().Contain("<C>110<V>2024 / 00007");
    }

    /// <summary>
    /// Verifies the date format is DDMMYY (not DDMMYYYY), matching legacy VB.NET
    /// <c>Right$(CStr(dConfirmationDate.Year), 2)</c>.
    /// </summary>
    [Fact]
    public void Build_DateFormatIsDDMMYY()
    {
        var cases = new List<AdnsCaseRecord>
        {
            new() { Id=1, AdnsYear=2024, AdnsNumber=1, AdnsRegionId=1001,
                    ConfirmationDate=new DateTime(2024, 12, 31), AdnsReference="2024/00001" }
        };

        var body = AdnsEmailBodyBuilder.Build(cases);
        body.Should().Contain("<C>140<V>311224"); // 31/12/24
    }

    [Fact]
    public void Build_DatePaddsDayAndMonthWithLeadingZero()
    {
        var cases = new List<AdnsCaseRecord>
        {
            new() { Id=1, AdnsYear=2024, AdnsNumber=1, AdnsRegionId=1001,
                    ConfirmationDate=new DateTime(2024, 2, 5), AdnsReference="2024/00001" }
        };

        var body = AdnsEmailBodyBuilder.Build(cases);
        body.Should().Contain("<C>140<V>050224"); // 05/02/24
    }

    /// <summary>
    /// Acceptance criterion: output matches legacy for 5 reference cases.
    /// Golden output hand-computed from the legacy VB.NET algorithm.
    /// </summary>
    [Fact]
    public void Build_FiveReferenceCases_MatchesLegacyGoldenOutput()
    {
        var cases = new List<AdnsCaseRecord>
        {
            new() { Id=1, Rbse="010000001", AdnsYear=2023, AdnsNumber=101, AdnsRegionId=1001,
                    AdnsRegionName="SW", ConfirmationDate=new DateTime(2023,3,1), AdnsReference="2023/00101" },
            new() { Id=2, Rbse="010000002", AdnsYear=2023, AdnsNumber=102, AdnsRegionId=2002,
                    AdnsRegionName="SE", ConfirmationDate=new DateTime(2023,3,2), AdnsReference="2023/00102" },
            new() { Id=3, Rbse="010000003", AdnsYear=2023, AdnsNumber=103, AdnsRegionId=3003,
                    AdnsRegionName="Midlands", ConfirmationDate=new DateTime(2023,3,3), AdnsReference="2023/00103" },
            new() { Id=4, Rbse="010000004", AdnsYear=2023, AdnsNumber=104, AdnsRegionId=4004,
                    AdnsRegionName="North", ConfirmationDate=new DateTime(2023,3,4), AdnsReference="2023/00104" },
            new() { Id=5, Rbse="010000005", AdnsYear=2023, AdnsNumber=105, AdnsRegionId=5005,
                    AdnsRegionName="Scotland", ConfirmationDate=new DateTime(2023,3,5), AdnsReference="2023/00105" },
        };

        var body = AdnsEmailBodyBuilder.Build(cases);

        // Golden output — identical to what legacy CreateEmailBody() would produce
        var expected = string.Concat(
            "<I>CVETUNK1\r\n<C>104<V>30\r\n<C>110<V>2023 / 00101\r\n<C>112<V>01001\r\n<C>117<V>2\r\n<C>140<V>010323\r\n",
            "<I>CVETUNK1\r\n<C>104<V>30\r\n<C>110<V>2023 / 00102\r\n<C>112<V>02002\r\n<C>117<V>2\r\n<C>140<V>020323\r\n",
            "<I>CVETUNK1\r\n<C>104<V>30\r\n<C>110<V>2023 / 00103\r\n<C>112<V>03003\r\n<C>117<V>2\r\n<C>140<V>030323\r\n",
            "<I>CVETUNK1\r\n<C>104<V>30\r\n<C>110<V>2023 / 00104\r\n<C>112<V>04004\r\n<C>117<V>2\r\n<C>140<V>040323\r\n",
            "<I>CVETUNK1\r\n<C>104<V>30\r\n<C>110<V>2023 / 00105\r\n<C>112<V>05005\r\n<C>117<V>2\r\n<C>140<V>050323\r\n");

        body.Should().Be(expected);
    }
}
