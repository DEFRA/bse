namespace BSE.Modules.AdnsExport.Models;

/// <summary>
/// One ADNS case row from <c>GetADNSCasesForGB</c> result set 1 (valid cases).
/// ADNSReference is pre-formatted as "YYYY/NNNNN" by the SP.
/// </summary>
public sealed record AdnsCaseRecord
{
    public int Id { get; init; }
    public string Rbse { get; init; } = string.Empty;
    public int AdnsYear { get; init; }
    public int AdnsNumber { get; init; }
    public int AdnsRegionId { get; init; }
    public string? AdnsRegionName { get; init; }
    public DateTime ConfirmationDate { get; init; }
    /// <summary>Pre-formatted "YYYY/NNNNN" reference string from SP.</summary>
    public string AdnsReference { get; init; } = string.Empty;
    public byte[]? RowStamp { get; init; }
}
