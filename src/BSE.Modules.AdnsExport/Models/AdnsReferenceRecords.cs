namespace BSE.Modules.AdnsExport.Models;

/// <summary>
/// One invalid ADNS case row from <c>GetADNSCasesForGB</c> result set 2 (cases without ADNSRegionID).
/// The SP returns only a display-formatted RBSE string for these.
/// </summary>
public sealed record MissingAdnsCaseRecord
{
    /// <summary>Display-formatted RBSE (NN/NN/NNNNN) from SP.</summary>
    public string Rbse { get; init; } = string.Empty;
}

/// <summary>
/// Summary row — count of cases per ADNS region. Computed from the valid case list.
/// </summary>
public sealed record AdnsRegionSummaryRecord(int AdnsRegionId, string AdnsRegionName, int CasesCount);

/// <summary>
/// Last ADNS reference record from <c>GetLastADNSReferenceByArea</c>.
/// </summary>
public sealed record LastAdnsReferenceRecord
{
    public string? LastEmailReference { get; init; }
    public short? LastAdnsReferenceYear { get; init; }
    public int? LastAdnsReferenceNumber { get; init; }
}

/// <summary>
/// Result from the report-generation step — the full export preview payload.
/// Passed to the UI layer for display before the user confirms dispatch.
/// </summary>
public sealed record AdnsExportPreview(
    IReadOnlyList<AdnsCaseRecord> Cases,
    IReadOnlyList<MissingAdnsCaseRecord> MissingCases,
    IReadOnlyList<AdnsRegionSummaryRecord> Summary,
    string EmailSubject,
    string EmailBody,
    string? StartAdnsReference,
    string? EndAdnsReference);
