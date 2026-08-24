using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;

namespace BSE.Host.Models.ViewModels;

/// <summary>
/// Flat view model for the Case (VLA) edit form.
/// VLA-owned fields are bound to visible inputs; DEFRA-owned fields are
/// round-tripped as hidden fields so the EditCase SP receives the full row.
/// </summary>
public class VlaEditViewModel
{
    public string Rbse { get; set; } = string.Empty;

    // ── VLA-owned editable fields ─────────────────────────────────────────────
    public DateTime? BirthDate { get; set; }
    public string? BirthDateSource { get; set; }
    public bool IsBirthDateEst { get; set; }
    public string? Sex { get; set; }
    public string? Breed { get; set; }
    public string? Origin { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public short? PurchaseAgeInMonths { get; set; }
    public string? PurchasedCounty { get; set; }
    public DateTime? HerdEntryDate { get; set; }
    public DateTime? OnsetDate { get; set; }
    public bool IsOnsetDateEst { get; set; }
    public byte? MonthsPregnant { get; set; }
    public byte? MonthsPostCalving { get; set; }
    public short? OnsetAgeInMonths { get; set; }
    public DateTime? SlaughterDate { get; set; }

    // ── DEFRA-owned pass-through fields (hidden in form) ─────────────────────
    public string? EartagCountry { get; set; }
    public string? EartagHerdmark { get; set; }
    public string? Eartag { get; set; }
    public string? PreviousEartag { get; set; }
    public DateTime? Bse1ReceivedDate { get; set; }
    public DateTime? FormADate { get; set; }
    public DateTime? FormAResubmittedDate { get; set; }
    public DateTime? FormBDate { get; set; }
    public string? Fate { get; set; }
    public DateTime? FormCDate { get; set; }
    public bool IsPurchaserBse1Received { get; set; }
    public bool IsBreederBse1Received { get; set; }
    public bool IsVendor1Bse1Received { get; set; }
    public bool IsHomebredBse1Received { get; set; }
    public bool IsSummarySheetReceived { get; set; }
    public bool IsPaperworkComplete { get; set; }
    public string? ReportedLocation { get; set; }
    public string? Survey { get; set; }
    public string? Notes { get; set; }
    public string? DamStatus { get; set; }
    public string? ValuationAge { get; set; }
    public string? AlternateDiagnosis { get; set; }
    public string? LabComment { get; set; }
    public string? CaseType { get; set; }

    public static VlaEditViewModel FromRecord(CaseRecord r) => new()
    {
        Rbse                  = r.Rbse,
        BirthDate             = r.BirthDate,
        BirthDateSource       = r.BirthDateSource,
        IsBirthDateEst        = r.IsBirthDateEst ?? false,
        Sex                   = r.Sex,
        Breed                 = r.Breed,
        Origin                = r.Origin,
        PurchaseDate          = r.PurchaseDate,
        PurchaseAgeInMonths   = r.PurchaseAgeInMonths,
        PurchasedCounty       = r.PurchasedCounty,
        HerdEntryDate         = r.HerdEntryDate,
        OnsetDate             = r.OnsetDate,
        IsOnsetDateEst        = r.IsOnsetDateEst ?? false,
        MonthsPregnant        = r.MonthsPregnant,
        MonthsPostCalving     = r.MonthsPostCalving,
        OnsetAgeInMonths      = r.OnsetAgeInMonths,
        SlaughterDate         = r.SlaughterDate,
        // pass-through
        EartagCountry         = r.EartagCountry,
        EartagHerdmark        = r.EartagHerdmark,
        Eartag                = r.Eartag,
        PreviousEartag        = r.PreviousEartag,
        Bse1ReceivedDate      = r.Bse1ReceivedDate,
        FormADate             = r.FormADate,
        FormAResubmittedDate  = r.FormAResubmittedDate,
        FormBDate             = r.FormBDate,
        Fate                  = r.Fate,
        FormCDate             = r.FormCDate,
        IsPurchaserBse1Received = r.IsPurchaserBse1Received,
        IsBreederBse1Received   = r.IsBreederBse1Received,
        IsVendor1Bse1Received   = r.IsVendor1Bse1Received,
        IsHomebredBse1Received  = r.IsHomebredBse1Received,
        IsSummarySheetReceived  = r.IsSummarySheetReceived,
        IsPaperworkComplete     = r.IsPaperworkComplete,
        ReportedLocation      = r.ReportedLocation,
        Survey                = r.Survey,
        Notes                 = r.Notes,
        DamStatus             = r.DamStatus,
        ValuationAge          = r.ValuationAge,
        AlternateDiagnosis    = r.AlternateDiagnosis,
        LabComment            = r.LabComment,
        CaseType              = r.CaseType,
    };

    public EditCaseCommand ToEditCommand(byte[] rowStamp) => new(
        Rbse:                   Rbse,
        EartagCountry:          EartagCountry,
        EartagHerdmark:         EartagHerdmark,
        Eartag:                 Eartag,
        PreviousEartag:         PreviousEartag,
        Bse1ReceivedDate:       Bse1ReceivedDate,
        FormADate:              FormADate,
        FormAResubmittedDate:   FormAResubmittedDate,
        FormBDate:              FormBDate,
        Fate:                   Fate,
        FormCDate:              FormCDate,
        IsPurchaserBse1Received: IsPurchaserBse1Received,
        IsBreederBse1Received:  IsBreederBse1Received,
        IsVendor1Bse1Received:  IsVendor1Bse1Received,
        IsHomebredBse1Received: IsHomebredBse1Received,
        IsSummarySheetReceived: IsSummarySheetReceived,
        IsPaperworkComplete:    IsPaperworkComplete,
        ReportedLocation:       ReportedLocation,
        Survey:                 Survey,
        Notes:                  Notes,
        BirthDate:              BirthDate,
        IsBirthDateEst:         BirthDate.HasValue ? IsBirthDateEst : null,
        DamStatus:              DamStatus,
        BirthDateSource:        BirthDateSource,
        ValuationAge:           ValuationAge,
        Sex:                    Sex,
        Breed:                  Breed,
        Origin:                 Origin,
        PurchaseDate:           PurchaseDate,
        PurchaseAgeInMonths:    PurchaseAgeInMonths,
        PurchasedCounty:        PurchasedCounty,
        HerdEntryDate:          HerdEntryDate,
        OnsetDate:              OnsetDate,
        IsOnsetDateEst:         OnsetDate.HasValue ? IsOnsetDateEst : null,
        MonthsPregnant:         MonthsPregnant,
        MonthsPostCalving:      MonthsPostCalving,
        OnsetAgeInMonths:       OnsetAgeInMonths,
        SlaughterDate:          SlaughterDate,
        RowStamp:               rowStamp,
        AlternateDiagnosis:     AlternateDiagnosis,
        LabComment:             LabComment,
        CaseType:               CaseType);
}
