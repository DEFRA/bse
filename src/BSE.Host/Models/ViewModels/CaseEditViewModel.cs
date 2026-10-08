using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseWork.Models;
using BSE.Host.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using CaseWorkRecord = BSE.Modules.CaseWork.Models.CaseWorkRecord;

namespace BSE.Host.Models.ViewModels;

/// <summary>
/// Flat view model for the Case Edit form. Populated from <see cref="CaseRecord"/>
/// and converted back to <see cref="EditCaseDetailsCommand"/> on POST.
/// RowStamp is round-tripped via TempData (Base64) to prevent tampering.
/// </summary>
public class CaseEditViewModel : ICaseEditFields
{
    public string Rbse { get; set; } = string.Empty;
    public string? EartagCountry { get; set; }
    public string? EartagHerdmark { get; set; }
    public string? Eartag { get; set; }
    public string? PreviousEartag { get; set; }
    [ModelBinder(BinderType = typeof(DatePickerModelBinder))] public DateTime? Bse1ReceivedDate { get; set; }
    [ModelBinder(BinderType = typeof(DatePickerModelBinder))] public DateTime? FormADate { get; set; }
    [ModelBinder(BinderType = typeof(DatePickerModelBinder))] public DateTime? FormAResubmittedDate { get; set; }
    [ModelBinder(BinderType = typeof(DatePickerModelBinder))] public DateTime? FormBDate { get; set; }
    public string? Fate { get; set; }
    [ModelBinder(BinderType = typeof(DatePickerModelBinder))] public DateTime? FormCDate { get; set; }
    public bool IsPurchaserBse1Received { get; set; }
    public bool IsBreederBse1Received { get; set; }
    public bool IsVendor1Bse1Received { get; set; }
    public bool IsHomebredBse1Received { get; set; }
    public bool IsSummarySheetReceived { get; set; }
    public bool IsPaperworkComplete { get; set; }
    public string? ReportedLocation { get; set; }
    public string? Survey { get; set; }
    public string? Notes { get; set; }
    [ModelBinder(BinderType = typeof(DatePickerModelBinder))] public DateTime? BirthDate { get; set; }
    public bool IsBirthDateEst { get; set; }
    public string? DamStatus { get; set; }
    public string? BirthDateSource { get; set; }
    public string? ValuationAge { get; set; }
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
    public string? AlternateDiagnosis { get; set; }
    public string? LabComment { get; set; }
    public string? CaseType { get; set; }

    // ── Read-only display fields (not editable, shown for context) ────────────
    public DateTime? FinalResultDate { get; set; }
    public string? FinalResult { get; set; }
    public string? Dbse { get; set; }

    // ── Casework fields (CaseWork table, EditCaseWork SP) ─────────────────────
    public string? Barcode { get; set; }
    public string? AhfReference { get; set; }
    [ModelBinder(BinderType = typeof(DatePickerModelBinder))] public DateTime? PurchaserBse1ReceivedDate { get; set; }
    [ModelBinder(BinderType = typeof(DatePickerModelBinder))] public DateTime? BreederBse1ReceivedDate { get; set; }
    [ModelBinder(BinderType = typeof(DatePickerModelBinder))] public DateTime? Vendor1Bse1ReceivedDate { get; set; }
    [ModelBinder(BinderType = typeof(DatePickerModelBinder))] public DateTime? HomebredBse1ReceivedDate { get; set; }
    [ModelBinder(BinderType = typeof(DatePickerModelBinder))] public DateTime? SummarySheetReceivedDate { get; set; }
    [ModelBinder(BinderType = typeof(DatePickerModelBinder))] public DateTime? PaperworkCompleteDate { get; set; }
    public DateTime? RbseDate { get; set; }
    public bool HasCaseWork { get; set; }

    public static CaseEditViewModel FromRecord(CaseRecord r)
    {
        var vm = new CaseEditViewModel { FinalResultDate = r.FinalResultDate, FinalResult = r.FinalResult, Dbse = r.Dbse };
        CaseEditFieldMapper.CopyFromRecord(vm, r);
        return vm;
    }

    /// <summary>Overlays a staged-but-not-yet-committed Case edit (from another tab's
    /// cross-tab draft) onto this view model, so revisiting a tab shows pending edits
    /// made elsewhere instead of silently reverting to the last-committed DB values.</summary>
    public void ApplyStagedCommand(EditCaseCommand c) => CaseEditFieldMapper.ApplyStagedCommand(this, c);

    public void ApplyCaseWork(CaseWorkRecord cw)
    {
        HasCaseWork = true;
        Barcode = cw.Barcode;
        AhfReference = cw.AhfReference;
        PurchaserBse1ReceivedDate = cw.PurchaserBse1ReceivedDate;
        BreederBse1ReceivedDate = cw.BreederBse1ReceivedDate;
        Vendor1Bse1ReceivedDate = cw.Vendor1Bse1ReceivedDate;
        HomebredBse1ReceivedDate = cw.HomebredBse1ReceivedDate;
        SummarySheetReceivedDate = cw.SummarySheetReceivedDate;
        PaperworkCompleteDate = cw.PaperworkCompleteDate;
        RbseDate = cw.RbseDate;
    }

    public EditCaseCommand ToEditCommand(byte[] rowStamp) => CaseEditFieldMapper.ToEditCommand(this, rowStamp);
}
