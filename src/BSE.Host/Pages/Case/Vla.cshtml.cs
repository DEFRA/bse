using BSE.Host.Models.ViewModels;
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
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;

namespace BSE.Host.Pages.Case;

[Authorize]
public class VlaModel(
    ICaseService caseService,
    IBabRepository babRepository,
    ICurrentUserService currentUserService,
    ICaseWizardStateService wizardState,
    ICaseEditDraftStateService caseEditDraftState,
    ICaseScalarDraftStateService caseScalarDraftState,
    ICaseEditOrchestrationService caseEditOrchestration,
    ILookupDataService lookups,
    IBatchRepository batchRepository,
    IOtherOwnerRepository ownerRepository,
    IDbConnectionFactory connectionFactory,
    IConfiguration configuration) : PageModel
{
    private const string RowStampKey    = "VlaEdit_RowStamp_{0}";
    private const int    OwnersPageSize = 10;
    private const string DataEntryRole  = "DataEntry";
    private const string WarningKey     = "Warning";
    private const string SpolSiteUrlKey = "SpolSiteUrl";

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)] public int    OPage { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string OSort { get; set; } = "type";
    [BindProperty(SupportsGet = true)] public string ODir  { get; set; } = "asc";
    public int OtherOwnersTotalPages { get; private set; } = 1;
    public int OtherOwnersTotalCount { get; private set; }

    [BindProperty]
    public VlaEditViewModel Case { get; set; } = new();

    public string? ConcurrencyError { get; private set; }
    public IReadOnlyList<BatchNumberEntry> BatchNumbers { get; private set; } = [];
    public CaseWizardState? PendingBatch { get; private set; }
    public bool CanEditMainCase { get; private set; }
    public bool HasTracedBabData { get; private set; }

    public IEnumerable<ILookupItem> BirthDateSourceOptions { get; private set; } = [];
    public IEnumerable<ILookupItem> SexOptions            { get; private set; } = [];
    public IEnumerable<ILookupItem> BreedOptions          { get; private set; } = [];
    public IEnumerable<ILookupItem> OriginOptions         { get; private set; } = [];
    public IEnumerable<ILookupItem> PurchasedCountyOptions { get; private set; } = [];

    public IReadOnlyList<OtherOwnerRecord> OtherOwners { get; private set; } = [];
    public IEnumerable<ILookupItem> OwnerTypeOptions { get; private set; } = [];

    [BindProperty] public string? NewOwnerType { get; set; }
    [BindProperty] public string? NewOwnerName { get; set; }
    [BindProperty] public string? NewOwnerCphh { get; set; }
    [BindProperty] public int EditingOwnerId { get; set; }
    [BindProperty] public string? EditOwnerType { get; set; }
    [BindProperty] public string? EditOwnerName { get; set; }
    [BindProperty] public string? EditOwnerCphh { get; set; }
    [BindProperty] public string EditOwnerRowStampBase64 { get; set; } = string.Empty;
    public bool ShowAddOwnerRow { get; private set; }
    public int? ReopenEditOwnerId { get; private set; }
    public bool HasUnsavedOwnerChanges { get; private set; }
    private List<CaseEditDraftOtherOwnerItem> StagedOtherOwners { get; set; } = [];

    public static string? SelectedIfMatches(string? currentValue, string? optionValue) =>
        string.Equals(currentValue, optionValue, StringComparison.OrdinalIgnoreCase) ? "selected" : null;

    public string SpolSiteUrl { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        // Legacy parity: CaseEntryVLA.aspx.vb's Page_Load redirects to SessionError.aspx when
        // Session(SV_RBSENumber) is missing (session timeout, direct URL access, stale back-button).
        if (string.IsNullOrWhiteSpace(Rbse))
            return RedirectToPage("/SessionError");

        var record = await caseService.GetCaseAsync(Rbse);

        // Legacy parity: a brand-new case lives entirely in the shared session object until the
        // first Save from *any* tab — CaseEditOrchestrationService.CommitAllAsync can create the
        // case from whichever tab's data is staged (it only needs Farm staged with a CPHH), so
        // this tab no longer forces the user back to Farm's own Save first.
        Case.Rbse = Rbse;
        if (record is not null)
        {
            TempData[string.Format(RowStampKey, Rbse)] = Convert.ToBase64String(record.RowStamp ?? []);
            Case = VlaEditViewModel.FromRecord(record);
        }
        else
        {
            TempData[string.Format(RowStampKey, Rbse)] = Convert.ToBase64String([]);
        }

        SpolSiteUrl = configuration[SpolSiteUrlKey] ?? string.Empty;
        HasTracedBabData = await HasTracedBabDataAsync(Rbse);

        var batchTask = batchRepository.GetBatchNumbersByRbseAsync(Rbse);
        var pendingBatchTask = wizardState.GetAsync();
        await Task.WhenAll(LoadLookupsAsync(), batchTask, pendingBatchTask);
        BatchNumbers = (await batchTask).ToList().AsReadOnly();
        PendingBatch = await pendingBatchTask;
        ApplyLegacyVlaEditPermissions();
        await LoadOrInitializeOwnersDraftAsync();
        await ApplyStagedCaseOverlayAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!User.IsInRole(DataEntryRole))
            return Forbid();

        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        Rbse = caseRbse;

        if (!await LoadBatchContextAndCheckEditPermissionAsync(caseRbse))
            return RedirectToPage(new { rbse = caseRbse });

        // Legacy parity: a brand-new case lives entirely in the shared session object until the
        // first Save from *any* tab — CaseEditOrchestrationService.CommitAllAsync can create the
        // case from whichever tab's data is staged, so this tab no longer forces the user back
        // to Farm's own Save first (matches the GET side's equivalent fix above).
        Case.Rbse = caseRbse;

        HasTracedBabData = await HasTracedBabDataAsync(caseRbse);

        SpolSiteUrl = configuration[SpolSiteUrlKey] ?? string.Empty;
        await LoadLookupsAsync();

        if (Case.Origin != "P")
        {
            Case.PurchaseDate = null;
            Case.PurchaseAgeInMonths = null;
            Case.PurchasedCounty = null;
        }

        if (Case.FormBDate.HasValue && !Case.SlaughterDate.HasValue)
            Case.SlaughterDate = Case.FormBDate;

        ValidateVlaDomainRules();

        if (!ModelState.IsValid)
        {
            await LoadOrInitializeOwnersDraftAsync();
            return Page();
        }

        var rowStampBase64 = TempData[string.Format(RowStampKey, Rbse)]?.ToString();
        if (rowStampBase64 is null)
        {
            ConcurrencyError = "Session expired — please reload the page and try again.";
            return Page();
        }

        // Cross-tab staging (restores legacy's "one session, one commit" model): stage this
        // tab's edit into the shared draft, then commit *everything* staged for this RBSE
        // (this tab and/or Farm/BAB/Clinical) together, rather than committing only this page's fields.
        await StageCaseScalarEditAsync(rowStampBase64);

        var userId = await currentUserService.GetUserIdAsync();
        var (failureRedirect, commitOutcome) = await CaseCommitHelper.CommitAllAsync(
            this, caseEditOrchestration, caseRbse, userId,
            result => result switch
            {
                EditCaseResult.RbseNotFound    => $"Case '{Rbse}' not found.",
                EditCaseResult.AuditLogError   => "Audit log error during update.",
                EditCaseResult.PostUpdateError => "Database error after update.",
                _                              => $"Update failed: {result}"
            });
        if (failureRedirect is not null)
            return failureRedirect;

        await PersistStagedOwnersAsync(caseRbse);
        await caseEditDraftState.ClearAsync(caseRbse);

        if (CaseCommitHelper.TryStageWarnings(this, commitOutcome!, caseRbse) is { } warningRedirect)
            return warningRedirect;

        // Legacy parity: CaseEntrySave.aspx auto-redirects to Home.aspx on a fully successful
        // save, clearing the session case state — not back to the tab the user was on.
        return RedirectToPage("/Home");
    }

    /// <summary>
    /// Validates this tab's fields and, if valid, stages them into the shared cross-tab
    /// draft (without committing) before navigating to another tab.
    /// </summary>
    public async Task<IActionResult> OnPostStageAndGotoAsync(string targetPage)
    {
        if (!User.IsInRole(DataEntryRole))
            return Forbid();

        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        Rbse = caseRbse;

        if (!await LoadBatchContextAndCheckEditPermissionAsync(caseRbse))
            return RedirectToPage(targetPage, new { rbse = caseRbse });

        SpolSiteUrl = configuration[SpolSiteUrlKey] ?? string.Empty;
        await LoadLookupsAsync();

        if (Case.Origin != "P")
        {
            Case.PurchaseDate = null;
            Case.PurchaseAgeInMonths = null;
            Case.PurchasedCounty = null;
        }

        if (Case.FormBDate.HasValue && !Case.SlaughterDate.HasValue)
            Case.SlaughterDate = Case.FormBDate;

        ValidateVlaDomainRules();

        if (!ModelState.IsValid)
        {
            await LoadOrInitializeOwnersDraftAsync();
            return Page();
        }

        var rowStampBase64 = TempData[string.Format(RowStampKey, Rbse)]?.ToString();
        if (rowStampBase64 is null)
        {
            ConcurrencyError = "Session expired — please reload the page and try again.";
            return Page();
        }

        await StageCaseScalarEditAsync(rowStampBase64);

        return RedirectToPage(targetPage, new { rbse = caseRbse });
    }

    /// <summary>Writes this tab's current field values into the shared cross-tab scalar
    /// draft (BSE.Host.Services.CaseScalarDraftState), without committing to the database.</summary>
    private async Task StageCaseScalarEditAsync(string rowStampBase64)
    {
        var draft = await caseScalarDraftState.GetAsync(Rbse) ?? new CaseScalarDraftState { Rbse = Rbse };
        draft.CaseBaseRowStampBase64 ??= rowStampBase64;
        var baseRowStamp = Convert.FromBase64String(draft.CaseBaseRowStampBase64);
        draft.Case = Case.ToEditCommand(baseRowStamp);
        draft.HasPendingChanges = true;
        await caseScalarDraftState.SetAsync(draft);
    }

    private async Task ApplyStagedCaseOverlayAsync()
    {
        var staged = await caseScalarDraftState.GetAsync(Rbse);
        if (staged?.Case is not null)
            Case.ApplyStagedCommand(staged.Case);
    }

    /// <summary>Stages the currently posted (model-bound) <see cref="Case"/> scalars — e.g. Purchased
    /// County, Purchase Date — into the shared cross-tab draft before an Other-Owners grid handler
    /// redirects. Without this, a scalar field edited in the same submission as an Add/Edit/Delete
    /// owner-row action is silently discarded: the redirect triggers a fresh GET that reloads
    /// <see cref="Case"/> from the database and overlays only what was already staged, not what was
    /// just posted in this exact request.</summary>
    private async Task StagePostedCaseScalarsBeforeRedirectAsync()
    {
        var rowStampBase64 = TempData[string.Format(RowStampKey, Rbse)]?.ToString();
        if (rowStampBase64 is not null)
            await StageCaseScalarEditAsync(rowStampBase64);
    }

    public async Task<IActionResult> OnGetCancelVlaEditAsync()
    {
        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        await caseEditDraftState.ClearAsync(caseRbse);
        await caseScalarDraftState.ClearAsync(caseRbse);
        return RedirectToPage("/Home");
    }

    public async Task<IActionResult> OnPostBeginEditOwnerRowAsync(int ownerId)
    {
        if (!User.IsInRole(DataEntryRole))
            return Forbid();

        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        Rbse = caseRbse;
        SpolSiteUrl = configuration[SpolSiteUrlKey] ?? string.Empty;
        await LoadLookupsAsync();
        if (!await LoadBatchContextAndCheckEditPermissionAsync(caseRbse))
            return RedirectToPage(new { rbse = caseRbse, OSort, ODir, OPage });

        await LoadOrInitializeOwnersDraftAsync();

        var owner = StagedOtherOwners.FirstOrDefault(o => (o.Id ?? 0) == ownerId);
        if (owner is null)
            return RedirectToPage(new { rbse = caseRbse, OSort, ODir, OPage });

        ReopenEditOwnerId = ownerId;
        EditOwnerType = owner.Type;
        EditOwnerName = owner.Name;
        EditOwnerCphh = owner.Cphh;
        EditOwnerRowStampBase64 = owner.RowStampBase64;
        return Page();
    }

    // Note: OnPostBeginEditOwnerRowAsync above redirects on its early-exit paths only (no edit
    // permission, owner not found) and returns Page() on success, so it never needs to stage —
    // Case remains whatever was model-bound from this exact POST when the page re-renders.

    public async Task<IActionResult> OnPostAddOwnerRowAsync()
    {
        if (!User.IsInRole(DataEntryRole))
            return Forbid();

        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        Rbse = caseRbse;
        SpolSiteUrl = configuration[SpolSiteUrlKey] ?? string.Empty;
        await LoadLookupsAsync();
        if (!await LoadBatchContextAndCheckEditPermissionAsync(caseRbse))
            return RedirectToPage(new { rbse = caseRbse, OSort, ODir, OPage });

        await LoadOrInitializeOwnersDraftAsync();

        var normalizedCphh = string.IsNullOrWhiteSpace(NewOwnerCphh) ? null : CphhNormalizer.Normalize(NewOwnerCphh);
        ValidateOwnerRow(NewOwnerType, NewOwnerName, normalizedCphh, null);

        if (!ModelState.IsValid)
        {
            ShowAddOwnerRow = true;
            return Page();
        }

        var draft = await GetOrCreateOwnersDraftAsync(caseRbse);
        draft.OtherOwners.Add(new CaseEditDraftOtherOwnerItem
        {
            Id = NextTemporaryOwnerId(draft),
            Type = NewOwnerType!,
            Name = string.IsNullOrWhiteSpace(NewOwnerName) ? null : NewOwnerName,
            Cphh = string.IsNullOrWhiteSpace(normalizedCphh) ? null : normalizedCphh,
            RowStampBase64 = string.Empty
        });
        draft.HasPendingChanges = true;
        await caseEditDraftState.SetAsync(draft);
        await StagePostedCaseScalarsBeforeRedirectAsync();

        return RedirectToPage(new { rbse = caseRbse, OSort, ODir, OPage });
    }

    public async Task<IActionResult> OnPostUpdateOwnerRowAsync()
    {
        if (!User.IsInRole(DataEntryRole))
            return Forbid();

        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        Rbse = caseRbse;
        SpolSiteUrl = configuration[SpolSiteUrlKey] ?? string.Empty;
        await LoadLookupsAsync();
        if (!await LoadBatchContextAndCheckEditPermissionAsync(caseRbse))
            return RedirectToPage(new { rbse = caseRbse, OSort, ODir, OPage });

        await LoadOrInitializeOwnersDraftAsync();

        var normalizedCphh = string.IsNullOrWhiteSpace(EditOwnerCphh) ? null : CphhNormalizer.Normalize(EditOwnerCphh);
        ValidateOwnerRow(EditOwnerType, EditOwnerName, normalizedCphh, EditingOwnerId);

        if (!ModelState.IsValid)
        {
            ReopenEditOwnerId = EditingOwnerId;
            return Page();
        }

        var draft = await GetOrCreateOwnersDraftAsync(caseRbse);
        var owner = draft.OtherOwners.FirstOrDefault(o => (o.Id ?? 0) == EditingOwnerId);
        if (owner is null)
        {
            TempData[WarningKey] = "Owner record not found.";
            return RedirectToPage(new { rbse = caseRbse, OSort, ODir, OPage });
        }

        owner.Type = EditOwnerType!;
        owner.Name = string.IsNullOrWhiteSpace(EditOwnerName) ? null : EditOwnerName;
        owner.Cphh = string.IsNullOrWhiteSpace(normalizedCphh) ? null : normalizedCphh;
        draft.HasPendingChanges = true;
        await caseEditDraftState.SetAsync(draft);
        await StagePostedCaseScalarsBeforeRedirectAsync();

        return RedirectToPage(new { rbse = caseRbse, OSort, ODir, OPage });
    }

    public async Task<IActionResult> OnPostDeleteOwnerAsync(int ownerId, string rowStampBase64)
    {
        if (!User.IsInRole(DataEntryRole))
            return Forbid();

        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        Rbse = caseRbse;
        if (!await LoadBatchContextAndCheckEditPermissionAsync(caseRbse))
            return RedirectToPage(new { rbse = caseRbse, OSort, ODir, OPage });

        var draft = await GetOrCreateOwnersDraftAsync(caseRbse);
        var owner = draft.OtherOwners.FirstOrDefault(o => (o.Id ?? 0) == ownerId);
        if (owner is null)
        {
            TempData[WarningKey] = "Owner record not found.";
            return RedirectToPage(new { rbse = caseRbse, OSort, ODir, OPage });
        }

        draft.OtherOwners.Remove(owner);
        draft.HasPendingChanges = true;
        await caseEditDraftState.SetAsync(draft);
        await StagePostedCaseScalarsBeforeRedirectAsync();
        return RedirectToPage(new { rbse = caseRbse, OSort, ODir, OPage });
    }

    private async Task<bool> HasTracedBabDataAsync(string rbse)
    {
        var bab = await babRepository.GetByRbseAsync(rbse);
        if (bab is null)
            return false;

        return !string.IsNullOrWhiteSpace(bab.NatalCphh)
               || !string.IsNullOrWhiteSpace(bab.TracedName)
               || !string.IsNullOrWhiteSpace(bab.TracedAddress1)
               || !string.IsNullOrWhiteSpace(bab.TracedAddress2)
               || !string.IsNullOrWhiteSpace(bab.TracedAddress3)
               || !string.IsNullOrWhiteSpace(bab.TracedPostcode);
    }

    private async Task LoadOrInitializeOwnersDraftAsync()
    {
        var caseRbse = RbseHelper.ParseToRaw(Rbse);
        var draft = await GetOrCreateOwnersDraftAsync(caseRbse);
        StagedOtherOwners = draft.OtherOwners.ToList();
        HasUnsavedOwnerChanges = draft.HasPendingChanges;
        var allOwners = StagedOtherOwners.Select(ToOwnerRecord).ToList();
        OtherOwnersTotalCount = allOwners.Count;
        OtherOwnersTotalPages = Math.Max(1, (int)Math.Ceiling(allOwners.Count / (double)OwnersPageSize));
        OPage = Math.Clamp(OPage, 1, OtherOwnersTotalPages);
        Func<OtherOwnerRecord, object?> ownerKeySelector = OSort switch
        {
            "cphh" => o => o.Cphh,
            "name" => o => o.Name,
            _ => o => o.Type,
        };
        var sorted = ODir == "desc"
            ? allOwners.OrderByDescending(ownerKeySelector)
            : allOwners.OrderBy(ownerKeySelector);
        OtherOwners = sorted.Skip((OPage - 1) * OwnersPageSize).Take(OwnersPageSize).ToList().AsReadOnly();
    }

    private void ApplyLegacyVlaEditPermissions()
    {
        // Legacy CaseEntryVLA uses IsVLAAllowedMainCaseEdit(Session):
        // selected batch present OR existing batch numbers linked to case.
        if (!User.IsInRole(DataEntryRole) || !User.IsInRole("VLAAccess"))
        {
            CanEditMainCase = false;
            return;
        }

        var hasCurrentBatchSelection = PendingBatch is not null
            && string.Equals(RbseHelper.ParseToRaw(PendingBatch.RbseNumber), RbseHelper.ParseToRaw(Rbse), StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(PendingBatch.BatchNumber);

        var hasAnyBatchHistory = BatchNumbers.Count > 0;
        CanEditMainCase = hasCurrentBatchSelection || hasAnyBatchHistory;
    }

    // Shared by OnPostAsync and the other-owner row handlers: loads the batch context this
    // request needs for ApplyLegacyVlaEditPermissions, then reports whether editing is allowed.
    private async Task<bool> LoadBatchContextAndCheckEditPermissionAsync(string caseRbse)
    {
        var batchTask = batchRepository.GetBatchNumbersByRbseAsync(caseRbse);
        var pendingBatchTask = wizardState.GetAsync();
        await Task.WhenAll(batchTask, pendingBatchTask);
        BatchNumbers = (await batchTask).ToList().AsReadOnly();
        PendingBatch = await pendingBatchTask;
        ApplyLegacyVlaEditPermissions();
        return CanEditMainCase;
    }

    private async Task<CaseEditDraftState> GetOrCreateOwnersDraftAsync(string caseRbse)
    {
        var draft = await caseEditDraftState.GetAsync(caseRbse);
        if (draft is null)
        {
            var owners = (await ownerRepository.GetByRbseAsync(caseRbse)).ToList();
            draft = new CaseEditDraftState
            {
                Rbse = caseRbse,
                OtherOwners = owners.Select(o => new CaseEditDraftOtherOwnerItem
                {
                    Id = o.Id,
                    Type = o.Type ?? string.Empty,
                    Name = o.Name,
                    Cphh = o.Cphh,
                    RowStampBase64 = Convert.ToBase64String(o.RowStamp ?? [])
                }).ToList(),
                HasPendingChanges = false
            };
            await caseEditDraftState.SetAsync(draft);
        }
        else if (!draft.HasPendingChanges && (draft.OtherOwners is null || draft.OtherOwners.Count == 0))
        {
            var owners = (await ownerRepository.GetByRbseAsync(caseRbse)).ToList();
            draft.OtherOwners = owners.Select(o => new CaseEditDraftOtherOwnerItem
            {
                Id = o.Id,
                Type = o.Type ?? string.Empty,
                Name = o.Name,
                Cphh = o.Cphh,
                RowStampBase64 = Convert.ToBase64String(o.RowStamp ?? [])
            }).ToList();
            await caseEditDraftState.SetAsync(draft);
        }

        draft.OtherOwners ??= [];
        return draft;
    }

    private static OtherOwnerRecord ToOwnerRecord(CaseEditDraftOtherOwnerItem owner)
    {
        var rowStamp = string.IsNullOrWhiteSpace(owner.RowStampBase64)
            ? []
            : Convert.FromBase64String(owner.RowStampBase64);

        return new OtherOwnerRecord(owner.Id ?? 0, string.Empty, owner.Type, owner.Name, owner.Cphh, rowStamp);
    }

    private static int NextTemporaryOwnerId(CaseEditDraftState draft)
    {
        var minId = draft.OtherOwners.Select(o => o.Id ?? 0).DefaultIfEmpty(0).Min();
        return minId <= 0 ? minId - 1 : -1;
    }

    private void ValidateOwnerRow(string? type, string? name, string? normalizedCphh, int? editingOwnerId)
    {
        if (string.IsNullOrWhiteSpace(type))
            ModelState.AddModelError(editingOwnerId.HasValue ? nameof(EditOwnerType) : nameof(NewOwnerType), "Owner type is required.");

        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(normalizedCphh))
            ModelState.AddModelError(editingOwnerId.HasValue ? nameof(EditOwnerName) : nameof(NewOwnerName), "You must enter either an owner name or a CPHH.");

        if (!string.IsNullOrWhiteSpace(normalizedCphh) && normalizedCphh.Length != 11)
            ModelState.AddModelError(editingOwnerId.HasValue ? nameof(EditOwnerCphh) : nameof(NewOwnerCphh), "CPHH must be 11 digits.");

        if (!string.IsNullOrWhiteSpace(type))
            ValidatePreviousOwnerUniqueness(type, editingOwnerId);
    }

    private void ValidatePreviousOwnerUniqueness(string type, int? editingOwnerId)
    {
        var typeDesc = OwnerTypeOptions.FirstOrDefault(t => t.Code == type)?.Description ?? string.Empty;
        if (!typeDesc.Contains("Previous", StringComparison.OrdinalIgnoreCase))
            return;

        var duplicateExists = StagedOtherOwners
            .Any(o => string.Equals(o.Type, type, StringComparison.OrdinalIgnoreCase)
                   && (!editingOwnerId.HasValue || (o.Id ?? 0) != editingOwnerId.Value));
        if (duplicateExists)
            ModelState.AddModelError(editingOwnerId.HasValue ? nameof(EditOwnerType) : nameof(NewOwnerType), "You can only have one owner of type Previous.");
    }

    private async Task PersistStagedOwnersAsync(string caseRbse)
    {
        var draft = await caseEditDraftState.GetAsync(caseRbse);
        if (draft is null || !draft.HasPendingChanges)
            return;

        var staged = draft.OtherOwners ?? [];
        var persisted = (await ownerRepository.GetByRbseAsync(caseRbse)).ToList();
        var persistedById = persisted.ToDictionary(x => x.Id);
        var stagedExistingIds = staged.Where(x => x.Id is > 0).Select(x => x.Id!.Value).ToHashSet();

        using var conn = connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        foreach (var removed in persisted.Where(x => !stagedExistingIds.Contains(x.Id)))
            await ownerRepository.DeleteAsync(removed.Id, removed.RowStamp ?? [], conn, tx);

        foreach (var owner in staged)
            await SaveStagedOwnerAsync(owner, persistedById, caseRbse, conn, tx);

        tx.Commit();
    }

    private async Task SaveStagedOwnerAsync(
        CaseEditDraftOtherOwnerItem owner,
        Dictionary<int, OtherOwnerRecord> persistedById,
        string caseRbse,
        System.Data.IDbConnection conn,
        System.Data.IDbTransaction tx)
    {
        var normalizedName = string.IsNullOrWhiteSpace(owner.Name) ? null : owner.Name;
        var normalizedCphh = string.IsNullOrWhiteSpace(owner.Cphh) ? null : owner.Cphh;

        if (owner.Id is > 0 && persistedById.TryGetValue(owner.Id.Value, out var persistedOwner))
        {
            var hasChanges = !string.Equals(owner.Type, persistedOwner.Type, StringComparison.Ordinal)
                             || !string.Equals(normalizedName, persistedOwner.Name, StringComparison.Ordinal)
                             || !string.Equals(normalizedCphh, persistedOwner.Cphh, StringComparison.Ordinal);

            if (!hasChanges)
                return;

            var rowStamp = string.IsNullOrWhiteSpace(owner.RowStampBase64)
                ? persistedOwner.RowStamp ?? []
                : Convert.FromBase64String(owner.RowStampBase64);

            await ownerRepository.EditAsync(new EditOtherOwnerCommand(
                owner.Id.Value,
                owner.Type,
                normalizedName,
                normalizedCphh,
                rowStamp), conn, tx);
        }
        else
        {
            await ownerRepository.AddAsync(new AddOtherOwnerCommand(
                caseRbse,
                owner.Type,
                normalizedName,
                normalizedCphh), conn, tx);
        }
    }

    private void ValidateVlaDomainRules()
    {
        var today = DateTime.Today;
        var formADate = Case.FormADate?.Date;

        // Legacy's CalendarDate.ascx control shows "Please enter a valid date" for
        // unparseable text and only uses the field-specific business-rule message once
        // the text parses but fails the earliest/latest range check. Match that: replace
        // the binder's generic format message with the same wording legacy used, without
        // altering the value the user typed.
        ReplaceUnparseableDateMessage("Case.BirthDate", Case.BirthDate);
        ReplaceUnparseableDateMessage("Case.PurchaseDate", Case.PurchaseDate);
        ReplaceUnparseableDateMessage("Case.HerdEntryDate", Case.HerdEntryDate);
        ReplaceUnparseableDateMessage("Case.OnsetDate", Case.OnsetDate);

        ValidateBirthDate(formADate, today);
        ValidatePurchaseDate(formADate, today);
        ValidateHerdEntryDate(formADate, today);
        ValidateOnsetDate(formADate, today);
        ValidateMonthsPregnantAndPostCalving();
        ValidateSlaughterDate(formADate, today);
    }

    private void ValidateBirthDate(DateTime? formADate, DateTime today)
    {
        if (!Case.BirthDate.HasValue)
            return;

        var limit = formADate ?? today;
        if (Case.BirthDate.Value.Date < DateTime.UnixEpoch || Case.BirthDate.Value.Date > limit)
            ModelState.AddModelError("Case.BirthDate", "Birth Date must be after 31/12/1969 and before the Form A Date");
    }

    private void ValidatePurchaseDate(DateTime? formADate, DateTime today)
    {
        if (!Case.PurchaseDate.HasValue)
            return;

        var limit = formADate ?? today;
        if ((Case.BirthDate.HasValue && Case.PurchaseDate.Value.Date < Case.BirthDate.Value.Date) ||
            Case.PurchaseDate.Value.Date > limit)
            ModelState.AddModelError("Case.PurchaseDate", "Purchase Date must be after the birth date and before the Form A Date");
    }

    private void ValidateHerdEntryDate(DateTime? formADate, DateTime today)
    {
        if (!Case.HerdEntryDate.HasValue)
            return;

        var limit = formADate ?? today;
        if (Case.HerdEntryDate.Value.Date > limit)
            ModelState.AddModelError("Case.HerdEntryDate", "The Herd Entry Date must be before the Form A Date.");
    }

    private void ValidateOnsetDate(DateTime? formADate, DateTime today)
    {
        if (!Case.OnsetDate.HasValue)
            return;

        var limit = formADate ?? today;
        if (Case.BirthDate.HasValue)
        {
            if (Case.OnsetDate.Value.Date < Case.BirthDate.Value.Date || Case.OnsetDate.Value.Date > limit)
                ModelState.AddModelError("Case.OnsetDate", "Onset Date must be after the Date Of Birth and before the Form A Date");
        }
        else if (Case.OnsetDate.Value.Date > limit)
            ModelState.AddModelError("Case.OnsetDate", "Onset Date must be before the Form A Date");
    }

    private void ValidateMonthsPregnantAndPostCalving()
    {
        if (Case.MonthsPregnant.HasValue && Case.MonthsPostCalving.HasValue)
            ModelState.AddModelError("Case.MonthsPostCalving", "You cannot enter a value for Month's Post Calving and Months Pregnant");

        if (Case.MonthsPregnant.HasValue && (Case.MonthsPregnant.Value < 1 || Case.MonthsPregnant.Value > 9))
            ModelState.AddModelError("Case.MonthsPregnant", "Months pregnant must be between 1 and 9.");
        if (Case.MonthsPostCalving.HasValue && (Case.MonthsPostCalving.Value < 1 || Case.MonthsPostCalving.Value > 3))
            ModelState.AddModelError("Case.MonthsPostCalving", "Months post calving must be between 1 and 3.");
    }

    private void ValidateSlaughterDate(DateTime? formADate, DateTime today)
    {
        if (!Case.SlaughterDate.HasValue)
            return;

        // Mirrors legacy SlaughterDateValid: earliest bound is Form A Date if present,
        // else Birth Date if present, else no earliest bound (only "not in the future" applies).
        if (formADate.HasValue)
        {
            if (Case.SlaughterDate.Value.Date < formADate.Value || Case.SlaughterDate.Value.Date > today)
                ModelState.AddModelError("Case.SlaughterDate", "You must enter a date between the Form A Date and todays date");
        }
        else if (Case.BirthDate.HasValue)
        {
            if (Case.SlaughterDate.Value.Date < Case.BirthDate.Value.Date || Case.SlaughterDate.Value.Date > today)
                ModelState.AddModelError("Case.SlaughterDate", "You must enter a date between the Birth Date and todays date");
        }
        else if (Case.SlaughterDate.Value.Date > today)
        {
            ModelState.AddModelError("Case.SlaughterDate", "You have entered a future date for the slaughter date");
        }
    }

    private void ReplaceUnparseableDateMessage(string key, DateTime? boundValue)
    {
        if (boundValue.HasValue)
            return;

        if (!ModelState.TryGetValue(key, out var entry) || entry.ValidationState != Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid)
            return;

        entry.Errors.Clear();
        ModelState.AddModelError(key, "Please enter a valid date");
    }

    public string OwnersSortUrl(string col)
    {
        var dir = string.Equals(OSort, col, StringComparison.OrdinalIgnoreCase) && ODir == "asc" ? "desc" : "asc";
        return $"?rbse={Uri.EscapeDataString(Rbse)}&OSort={col}&ODir={dir}&OPage=1";
    }

    private async Task LoadLookupsAsync()
    {
        var t1 = lookups.GetLookupAsync(LookupTableId.BirthDateSource);
        var t2 = lookups.GetLookupAsync(LookupTableId.Sex);
        var t3 = lookups.GetLookupAsync(LookupTableId.Breed);
        var t4 = lookups.GetLookupAsync(LookupTableId.AnimalOrigin);
        var t5 = lookups.GetLookupAsync(LookupTableId.BSECounty);
        var t6 = lookups.GetLookupAsync(LookupTableId.OwnerType);

        await Task.WhenAll(t1, t2, t3, t4, t5, t6);

        BirthDateSourceOptions  = await t1;
        SexOptions              = await t2;
        BreedOptions            = await t3;
        OriginOptions           = await t4;
        PurchasedCountyOptions  = await t5;
        OwnerTypeOptions        = await t6;
    }
}
