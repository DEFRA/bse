using System.Collections.ObjectModel;
using BSE.Host.Helpers;
using BSE.Host.Services;
using BSE.Infrastructure;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Repositories;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace BSE.Host.Pages.Case;

/// <summary>
/// Feeds tab for a case — mirrors legacy CaseEntryFeeds.aspx: a single shared field panel used
/// for both "Add As New" and "Update Selected", plus Validate Supplier lookup. All add/edit/delete
/// operations are staged and only committed to the database when the user selects Save
/// (matches /Case/Farm's inline editable grid behaviour).
/// </summary>
[Authorize]
public class FeedsModel(
    IFeedRepository feedRepository,
    ICaseService caseService,
    ILookupDataService lookups,
    ILookupRepository lookupRepository,
    IBatchRepository batchRepository,
    ICaseFeedsDraftStateService feedsDraftState,
    ICaseScalarDraftStateService caseScalarDraftState,
    ICaseEditOrchestrationService caseEditOrchestration,
    ICurrentUserService currentUser,
    IConfiguration configuration) : PageModel
{
    private List<CaseFeedRecord> _persistedFeeds = [];

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? SortColumn { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool SortDesc { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public IReadOnlyList<StagedFeedItem> Feeds { get; private set; } = [];
    public const int PageSize = 10;
    private const string DataEntryRole = "DataEntry";
    private const string VlaAccessRole = "VLAAccess";
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Feeds.Count / (double)PageSize));
    public int CurrentPage => Math.Clamp(PageNumber, 1, TotalPages);
    public IReadOnlyList<StagedFeedItem> GetPagedFeeds() =>
        [.. Feeds.Skip((CurrentPage - 1) * PageSize).Take(PageSize)];
    public IEnumerable<LookupItem> RationTypes { get; private set; } = [];
    public string SpolSiteUrl { get; private set; } = string.Empty;
    public IReadOnlyList<BatchNumberEntry> BatchNumbers { get; private set; } = [];

    // ── Shared add/edit field panel state (mirrors legacy's single panel used for both
    // "Add As New" and "Update Selected"; not per-row inline like the herd size grid) ──
    [BindProperty] public string? EditingClientKey { get; set; }
    [BindProperty] public int? EditingFeedId { get; set; }
    [BindProperty] public short? YearFrom { get; set; }
    [BindProperty] public short? YearTo { get; set; }
    [BindProperty] public string? RationType { get; set; }
    [BindProperty] public string? RationName { get; set; }
    [BindProperty] public bool IsPrePurchase { get; set; }
    [BindProperty] public string? SupplierName { get; set; }
    [BindProperty] public int? SupplierId { get; set; }
    [BindProperty] public string? SupplierLookupName { get; set; }
    [BindProperty(SupportsGet = true)] public int? PickedSupplierId { get; set; }
    [BindProperty(SupportsGet = true)] public string? PickedSupplierName { get; set; }
    [BindProperty(SupportsGet = true)] public bool ResetSupplier { get; set; }

    public IDictionary<string, string> FieldErrors { get; private set; } = new Dictionary<string, string>();

    /// <summary>True when the draft has staged changes not yet committed by Save.</summary>
    public bool HasUnsavedChanges { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Legacy parity: CaseEntryFeeds.aspx.vb's Page_Load redirects to SessionError.aspx when
        // Session(SV_RBSENumber) is missing (session timeout, direct URL access, stale back-button).
        if (string.IsNullOrWhiteSpace(Rbse))
            return RedirectToPage("/SessionError");

        Rbse = RbseHelper.ParseToRaw(Rbse);
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
        await LoadAsync();
        await LoadOrInitializeDraftStateAsync();
        RestorePostedPanelState();

        if (ResetSupplier)
        {
            SupplierId = null;
            SupplierName = string.Empty;
        }

        if (PickedSupplierId.HasValue && !string.IsNullOrWhiteSpace(PickedSupplierName))
        {
            SupplierId = PickedSupplierId;
            SupplierName = PickedSupplierName;
        }

        return Page();
    }

    /// <summary>AJAX: "Validate Supplier" — exact match auto-fills; otherwise returns close matches to pick from.</summary>
    public async Task<IActionResult> OnGetValidateSupplierAsync(string? name)
    {
        if (!User.IsInRole(DataEntryRole) || !User.IsInRole(VlaAccessRole))
            return new JsonResult(new { found = false, matches = Array.Empty<object>() });

        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            return new JsonResult(new { found = false, matches = Array.Empty<object>() });

        var pickUrl = Url.Page("/Case/PickSupplier", new { rbse = Rbse, name = trimmed });

        var exact = await lookupRepository.GetSupplierByNameAsync(trimmed.ToUpperInvariant());
        if (exact is not null)
            return new JsonResult(new { found = true, id = exact.Id, name = exact.Name, pick = true, url = pickUrl });

        return new JsonResult(new { found = false, pick = true, url = pickUrl });
    }

    /// <summary>Adds a feed record to the draft only. Not persisted until Save.</summary>
    public async Task<IActionResult> OnPostAddFeedRowAsync()
    {
        if (!User.IsInRole(DataEntryRole) || !User.IsInRole(VlaAccessRole))
            return Forbid();

        Rbse = RbseHelper.ParseToRaw(Rbse);

        await LoadAsync();
        var draft = await LoadOrInitializeDraftStateAsync();

        var caseRecord = await caseService.GetCaseAsync(RbseHelper.ParseToRaw(Rbse));
        var stagedCase = (await caseScalarDraftState.GetAsync(Rbse))?.Case;
        var effectiveBirthDate = stagedCase is not null ? stagedCase.BirthDate : caseRecord?.BirthDate;
        FieldErrors = FeedValidation.Validate(new FeedValidation.Input(YearFrom, YearTo, RationType, SupplierId), effectiveBirthDate);

        if (FieldErrors.Count > 0)
            return Page();

        draft.Feeds.Add(new CaseFeedsDraftItem
        {
            ClientKey = Guid.NewGuid().ToString("N"),
            Id = null,
            YearFrom = YearFrom,
            YearTo = YearTo,
            RationType = RationType,
            RationDescription = RationTypes.FirstOrDefault(r => r.Code == RationType)?.Description,
            RationName = string.IsNullOrWhiteSpace(RationName) ? null : RationName,
            IsPrePurchase = IsPrePurchase,
            SupplierId = SupplierId,
            SupplierName = SupplierName,
            RowStampBase64 = string.Empty
        });
        draft.HasPendingChanges = true;
        await feedsDraftState.SetAsync(draft);

        return RedirectToPage(new { rbse = Rbse });
    }

    /// <summary>Populates the shared field panel from a staged row for editing (no changes saved yet).</summary>
    public async Task<IActionResult> OnPostBeginEditFeedRowAsync(string clientKey)
    {
        if (!User.IsInRole(DataEntryRole) || !User.IsInRole(VlaAccessRole))
            return Forbid();

        Rbse = RbseHelper.ParseToRaw(Rbse);

        await LoadAsync();
        var draft = await LoadOrInitializeDraftStateAsync();

        var item = draft.Feeds.FirstOrDefault(f => f.ClientKey == clientKey);
        if (item is not null)
        {
            EditingClientKey = clientKey;
            EditingFeedId = item.Id;
            YearFrom = item.YearFrom;
            YearTo = item.YearTo;
            RationType = item.RationType;
            RationName = item.RationName;
            IsPrePurchase = item.IsPrePurchase;
            SupplierId = item.SupplierId;
            SupplierName = item.SupplierName;

            // Clear posted values so Razor uses the values set above for the edit panel/hidden fields.
            ModelState.Remove(nameof(EditingClientKey));
            ModelState.Remove(nameof(EditingFeedId));
            ModelState.Remove(nameof(YearFrom));
            ModelState.Remove(nameof(YearTo));
            ModelState.Remove(nameof(RationType));
            ModelState.Remove(nameof(RationName));
            ModelState.Remove(nameof(IsPrePurchase));
            ModelState.Remove(nameof(SupplierId));
            ModelState.Remove(nameof(SupplierName));
        }

        return Page();
    }

    /// <summary>Updates the currently selected staged row from the shared field panel. Not persisted until Save.</summary>
    public async Task<IActionResult> OnPostUpdateFeedRowAsync()
    {
        if (!User.IsInRole(DataEntryRole) || !User.IsInRole(VlaAccessRole))
            return Forbid();

        Rbse = RbseHelper.ParseToRaw(Rbse);

        var clientKey = EditingClientKey;

        await LoadAsync();
        var draft = await LoadOrInitializeDraftStateAsync();

        var item = draft.Feeds.FirstOrDefault(f => f.ClientKey == clientKey);
        if (item is null && EditingFeedId is > 0)
        {
            item = draft.Feeds.FirstOrDefault(f => f.Id == EditingFeedId.Value);
            if (item is not null)
                EditingClientKey = item.ClientKey;
        }
        if (item is null)
        {
            TempData["ErrorMessage"] = "The feed record being edited no longer exists.";
            return RedirectToPage(new { rbse = Rbse });
        }

        var caseRecord = await caseService.GetCaseAsync(RbseHelper.ParseToRaw(Rbse));
        var stagedCase = (await caseScalarDraftState.GetAsync(Rbse))?.Case;
        var effectiveBirthDate = stagedCase is not null ? stagedCase.BirthDate : caseRecord?.BirthDate;
        FieldErrors = FeedValidation.Validate(new FeedValidation.Input(YearFrom, YearTo, RationType, SupplierId), effectiveBirthDate);

        if (FieldErrors.Count > 0)
        {
            EditingClientKey = clientKey;
            return Page();
        }

        item.YearFrom = YearFrom;
        item.YearTo = YearTo;
        item.RationType = RationType;
        item.RationDescription = RationTypes.FirstOrDefault(r => r.Code == RationType)?.Description;
        item.RationName = string.IsNullOrWhiteSpace(RationName) ? null : RationName;
        item.IsPrePurchase = IsPrePurchase;
        item.SupplierId = SupplierId;
        item.SupplierName = SupplierName;
        draft.HasPendingChanges = true;
        await feedsDraftState.SetAsync(draft);

        return RedirectToPage(new { rbse = Rbse });
    }

    /// <summary>Removes a staged feed row (mirrors legacy "Delete Selected"). Not persisted until Save.</summary>
    public async Task<IActionResult> OnPostDeleteFeedRowAsync(string clientKey)
    {
        if (!User.IsInRole(DataEntryRole) || !User.IsInRole(VlaAccessRole))
            return Forbid();

        Rbse = RbseHelper.ParseToRaw(Rbse);
        StashPostedPanelState();

        await LoadAsync();
        var draft = await LoadOrInitializeDraftStateAsync();

        var item = draft.Feeds.FirstOrDefault(f => f.ClientKey == clientKey);
        if (item is not null)
        {
            draft.Feeds.Remove(item);
            draft.HasPendingChanges = true;
            await feedsDraftState.SetAsync(draft);
        }

        return RedirectToPage(new { rbse = Rbse });
    }

    /// <summary>Commits all staged feed changes to the database in one transaction.</summary>
    public async Task<IActionResult> OnPostSaveFeedsAsync()
    {
        if (!User.IsInRole(DataEntryRole))
            return Forbid();

        Rbse = RbseHelper.ParseToRaw(Rbse);

        await LoadAsync();
        await LoadOrInitializeDraftStateAsync();

        // Cross-tab commit (restores legacy's "one session, one commit" model): the staged feed
        // rows are committed together with whatever else is staged for this RBSE (Case/Farm/Bab/
        // Clinical), not in isolation, so Save from any tab commits everything together.
        var userId = await currentUser.GetUserIdAsync();
        var (failureRedirect, commitOutcome) = await CaseCommitHelper.CommitAllAsync(
            this, caseEditOrchestration, Rbse, userId,
            result => $"Unable to save feed records: {result}.");
        if (failureRedirect is not null)
            return failureRedirect;

        if (CaseCommitHelper.TryStageWarnings(this, commitOutcome!, Rbse) is { } warningRedirect)
            return warningRedirect;

        // Legacy parity: CaseEntrySave.aspx auto-redirects to Home.aspx on a fully successful
        // save, clearing the session case state — not back to the tab the user was on.
        return RedirectToPage("/Home");
    }

    /// <summary>Discards all staged feed changes without persisting them.</summary>
    public async Task<IActionResult> OnPostCancelFeedsEditAsync()
    {
        Rbse = RbseHelper.ParseToRaw(Rbse);
        await feedsDraftState.ClearAsync(Rbse);
        await caseScalarDraftState.ClearAsync(Rbse);
        return RedirectToPage("/Home");
    }

    public async Task<IActionResult> OnPostValidateSupplierNavigateAsync()
    {
        if (!User.IsInRole(DataEntryRole) || !User.IsInRole(VlaAccessRole))
            return Forbid();

        Rbse = RbseHelper.ParseToRaw(Rbse);
        StashPostedPanelState();
        var postedLookupName = Request.Form[nameof(SupplierLookupName)].ToString();
        var supplierName = (SupplierLookupName ?? postedLookupName ?? SupplierName ?? string.Empty).Trim();

        return RedirectToPage("/Case/PickSupplier", new { rbse = Rbse, name = supplierName });
    }

    private async Task LoadAsync()
    {
        var rbse = RbseHelper.ParseToRaw(Rbse);
        var feedsTask = feedRepository.GetByRbseAsync(rbse);
        var batchTask = batchRepository.GetBatchNumbersByRbseAsync(Rbse);
        var rtTask = lookups.GetLookupAsync(LookupTableId.RationType);

        await Task.WhenAll(feedsTask, batchTask, rtTask);

        _persistedFeeds = (await feedsTask).ToList();
        BatchNumbers = (await batchTask).ToList().AsReadOnly();
        RationTypes = await rtTask;
    }

    private string PanelStateTempDataKey => $"FeedsPanelState_{RbseHelper.ParseToRaw(Rbse)}";

    /// <summary>Stashes the shared add/edit panel's posted-but-not-yet-committed values into
    /// TempData before a row operation (e.g. Delete) reloads and redirects — otherwise an
    /// in-progress selection for a not-yet-added/updated row is silently discarded, since none
    /// of these fields round-trip through the GET that follows the redirect.</summary>
    private void StashPostedPanelState()
    {
        var panel = new FeedsPanelState(
            EditingClientKey, EditingFeedId, YearFrom, YearTo, RationType, RationName,
            IsPrePurchase, SupplierId, SupplierName);
        TempData[PanelStateTempDataKey] = System.Text.Json.JsonSerializer.Serialize(panel);
    }

    /// <summary>Restores whatever OnPostDeleteFeedRowAsync (or similar) stashed, so the follow-up
    /// GET redisplays the panel exactly as the user left it rather than blank.</summary>
    private void RestorePostedPanelState()
    {
        if (TempData[PanelStateTempDataKey] is not string json)
            return;

        var panel = System.Text.Json.JsonSerializer.Deserialize<FeedsPanelState>(json);
        if (panel is null)
            return;

        EditingClientKey = panel.EditingClientKey;
        EditingFeedId = panel.EditingFeedId;
        YearFrom = panel.YearFrom;
        YearTo = panel.YearTo;
        RationType = panel.RationType;
        RationName = panel.RationName;
        IsPrePurchase = panel.IsPrePurchase;
        SupplierId = panel.SupplierId;
        SupplierName = panel.SupplierName;
    }

    private sealed record FeedsPanelState(
        string? EditingClientKey, int? EditingFeedId, short? YearFrom, short? YearTo,
        string? RationType, string? RationName, bool IsPrePurchase, int? SupplierId, string? SupplierName);

    private async Task<CaseFeedsDraftState> LoadOrInitializeDraftStateAsync()
    {
        var rbse = RbseHelper.ParseToRaw(Rbse);
        var draft = await feedsDraftState.GetAsync(rbse);
        if (draft is null)
        {
            draft = new CaseFeedsDraftState
            {
                Rbse = rbse,
                Feeds = _persistedFeeds.Select(f => new CaseFeedsDraftItem
                {
                    ClientKey = Guid.NewGuid().ToString("N"),
                    Id = f.Id,
                    YearFrom = f.YearFrom,
                    YearTo = f.YearTo,
                    RationType = f.RationType,
                    RationDescription = f.RationDescription,
                    RationName = f.RationName,
                    IsPrePurchase = f.IsPrePurchase,
                    SupplierId = f.SupplierId,
                    SupplierName = f.SupplierName,
                    RowStampBase64 = f.RowStamp is null ? string.Empty : Convert.ToBase64String(f.RowStamp)
                }).ToList(),
                HasPendingChanges = false
            };
            await feedsDraftState.SetAsync(draft);
        }

        var staged = draft.Feeds.Select(f => new StagedFeedItem
        {
            ClientKey = f.ClientKey,
            Id = f.Id,
            YearFrom = f.YearFrom,
            YearTo = f.YearTo,
            RationType = f.RationType,
            RationDescription = f.RationDescription,
            RationName = f.RationName,
            IsPrePurchase = f.IsPrePurchase,
            SupplierId = f.SupplierId,
            SupplierName = f.SupplierName,
            RowStampBase64 = f.RowStampBase64
        });

        Feeds = SortFeeds(staged.ToList());
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);
        HasUnsavedChanges = draft.HasPendingChanges;

        return draft;
    }

    private ReadOnlyCollection<StagedFeedItem> SortFeeds(IReadOnlyList<StagedFeedItem> feeds)
    {
        IEnumerable<StagedFeedItem> q = feeds;
        q = SortColumn switch
        {
            "YearFrom"          => SortDesc ? q.OrderByDescending(f => f.YearFrom)          : q.OrderBy(f => f.YearFrom),
            "YearTo"            => SortDesc ? q.OrderByDescending(f => f.YearTo)            : q.OrderBy(f => f.YearTo),
            "RationDescription" => SortDesc ? q.OrderByDescending(f => f.RationDescription) : q.OrderBy(f => f.RationDescription),
            "SupplierName"      => SortDesc ? q.OrderByDescending(f => f.SupplierName)      : q.OrderBy(f => f.SupplierName),
            "RationName"        => SortDesc ? q.OrderByDescending(f => f.RationName)        : q.OrderBy(f => f.RationName),
            "IsPrePurchase"     => SortDesc ? q.OrderByDescending(f => f.IsPrePurchase)     : q.OrderBy(f => f.IsPrePurchase),
            _                   => q.OrderBy(f => f.YearFrom).ThenBy(f => f.YearTo)
        };
        return q.ToList().AsReadOnly();
    }

    public sealed class StagedFeedItem
    {
        public string ClientKey { get; set; } = string.Empty;
        public int? Id { get; set; }
        public short? YearFrom { get; set; }
        public short? YearTo { get; set; }
        public string? RationType { get; set; }
        public string? RationDescription { get; set; }
        public string? RationName { get; set; }
        public bool IsPrePurchase { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string RowStampBase64 { get; set; } = string.Empty;

        /// <summary>True for rows staged locally that do not yet exist in the database.</summary>
        public bool IsUnsaved => Id is null or <= 0;
    }
}

