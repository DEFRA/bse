using BSE.Modules.Batch.Models;

namespace BSE.Host.Models;

/// <summary>
/// View model for the _CaseTabs partial — identifies active tab and RBSE for link generation.
/// Also carries the batch numbers for the shared panel rendered by _CaseTabs itself,
/// so the panel appears in exactly one place in code but on every case-entry page (legacy parity).
/// </summary>
public sealed record CaseTabsViewModel(string ActiveTab, string Rbse, IReadOnlyList<BatchNumberEntry>? BatchNumbers = null);
