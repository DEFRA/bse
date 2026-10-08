using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.FarmManagement.Models;

namespace BSE.Host.Services;

/// <summary>
/// Cross-tab staging for the *scalar* (non-grid) fields of the Case and Farm tabs, keyed by RBSE.
/// Restores legacy CaseEntry*.aspx's "one session, one commit" model: edits made on one tab but
/// not yet saved are held here so that Save on *any* tab (Farm or Case (DEFRA)) commits every
/// staged tab together, in one transaction, instead of each tab committing independently and
/// silently discarding whatever the other tab had typed but not yet saved.
/// </summary>
public sealed class CaseScalarDraftState
{
    public string Rbse { get; set; } = string.Empty;

    /// <summary>Staged Case-row edit. Shared by the Case (DEFRA) and Case (APHA) tabs, since both
    /// edit different columns of the same underlying Case row (each round-trips the other's
    /// columns via hidden fields, so whichever tab staged last always carries a complete row).</summary>
    public EditCaseCommand? Case { get; set; }

    /// <summary>RowStamp captured the first time this draft staged a Case edit, used as the
    /// concurrency token for the eventual commit — mirrors legacy holding one row in session
    /// from the moment the case was first loaded until the final Save.</summary>
    public string? CaseBaseRowStampBase64 { get; set; }

    public UpdateFarmCommand? Farm { get; set; }
    public string? FarmBaseRowStampBase64 { get; set; }

    public EditCaseBabCommand? Bab { get; set; }
    public string? BabOrigin { get; set; }
    public string? BabBaseRowStampBase64 { get; set; }

    public EditCaseClinicalCommand? Clinical { get; set; }
    public string? ClinicalBaseRowStampBase64 { get; set; }

    /// <summary>Staged herdbook/dam-sire pedigree edit from the Relations tab. Only Relations
    /// ever stages this, so last-write-wins with no merge needed (unlike Case above).</summary>
    public AddEditDamSireCommand? DamSire { get; set; }

    public bool HasPendingChanges { get; set; }
}
