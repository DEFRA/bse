using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;

namespace BSE.Host.Models.ViewModels;

/// <summary>
/// Maps the <see cref="ICaseEditFields"/> shared by <see cref="CaseEditViewModel"/> and
/// <see cref="VlaEditViewModel"/> to/from <see cref="CaseRecord"/> and <see cref="EditCaseCommand"/>.
/// Extracted so both view models' near-identical FromRecord/ApplyStagedCommand/ToEditCommand bodies
/// are written once instead of once per tab.
/// </summary>
public static class CaseEditFieldMapper
{
    public static void CopyFromRecord(ICaseEditFields target, CaseRecord r)
    {
        target.Rbse = r.Rbse;
        target.EartagCountry = r.EartagCountry;
        target.EartagHerdmark = r.EartagHerdmark;
        target.Eartag = r.Eartag;
        target.PreviousEartag = r.PreviousEartag;
        target.Bse1ReceivedDate = r.Bse1ReceivedDate;
        target.FormADate = r.FormADate;
        target.FormAResubmittedDate = r.FormAResubmittedDate;
        target.FormBDate = r.FormBDate;
        target.Fate = r.Fate;
        target.FormCDate = r.FormCDate;
        target.IsPurchaserBse1Received = r.IsPurchaserBse1Received;
        target.IsBreederBse1Received = r.IsBreederBse1Received;
        target.IsVendor1Bse1Received = r.IsVendor1Bse1Received;
        target.IsHomebredBse1Received = r.IsHomebredBse1Received;
        target.IsSummarySheetReceived = r.IsSummarySheetReceived;
        target.IsPaperworkComplete = r.IsPaperworkComplete;
        target.ReportedLocation = r.ReportedLocation;
        target.Survey = r.Survey;
        target.Notes = r.Notes;
        target.BirthDate = r.BirthDate;
        target.IsBirthDateEst = r.IsBirthDateEst ?? false;
        target.DamStatus = r.DamStatus;
        target.BirthDateSource = r.BirthDateSource;
        target.ValuationAge = r.ValuationAge;
        target.Sex = r.Sex;
        target.Breed = r.Breed;
        target.Origin = r.Origin;
        target.PurchaseDate = r.PurchaseDate;
        target.PurchaseAgeInMonths = r.PurchaseAgeInMonths;
        target.PurchasedCounty = r.PurchasedCounty;
        target.HerdEntryDate = r.HerdEntryDate;
        target.OnsetDate = r.OnsetDate;
        target.IsOnsetDateEst = r.IsOnsetDateEst ?? false;
        target.MonthsPregnant = r.MonthsPregnant;
        target.MonthsPostCalving = r.MonthsPostCalving;
        target.OnsetAgeInMonths = r.OnsetAgeInMonths;
        target.SlaughterDate = r.SlaughterDate;
        target.AlternateDiagnosis = r.AlternateDiagnosis;
        target.LabComment = r.LabComment;
        target.CaseType = r.CaseType;
    }

    /// <summary>Overlays a staged-but-not-yet-committed Case edit (from another tab's
    /// cross-tab draft) onto this view model, so revisiting a tab shows pending edits
    /// made elsewhere instead of silently reverting to the last-committed DB values.</summary>
    public static void ApplyStagedCommand(ICaseEditFields target, EditCaseCommand c)
    {
        target.EartagCountry = c.EartagCountry;
        target.EartagHerdmark = c.EartagHerdmark;
        target.Eartag = c.Eartag;
        target.PreviousEartag = c.PreviousEartag;
        target.Bse1ReceivedDate = c.Bse1ReceivedDate;
        target.FormADate = c.FormADate;
        target.FormAResubmittedDate = c.FormAResubmittedDate;
        target.FormBDate = c.FormBDate;
        target.Fate = c.Fate;
        target.FormCDate = c.FormCDate;
        target.IsPurchaserBse1Received = c.IsPurchaserBse1Received;
        target.IsBreederBse1Received = c.IsBreederBse1Received;
        target.IsVendor1Bse1Received = c.IsVendor1Bse1Received;
        target.IsHomebredBse1Received = c.IsHomebredBse1Received;
        target.IsSummarySheetReceived = c.IsSummarySheetReceived;
        target.IsPaperworkComplete = c.IsPaperworkComplete;
        target.ReportedLocation = c.ReportedLocation;
        target.Survey = c.Survey;
        target.Notes = c.Notes;
        target.BirthDate = c.BirthDate;
        target.IsBirthDateEst = c.IsBirthDateEst ?? false;
        target.DamStatus = c.DamStatus;
        target.BirthDateSource = c.BirthDateSource;
        target.ValuationAge = c.ValuationAge;
        target.Sex = c.Sex;
        target.Breed = c.Breed;
        target.Origin = c.Origin;
        target.PurchaseDate = c.PurchaseDate;
        target.PurchaseAgeInMonths = c.PurchaseAgeInMonths;
        target.PurchasedCounty = c.PurchasedCounty;
        target.HerdEntryDate = c.HerdEntryDate;
        target.OnsetDate = c.OnsetDate;
        target.IsOnsetDateEst = c.IsOnsetDateEst ?? false;
        target.MonthsPregnant = c.MonthsPregnant;
        target.MonthsPostCalving = c.MonthsPostCalving;
        target.OnsetAgeInMonths = c.OnsetAgeInMonths;
        target.SlaughterDate = c.SlaughterDate;
        target.AlternateDiagnosis = c.AlternateDiagnosis;
        target.LabComment = c.LabComment;
        target.CaseType = c.CaseType;
    }

    public static EditCaseCommand ToEditCommand(ICaseEditFields source, byte[] rowStamp) => new(
        Rbse: source.Rbse,
        EartagCountry: source.EartagCountry,
        EartagHerdmark: source.EartagHerdmark,
        Eartag: source.Eartag,
        PreviousEartag: source.PreviousEartag,
        Bse1ReceivedDate: source.Bse1ReceivedDate,
        FormADate: source.FormADate,
        FormAResubmittedDate: source.FormAResubmittedDate,
        FormBDate: source.FormBDate,
        Fate: source.Fate,
        FormCDate: source.FormCDate,
        IsPurchaserBse1Received: source.IsPurchaserBse1Received,
        IsBreederBse1Received: source.IsBreederBse1Received,
        IsVendor1Bse1Received: source.IsVendor1Bse1Received,
        IsHomebredBse1Received: source.IsHomebredBse1Received,
        IsSummarySheetReceived: source.IsSummarySheetReceived,
        IsPaperworkComplete: source.IsPaperworkComplete,
        ReportedLocation: source.ReportedLocation,
        Survey: source.Survey,
        Notes: source.Notes,
        BirthDate: source.BirthDate,
        IsBirthDateEst: source.BirthDate.HasValue ? source.IsBirthDateEst : null,
        DamStatus: source.DamStatus,
        BirthDateSource: source.BirthDateSource,
        ValuationAge: source.ValuationAge,
        Sex: source.Sex,
        Breed: source.Breed,
        Origin: source.Origin,
        PurchaseDate: source.PurchaseDate,
        PurchaseAgeInMonths: source.PurchaseAgeInMonths,
        PurchasedCounty: source.PurchasedCounty,
        HerdEntryDate: source.HerdEntryDate,
        OnsetDate: source.OnsetDate,
        IsOnsetDateEst: source.OnsetDate.HasValue ? source.IsOnsetDateEst : null,
        MonthsPregnant: source.MonthsPregnant,
        MonthsPostCalving: source.MonthsPostCalving,
        OnsetAgeInMonths: source.OnsetAgeInMonths,
        SlaughterDate: source.SlaughterDate,
        RowStamp: rowStamp,
        AlternateDiagnosis: source.AlternateDiagnosis,
        LabComment: source.LabComment,
        CaseType: source.CaseType);
}
