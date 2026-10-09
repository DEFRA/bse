using BSE.Modules.Batch.Models;

namespace BSE.Host.Models;

/// <summary>
/// View model for the _CaseTabs partial — identifies active tab and RBSE for link generation.
/// Also carries the batch numbers for the shared panel rendered by _CaseTabs itself,
/// so the panel appears in exactly one place in code but on every case-entry page (legacy parity).
/// </summary>
public sealed record CaseTabsViewModel(
    string ActiveTab,
    string Rbse,
    IReadOnlyList<BatchNumberEntry>? BatchNumbers = null,
    string? ViewDocsUrl = null,
    // True to render the common Save/Cancel actions next to "View docs" for the active tab's
    // own form — each page computes this from its own edit-permission check.
    bool CanEditCurrentTab = false,
    // Overrides the per-ActiveTab Save handler/Cancel target below — used by Farm's new-case
    // creation mode, whose Save/Cancel are "CreateCase"/plain navigate-to-Home rather than the
    // existing-case "SaveFarm"/"CancelFarmEdit" handlers.
    string? SaveHandlerOverride = null,
    string? CancelPageOverride = null,
    string? CancelHandlerOverride = null);
