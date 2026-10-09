namespace BSE.Host.Models.ViewModels;

/// <summary>
/// The Case fields shared by <see cref="CaseEditViewModel"/> (Case (DEFRA) tab) and
/// <see cref="VlaEditViewModel"/> (Case (VLA) tab) — both round-trip the same underlying
/// <c>CaseRecord</c>/<c>EditCaseCommand</c>, just split across two tabs with different
/// editable/pass-through groupings. Implemented by both so <see cref="CaseEditFieldMapper"/>
/// can hold the mapping logic once instead of twice.
/// </summary>
public interface ICaseEditFields
{
    string Rbse { get; set; }
    string? EartagCountry { get; set; }
    string? EartagHerdmark { get; set; }
    string? Eartag { get; set; }
    string? PreviousEartag { get; set; }
    DateTime? Bse1ReceivedDate { get; set; }
    DateTime? FormADate { get; set; }
    DateTime? FormAResubmittedDate { get; set; }
    DateTime? FormBDate { get; set; }
    string? Fate { get; set; }
    DateTime? FormCDate { get; set; }
    bool IsPurchaserBse1Received { get; set; }
    bool IsBreederBse1Received { get; set; }
    bool IsVendor1Bse1Received { get; set; }
    bool IsHomebredBse1Received { get; set; }
    bool IsSummarySheetReceived { get; set; }
    bool IsPaperworkComplete { get; set; }
    string? ReportedLocation { get; set; }
    string? Survey { get; set; }
    string? Notes { get; set; }
    DateTime? BirthDate { get; set; }
    bool IsBirthDateEst { get; set; }
    string? DamStatus { get; set; }
    string? BirthDateSource { get; set; }
    string? ValuationAge { get; set; }
    string? Sex { get; set; }
    string? Breed { get; set; }
    string? Origin { get; set; }
    DateTime? PurchaseDate { get; set; }
    short? PurchaseAgeInMonths { get; set; }
    string? PurchasedCounty { get; set; }
    DateTime? HerdEntryDate { get; set; }
    DateTime? OnsetDate { get; set; }
    bool IsOnsetDateEst { get; set; }
    byte? MonthsPregnant { get; set; }
    byte? MonthsPostCalving { get; set; }
    short? OnsetAgeInMonths { get; set; }
    DateTime? SlaughterDate { get; set; }
    string? AlternateDiagnosis { get; set; }
    string? LabComment { get; set; }
    string? CaseType { get; set; }
}
