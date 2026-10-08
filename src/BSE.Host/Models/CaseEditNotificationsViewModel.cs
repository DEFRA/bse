namespace BSE.Host.Models;

/// <summary>
/// View model for the _CaseEditNotifications partial — the success/warning banners and
/// GDS error summary shared by every case-edit tab (Farm/Case (DEFRA)/BAB/Case (APHA)/
/// Clinical/Feeds). Extracted so the markup is written once instead of once per tab.
/// </summary>
public sealed record CaseEditNotificationsViewModel(
    // Prefix for the banner heading ids (e.g. "feeds", "bab") — kept unique per page for
    // accessibility, even though only one tab's banners are ever rendered at a time.
    string IdPrefix,
    string? SuccessMessage = null,
    string? WarningMessage = null,
    // Feeds' own field-level validation errors (e.g. FeedValidation), shown alongside
    // ModelState errors in the same error summary — every other tab passes none.
    IReadOnlyCollection<string>? ExtraFieldErrors = null);
