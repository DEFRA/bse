using BSE.Modules.AdnsExport.Models;

namespace BSE.Modules.AdnsExport.Email;

/// <summary>
/// Stateless ADNS email body builder. Replaces the <c>CreateEmailBody()</c> private
/// method of the legacy <c>clsADNSReport</c> class.
///
/// Email format is fixed-field structured text for the ADNS system ("CVETUNK1" messages).
/// Each case produces exactly 6 lines:
/// <code>
/// &lt;I&gt;CVETUNK1
/// &lt;C&gt;104&lt;V&gt;30
/// &lt;C&gt;110&lt;V&gt;{year} / {number:00000}
/// &lt;C&gt;112&lt;V&gt;{regionId:00000}
/// &lt;C&gt;117&lt;V&gt;2
/// &lt;C&gt;140&lt;V&gt;{DDMMYY}
/// </code>
/// </summary>
public static class AdnsEmailBodyBuilder
{
    /// <summary>
    /// Builds the ADNS notification email body from a list of case records.
    /// The output is byte-for-byte identical to the legacy VB.NET implementation.
    /// </summary>
    public static string Build(IReadOnlyList<AdnsCaseRecord> cases)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var c in cases)
        {
            sb.Append("<I>CVETUNK1\r\n");
            sb.Append("<C>104<V>30\r\n");
            sb.Append($"<C>110<V>{c.AdnsYear} / {c.AdnsNumber:00000}\r\n");
            sb.Append($"<C>112<V>{c.AdnsRegionId:00000}\r\n");
            sb.Append("<C>117<V>2\r\n");
            sb.Append($"<C>140<V>{c.ConfirmationDate:ddMMyy}\r\n");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Builds the ADNS notification email body for Channel Islands or NI cases,
    /// which share the same format but are provided as generic input rows (no RowStamp).
    /// </summary>
    public static string BuildFromInputs(IReadOnlyList<ChannelIslandCaseInput> cases)
    {
        var mapped = cases.Select((c, i) => new AdnsCaseRecord
        {
            Id = i + 1,
            AdnsYear = c.AdnsYear,
            AdnsNumber = c.AdnsNumber,
            AdnsRegionId = c.AdnsRegionId,
            AdnsRegionName = c.AdnsRegionName,
            ConfirmationDate = c.ConfirmationDate,
            AdnsReference = $"{c.AdnsYear}/{c.AdnsNumber:00000}"
        }).ToList();
        return Build(mapped);
    }
}
