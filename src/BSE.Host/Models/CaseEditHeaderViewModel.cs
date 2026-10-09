using BSE.Modules.Batch.Models;

namespace BSE.Host.Models;

/// <summary>
/// View model for the _CaseEditHeader partial — the breadcrumb + _CaseTabs + _CaseEditNotifications
/// block repeated near-identically at the top of every case-edit tab (Farm/Case (DEFRA)/BAB/
/// Case (APHA)/Clinical/Feeds). Extracted so the markup is written once instead of once per tab.
/// </summary>
public sealed record CaseEditHeaderViewModel(
    // Breadcrumb trail text for the current page, e.g. "Feeds", "BAB", "Case (APHA)".
    string BreadcrumbLabel,
    // Active tab key passed through to _CaseTabs — same value as BreadcrumbLabel on every
    // current caller, but kept distinct in case a future page's breadcrumb/tab text diverge.
    string ActiveTab,
    string Rbse,
    IReadOnlyList<BatchNumberEntry>? BatchNumbers,
    string? ViewDocsUrl,
    bool CanEditCurrentTab,
    // Prefix for the notification banner heading ids — see CaseEditNotificationsViewModel.
    string IdPrefix,
    string? SuccessMessage = null,
    string? WarningMessage = null,
    IReadOnlyCollection<string>? ExtraFieldErrors = null);
