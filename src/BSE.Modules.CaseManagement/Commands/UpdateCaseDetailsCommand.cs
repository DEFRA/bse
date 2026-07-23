using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Models;

namespace BSE.Modules.CaseManagement.Commands;

/// <summary>
/// Full case-save payload passed to <c>ICaseService.UpdateCaseDetailsAsync</c>.
/// All child collections are optional; null means "leave unchanged".
/// This object replaces the legacy 11-table DataSet (CASE_TABLE=0 through CASEWORK_TABLE=10).
/// </summary>
public sealed record UpdateCaseDetailsCommand(
    /// <summary>Case core fields. Must be non-null. BatchId links the case to a batch for new cases.</summary>
    AddCaseCommand Case,
    int? BatchId,
    AddCaseClinicalCommand? Clinical,
    AddCaseBabCommand? Bab,
    IReadOnlyList<AddFeedCommand> Feeds,
    IReadOnlyList<AddTestCommand> Tests,
    IReadOnlyList<AddOtherOwnerCommand> OtherOwners,
    AddEditDamSireCommand? DamSire,
    IReadOnlyList<AddClinicalVisitCommand> ClinicalVisits);

/// <summary>
/// Full case-edit payload passed to <c>ICaseService.EditCaseAsync</c>.
/// Mirrors <see cref="UpdateCaseDetailsCommand"/> but uses Edit commands that carry
/// <c>RowStamp</c> values for optimistic concurrency checking.
/// Null child objects mean "leave child table unchanged".
/// </summary>
public sealed record EditCaseDetailsCommand(
    /// <summary>Core case fields including RowStamp for concurrency protection.</summary>
    EditCaseCommand Case,
    EditCaseClinicalCommand? Clinical,
    EditCaseBabCommand? Bab,
    AddEditDamSireCommand? DamSire);
