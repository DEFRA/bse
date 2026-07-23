namespace BSE.Modules.CaseManagement.Models;

/// <summary>
/// Result record for GetCaseByRBSE — the primary case row including all fields
/// and the RowStamp (timestamp) used for optimistic concurrency on EditCase.
/// </summary>
public sealed record CaseRecord
{
    public string Rbse { get; init; } = string.Empty;
    public string Cphh { get; init; } = string.Empty;
    public string? EartagCountry { get; init; }
    public string? EartagHerdmark { get; init; }
    public string? Eartag { get; init; }
    public bool IsNonGbCase { get; init; }
    public string? PreviousEartag { get; init; }
    public DateTime? Bse1ReceivedDate { get; init; }
    public DateTime? FormADate { get; init; }
    public DateTime? FormAResubmittedDate { get; init; }
    public DateTime? FormBDate { get; init; }
    public string? Fate { get; init; }
    public DateTime? FormCDate { get; init; }
    public bool IsPurchaserBse1Received { get; init; }
    public bool IsBreederBse1Received { get; init; }
    public bool IsVendor1Bse1Received { get; init; }
    public bool IsHomebredBse1Received { get; init; }
    public bool IsSummarySheetReceived { get; init; }
    public bool IsPaperworkComplete { get; init; }
    public DateTime? FinalResultDate { get; init; }
    public string? FinalResult { get; init; }
    public string? Dbse { get; init; }
    public string? ReportedLocation { get; init; }
    public string? Survey { get; init; }
    public string? Notes { get; init; }
    public DateTime? BirthDate { get; init; }
    public bool IsBAB { get; init; }
    public bool IsBirthDateEst { get; init; }
    public string? DamStatus { get; init; }
    public string? BirthDateSource { get; init; }
    public string? ValuationAge { get; init; }
    public string? Sex { get; init; }
    public string? Breed { get; init; }
    public string? Origin { get; init; }
    public DateTime? PurchaseDate { get; init; }
    public short? PurchaseAgeInMonths { get; init; }
    public string? PurchasedCounty { get; init; }
    public DateTime? HerdEntryDate { get; init; }
    public DateTime? OnsetDate { get; init; }
    public bool IsOnsetDateEst { get; init; }
    public byte? MonthsPregnant { get; init; }
    public byte? MonthsPostCalving { get; init; }
    public short? OnsetAgeInMonths { get; init; }
    public DateTime? SlaughterDate { get; init; }
    public DateTime? EmailSentToAdnsDate { get; init; }
    /// <summary>SQL Server timestamp (ROWVERSION) used for optimistic concurrency in EditCase.</summary>
    public byte[]? RowStamp { get; init; }
    public string? AlternateDiagnosis { get; init; }
    public string? LabComment { get; init; }
    public string? CaseType { get; init; }
    public byte[]? PedigreeRowStamp { get; init; }
    public string? Herdbook { get; init; }
}
