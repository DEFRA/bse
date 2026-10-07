namespace BSE.Host.Models;

/// <summary>
/// View model for the shared _CaseSaveCancel partial — a single Save/Cancel button pair
/// reused (not re-implemented per tab) at both the top and bottom of every case tab page.
/// Save still submits to this page's own handler (each tab has its own fields/form), but the
/// underlying commit is unified across tabs via ICaseEditOrchestrationService.
/// </summary>
public sealed record CaseSaveCancelViewModel(
    string Rbse,
    string CancelPage,
    string CancelHandler,
    string? SaveHandler = null,
    bool MarginTop = false,
    // True when the caller wraps this partial in its own flex row (e.g. alongside the
    // bottom audit-log/view-docs links) and needs it flush, with no extra top/bottom margin.
    bool NoMargin = false);
