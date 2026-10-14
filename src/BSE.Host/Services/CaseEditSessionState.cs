using BSE.Modules.AnimalRelations.Commands;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseWork.Commands;
using BSE.Modules.FarmManagement.Models;
using BSE.SharedKernel;

namespace BSE.Host.Services;

/// <summary>
/// Single cross-tab session object for an in-progress case edit — the direct migrated equivalent
/// of legacy's one DataSet (Case/Farm/Bab/Clinical/Tests/OtherOwners/Visits/HerdSizes/LinkedFarms/
/// Feeds/Relations/DamSire/CaseWork tables) held in <c>Session(SV_CaseDetails)</c> for the entire
/// multi-tab editing session, from the moment the case is first loaded on any tab until the final
/// Save or Cancel. Every tab stages into (and reads from) this one object, keyed only by RBSE —
/// replacing the six separate per-concern draft stores that previously split this same
/// responsibility apart (each with its own cache key and its own "has this been consumed" edge
/// cases). Stored as one JSON document under one cache key per RBSE.
/// </summary>
public sealed class CaseEditSessionState
{
    public string Rbse { get; set; } = string.Empty;

    // ── Scalars ──────────────────────────────────────────────────────────────

    /// <summary>Staged Case-row edit. Shared by the Case (DEFRA) and Case (APHA) tabs, since both
    /// edit different columns of the same underlying Case row (each round-trips the other's
    /// columns via hidden fields, so whichever tab staged last always carries a complete row).</summary>
    public EditCaseCommand? Case { get; set; }

    /// <summary>RowStamp captured the first time this session staged a Case edit, used as the
    /// concurrency token for the eventual commit — mirrors legacy holding one row in session
    /// from the moment the case was first loaded until the final Save.</summary>
    public string? CaseBaseRowStampBase64 { get; set; }

    /// <summary>Staged CaseWork-row edit (Barcode/AHF Reference/BSE1 dates/Paperwork Complete),
    /// from the Case (DEFRA) tab.</summary>
    public EditCaseWorkCommand? CaseWork { get; set; }

    public UpdateFarmCommand? Farm { get; set; }
    public string? FarmBaseRowStampBase64 { get; set; }

    public EditCaseBabCommand? Bab { get; set; }
    public string? BabOrigin { get; set; }
    public string? BabBaseRowStampBase64 { get; set; }

    public EditCaseClinicalCommand? Clinical { get; set; }
    public string? ClinicalBaseRowStampBase64 { get; set; }

    /// <summary>Staged herdbook/dam-sire pedigree edit from the Relations tab. Only Relations
    /// ever stages this, so last-write-wins with no merge needed.</summary>
    public AddEditDamSireCommand? DamSire { get; set; }

    // ── Collections ──────────────────────────────────────────────────────────

    public List<CaseEditDraftTestItem> Tests { get; set; } = [];
    public List<CaseEditDraftOtherOwnerItem> OtherOwners { get; set; } = [];

    /// <summary>True once any Test or Other Owner row has been added/edited/deleted this session.</summary>
    public bool HasCaseEditChanges { get; set; }

    public List<CaseClinicalDraftVisitItem> Visits { get; set; } = [];

    /// <summary>True once any Clinical Visit row has been added/edited/deleted this session.</summary>
    public bool HasClinicalVisitsChanges { get; set; }

    /// <summary>CPHH that Linked Farms/Herd Size are keyed by (not RBSE-keyed).</summary>
    public string Cphh { get; set; } = string.Empty;
    public List<CaseFarmDraftLinkedFarmItem> LinkedFarms { get; set; } = [];
    public List<CaseFarmDraftHerdSizeItem> HerdSizes { get; set; } = [];

    /// <summary>True once any Linked Farm or Herd Size row has been added/edited/deleted this session.</summary>
    public bool HasFarmCollectionsChanges { get; set; }

    public List<CaseFeedsDraftItem> Feeds { get; set; } = [];

    /// <summary>True once any Feed row has been added/edited/deleted this session.</summary>
    public bool HasFeedsChanges { get; set; }

    public List<CaseRelationsDraftItem> Relations { get; set; } = [];
    public bool RemoveDamPending { get; set; }
    public bool RemoveSirePending { get; set; }

    /// <summary>True once any Relation row (or Dam/Sire pedigree field) has been
    /// added/edited/deleted this session.</summary>
    public bool HasRelationsChanges { get; set; }

    /// <summary>A dam/sire match staged from Look Up (or PickSireDam), not yet saved. Needed
    /// because Look Up for the dam and the sire post to two separate forms — without staging
    /// here, a match found for one parent is lost from the next request.</summary>
    public PendingParentDraft? PendingDam { get; set; }
    public PendingParentDraft? PendingSire { get; set; }

    /// <summary>True once any Case/Farm/Bab/Clinical/DamSire/CaseWork scalar field has been
    /// staged this session — kept as the single name every scalar-staging call site already uses.</summary>
    public bool HasPendingChanges { get; set; }
}
