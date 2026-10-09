using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;
using BSE.Host.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace BSE.Host.Models.ViewModels;

/// <summary>
/// Flat view model for the Case (VLA) edit form.
/// VLA-owned fields are bound to visible inputs; DEFRA-owned fields are
/// round-tripped as hidden fields so the EditCase SP receives the full row.
/// </summary>
public class VlaEditViewModel : ICaseEditFields
{
    public string Rbse { get; set; } = string.Empty;

    // ── VLA-owned editable fields ─────────────────────────────────────────────
    [ModelBinder(BinderType = typeof(DatePickerModelBinder))] public DateTime? BirthDate { get; set; }
    public string? BirthDateSource { get; set; }
    public bool IsBirthDateEst { get; set; }
    public string? Sex { get; set; }
    public string? Breed { get; set; }
    public string? Origin { get; set; }
    [ModelBinder(BinderType = typeof(DatePickerModelBinder))] public DateTime? PurchaseDate { get; set; }
    public short? PurchaseAgeInMonths { get; set; }
    public string? PurchasedCounty { get; set; }
    [ModelBinder(BinderType = typeof(DatePickerModelBinder))] public DateTime? HerdEntryDate { get; set; }
    [ModelBinder(BinderType = typeof(DatePickerModelBinder))] public DateTime? OnsetDate { get; set; }
    public bool IsOnsetDateEst { get; set; }
    public byte? MonthsPregnant { get; set; }
    public byte? MonthsPostCalving { get; set; }
    public short? OnsetAgeInMonths { get; set; }
    [ModelBinder(BinderType = typeof(DatePickerModelBinder))] public DateTime? SlaughterDate { get; set; }

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

    public static VlaEditViewModel FromRecord(CaseRecord r)
    {
        var vm = new VlaEditViewModel();
        CaseEditFieldMapper.CopyFromRecord(vm, r);
        return vm;
    }

    public EditCaseCommand ToEditCommand(byte[] rowStamp) => CaseEditFieldMapper.ToEditCommand(this, rowStamp);

    /// <summary>Overlays a staged-but-not-yet-committed Case edit (from another tab's
    /// cross-tab draft) onto this view model, so revisiting a tab shows pending edits
    /// made elsewhere instead of silently reverting to the last-committed DB values.</summary>
    public void ApplyStagedCommand(EditCaseCommand c) => CaseEditFieldMapper.ApplyStagedCommand(this, c);
}
