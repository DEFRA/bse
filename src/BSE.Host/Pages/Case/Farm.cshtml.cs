using BSE.Host.Services;
using BSE.Host.Models.ViewModels;
using BSE.Host.Models;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.Batch.Services;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Repositories;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using IGeoLookupService = BSE.Host.Services.IGeoLookupService;

namespace BSE.Host.Pages.Case;

/// <summary>
/// Farm tab for a case — mirrors the legacy CaseEntryFarm.aspx Farm tab.
/// Shows: confirmed case count, full farm details, linked farms (add/delete),
/// herd size history (add/delete), and supports inline staged Save/Cancel.
/// </summary>
[Authorize]
public class FarmModel(
    ICaseService caseService,
    IFarmService farmService,
    IFarmRelationRepository relationRepo,
    IHerdSizeRepository herdSizeRepo,
    ILookupDataService lookups,
    IBatchRepository batchRepository,
    IBatchService batchService,
    ICaseWizardStateService wizardState,
    ICaseFarmDraftStateService farmDraftState,
    ICaseScalarDraftStateService caseScalarDraftState,
    ICaseEditOrchestrationService caseEditOrchestration,
    ICurrentUserService currentUser,
    ILogger<FarmModel> logger,
    IConfiguration configuration,
    IGeoLookupService geoLookup) : PageModel
{
    private const int LinkedFarmCphhLength = 11;

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    // ── Create mode — shown instead of the tabs when no case exists yet for this RBSE
    // (legacy Home.aspx redirected a brand-new GB case straight to CaseEntryFarm.aspx) ──
    [BindProperty(SupportsGet = true)] public string NewCphh { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string? SelectedCphh { get; set; }
    [BindProperty(SupportsGet = true)] public bool ForceNewFarmDetails { get; set; }
    [BindProperty(SupportsGet = true)] public string? SeedParish { get; set; }
    [BindProperty(SupportsGet = true)] public string? SeedCounty { get; set; }
    [BindProperty(SupportsGet = true)] public int? SeedAdnsRegionId { get; set; }
    [BindProperty(SupportsGet = true)] public int? SeedAuthorityId { get; set; }
    [BindProperty(SupportsGet = true)] public int? SeedAuthorityCountyId { get; set; }
    [BindProperty(SupportsGet = true)] public string? SeedHerdmark1 { get; set; }
    [BindProperty(SupportsGet = true)] public string? SeedNumericHerdmark1 { get; set; }
    [BindProperty] public string? NewSurvey { get; set; }
    [BindProperty] public string? NewSex { get; set; }
    [BindProperty] public string? NewBreed { get; set; }
    [BindProperty] public string? NewEartagCountry { get; set; }
    [BindProperty] public string? NewEartagHerdmark { get; set; }
    [BindProperty] public string? NewEartag { get; set; }
    [BindProperty] public DateTime? NewBirthDate { get; set; }
    [BindProperty] public DateTime? NewFormADate { get; set; }
    [BindProperty] public string? NewFate { get; set; }
    [BindProperty] public string? NewOrigin { get; set; }
    [BindProperty] public string? NewNotes { get; set; }
    [BindProperty] public string? NewCaseType { get; set; }
    [BindProperty] public string? NewOwnerName { get; set; }
    [BindProperty] public string? NewAddress1 { get; set; }
    [BindProperty] public string? NewAddress2 { get; set; }
    [BindProperty] public string? NewAddress3 { get; set; }
    [BindProperty] public string? NewPostcode { get; set; }
    [BindProperty] public string? NewParish { get; set; }
    [BindProperty] public string? NewCounty { get; set; }
    [BindProperty] public string? NewAho { get; set; }
    [BindProperty] public int? NewAdnsRegionId { get; set; }

    /// <summary>True once a Farm lookup has confirmed the CPHH has no existing farm — shows the farm fields.</summary>
    public bool RequireFarmDetails { get; private set; }
    public IReadOnlyList<LookupItem> NewCountyOptions { get; private set; } = [];
    public IReadOnlyList<LookupItem> NewAhoOptions { get; private set; } = [];
    public IReadOnlyList<LuADNSRegion> NewAdnsOptions { get; private set; } = [];

    public CaseRecord? Case { get; private set; }
    public FarmRecord? Farm { get; private set; }
    public int ConfirmedCaseCount { get; private set; }
    public IReadOnlyList<FarmRelationRecord> LinkedFarms { get; private set; } = [];
    public IReadOnlyList<HerdSizeRecord> HerdSizes { get; private set; } = [];
    public string? ADNSRegionName { get; private set; }
    public string? CountyName { get; private set; }
    public string? AHOName { get; private set; }
    public string? HerdTypeName { get; private set; }
    public string? PedigreeTypeName { get; private set; }
    public string? AuthorityCountyName { get; private set; }
    public string? LocalAuthorityName { get; private set; }
    public IReadOnlyList<BatchNumberEntry> BatchNumbers { get; private set; } = [];
    public bool CanEditJointControls { get; private set; }
    public bool CanEditVlaControls { get; private set; }
    public bool CanEditCreateMode { get; private set; }

    /// <summary>Legacy MakeDEFRAControlsWritable/ReadOnly: every group except DEFRA Viewer can edit.</summary>
    public bool CanEditDefraControls { get; private set; }

    /// <summary>Legacy DEFRAViewerEnable(): shows the "CONFIDENTIAL DATA" banner and locks every field.</summary>
    public bool IsConfidentialViewOnly => !CanEditDefraControls;

    /// <summary>Legacy CPHH1.Enabled: DEFRA Data Entry/Maintenance and VLA Maintenance can look up a
    /// different farm for this case (VLA Data Entry and DEFRA Viewer cannot). Routed via the existing,
    /// validated /Case/MoveCase page rather than re-implementing farm-reassignment inline.</summary>
    public bool CanChangeCphh => User.IsInRole("DEFRAMaintenance");
    private List<FarmRelationRecord> PersistedLinkedFarms { get; set; } = [];
    private List<HerdSizeRecord> PersistedHerdSizes { get; set; } = [];

    /// <summary>Batch chosen on the home page and not yet saved against this case.</summary>
    public CaseWizardState? PendingBatch { get; private set; }

    /// <summary>True when the pending batch applies to the case currently open.</summary>
    public bool ShowBatchAssignment =>
        PendingBatch is not null
        && string.Equals(PendingBatch.RbseNumber, Rbse, StringComparison.OrdinalIgnoreCase)
        && User.IsInRole(VlaAccessRole);

    /// <summary>True when this case is already linked to the pending batch for the BSE1 document.</summary>
    public bool AlreadyInPendingBatch =>
        PendingBatch is not null
        && BatchNumbers.Any(b => b.BatchId == PendingBatch.BatchId
                              && string.Equals(b.Document, Bse1Document, StringComparison.OrdinalIgnoreCase));

    private const string Bse1Document = "BSE1";
    private const string VlaAccessRole = "VLAAccess";
    private const string DataEntryRole = "DataEntry";
    private const string EnterCphhMessage = "Enter a CPHH.";
    private const string AdnsRegionField = "EditableFarm.ADNSRegionID";
    private const string SpolSiteUrlConfigKey = "SpolSiteUrl";
    private const string ErrorMessageKey = "ErrorMessage";
    private const string HomePagePath = "/Home";

    // ── Table pagination / sort state (matches legacy DataGridPager PageLinkCount=10) ──
    public const int PageSize = 10;

    [BindProperty(SupportsGet = true)] public int    LPage { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string LSort { get; set; } = "cphh";
    [BindProperty(SupportsGet = true)] public string LDir  { get; set; } = "asc";
    public int LinkedFarmsTotalPages => Math.Max(1, (int)Math.Ceiling(SortedStagedLinkedFarms().Count / (double)PageSize));
    public int LinkedFarmsTotalCount => SortedStagedLinkedFarms().Count;
    public int LinkedFarmsCurrentPage => Math.Clamp(LPage, 1, LinkedFarmsTotalPages);

    [BindProperty(SupportsGet = true)] public int    HPage { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string HSort { get; set; } = "year";
    [BindProperty(SupportsGet = true)] public string HDir  { get; set; } = "desc";
    public int HerdSizesTotalPages => Math.Max(1, (int)Math.Ceiling(SortedStagedHerdSizes().Count / (double)PageSize));
    public int HerdSizesTotalCount => SortedStagedHerdSizes().Count;
    public int HerdSizesCurrentPage => Math.Clamp(HPage, 1, HerdSizesTotalPages);

    public string SpolSiteUrl { get; private set; } = string.Empty;

    [BindProperty] public FarmEditViewModel? EditableFarm { get; set; }
    [BindProperty] public List<StagedLinkedFarmItem> StagedLinkedFarms { get; set; } = [];
    [BindProperty] public List<StagedHerdSizeItem> StagedHerdSizes { get; set; } = [];

    // ── Inline "add/edit herd size row" state (GDS editable-grid pattern) ──────
    [BindProperty] public HerdSizeRowInput NewHerdRow { get; set; } = new();
    [BindProperty] public HerdSizeRowInput EditHerdRow { get; set; } = new();
    [BindProperty] public string? EditingClientKey { get; set; }
    [BindProperty] public string? EditingLinkedClientKey { get; set; }
    [BindProperty] public string? EditLinkedCphh { get; set; }
    [BindProperty] public string? NewLinkedCphh { get; set; }

    /// <summary>True to re-open the "add row" UI after a failed validation postback.</summary>
    public bool ShowAddHerdRow { get; private set; }
    public bool ShowAddLinkedRow { get; private set; }

    /// <summary>Set to the row being edited when its validation fails, so it re-opens.</summary>
    public string? ReopenEditClientKey { get; private set; }
    public string? ReopenLinkedEditClientKey { get; private set; }

    /// <summary>True when the draft has staged changes not yet committed by Save.</summary>
    public bool HasUnsavedChanges { get; private set; }

    // ── GET ────────────────────────────────────────────────────────────────────

    public async Task<IActionResult> OnGetAsync()
    {
        SpolSiteUrl = configuration[SpolSiteUrlConfigKey] ?? string.Empty;
        await LoadAsync();

        if (Case is null)
            return await OnGetNewCaseAsync();

        await LoadOrInitializeDraftStateAsync();
        return Page();
    }

    private async Task<IActionResult> OnGetNewCaseAsync()
    {
        CanEditCreateMode = CanEditCreateModeForCurrentUser();
        ApplyLegacyEditPermissions();
        EditableFarm ??= new FarmEditViewModel();

        await ResolveNewCaseCphhAsync(EditableFarm);

        if (Farm is null && !string.IsNullOrWhiteSpace(EditableFarm.CPHH))
            await LoadFromFarmCphhAsync(EditableFarm.CPHH);

        if (Farm is not null)
            await LoadOrInitializeDraftStateAsync();

        await LoadLookupsForEditAsync();
        return Page();
    }

    // Resolves the CPHH for a brand-new GB case from, in priority order: an in-progress
    // draft, the NewCphh query value, a user-selected existing farm, or seeded values for
    // a confirmed new farm. Mutates EditableFarm (may replace it entirely when a farm is selected).
    private async Task ResolveNewCaseCphhAsync(FarmEditViewModel editableFarm)
    {
        if (string.IsNullOrWhiteSpace(editableFarm.CPHH))
        {
            var existingDraft = await farmDraftState.GetAsync(Rbse);
            if (!string.IsNullOrWhiteSpace(existingDraft?.Cphh))
                editableFarm.CPHH = CphhNormalizer.Normalize(existingDraft.Cphh);
        }

        if (string.IsNullOrWhiteSpace(editableFarm.CPHH) && !string.IsNullOrWhiteSpace(NewCphh))
            editableFarm.CPHH = CphhNormalizer.Normalize(NewCphh);

        if (!string.IsNullOrWhiteSpace(SelectedCphh))
        {
            var selectedFarm = await farmService.GetByCphhAsync(CphhNormalizer.Normalize(SelectedCphh));
            if (selectedFarm is not null)
            {
                EditableFarm = FarmEditViewModel.FromRecord(selectedFarm);
                RequireFarmDetails = false;
            }
        }
        else if (ForceNewFarmDetails && !string.IsNullOrWhiteSpace(NewCphh))
        {
            editableFarm.CPHH = CphhNormalizer.Normalize(NewCphh);
            editableFarm.Parish = SeedParish;
            editableFarm.County = SeedCounty;
            editableFarm.ADNSRegionID = SeedAdnsRegionId;
            editableFarm.AuthorityID = SeedAuthorityId;
            editableFarm.AuthorityCountyID = SeedAuthorityCountyId;
            editableFarm.Herdmark1 = SeedHerdmark1;
            editableFarm.NumericHerdmark1 = SeedNumericHerdmark1;
            RequireFarmDetails = true;
        }
        else if (!string.IsNullOrWhiteSpace(editableFarm.CPHH))
        {
            editableFarm.CPHH = CphhNormalizer.Normalize(editableFarm.CPHH);
        }
    }

    /// <summary>Creates the case (and its farm, if the CPHH has none yet) for a brand-new GB case
    /// opened from the Home page — mirrors legacy CaseEntryFarm.aspx's create-mode behaviour.</summary>
    public async Task<IActionResult> OnPostCreateCaseAsync()
    {
        var postedEditableFarm = EditableFarm;

        SpolSiteUrl = configuration[SpolSiteUrlConfigKey] ?? string.Empty;
        await LoadAsync();
        EditableFarm = postedEditableFarm ?? EditableFarm;

        if (!CanEditCreateModeForCurrentUser())
            return Forbid();

        if (string.IsNullOrWhiteSpace(EditableFarm?.CPHH))
            ModelState.AddModelError("EditableFarm.CPHH", EnterCphhMessage);

        var normalisedCphh = CphhNormalizer.Normalize(EditableFarm?.CPHH);
        if (string.IsNullOrWhiteSpace(normalisedCphh))
            normalisedCphh = CphhNormalizer.Normalize(NewCphh);

        FarmRecord? farm = null;
        if (!string.IsNullOrWhiteSpace(normalisedCphh))
            farm = await farmService.GetByCphhAsync(normalisedCphh);

        RequireFarmDetails = farm is null;

        EditableFarm ??= new FarmEditViewModel();
        EditableFarm.CPHH = normalisedCphh;

        if (RequireFarmDetails)
            ValidateNewFarmDetails(EditableFarm);

        await ValidateNewFarmMapAndAdnsAsync(EditableFarm);

        if (!ModelState.IsValid)
        {
            await LoadLookupsForEditAsync();
            return Page();
        }

        var userId = await currentUser.GetUserIdAsync();

        if (RequireFarmDetails)
        {
            await farmService.AddAsync(EditableFarm.ToAddCommand(), userId);
        }
        else
        {
            await farmService.UpdateAsync(EditableFarm.ToUpdateCommand(farm?.RowStamp), userId);
        }

        var batchId = await GetOrCreateCaseBatchIdAsync();
        var command = BuildNewCaseCommand(normalisedCphh, batchId);
        var result = await caseService.CreateCaseAsync(command, userId);

        if (result != AddCaseResult.Success)
        {
            var message = result switch
            {
                AddCaseResult.DuplicateRbse => $"Case '{Rbse}' already exists.",
                AddCaseResult.InsertError => "Database error during insert.",
                AddCaseResult.AuditLogError => "Audit log error during create.",
                _ => $"Failed to create case: {result}"
            };
            ModelState.AddModelError("", message);
            await LoadLookupsForEditAsync();
            return Page();
        }

        await farmDraftState.ClearAsync(Rbse);
        TempData["SuccessMessage"] = $"Case {Rbse} created successfully.";
        return RedirectToPage("/Case/Farm", new { rbse = Rbse.Trim() });
    }

    private void ValidateNewFarmDetails(FarmEditViewModel editableFarm)
    {
        if (string.IsNullOrWhiteSpace(editableFarm.OwnerName))
            ModelState.AddModelError("EditableFarm.OwnerName", "Enter an owner name for the farm.");
        if (string.IsNullOrWhiteSpace(editableFarm.Address1))
            ModelState.AddModelError("EditableFarm.Address1", "Enter the first line of the farm address.");
        if (string.IsNullOrWhiteSpace(editableFarm.Parish))
            ModelState.AddModelError("EditableFarm.Parish", "Enter a parish for the farm.");
        if (string.IsNullOrWhiteSpace(editableFarm.County))
            ModelState.AddModelError("EditableFarm.County", "Specify a county for the farm.");
        if (string.IsNullOrWhiteSpace(editableFarm.AHO))
            ModelState.AddModelError("EditableFarm.AHO", "Specify an AHO for the farm.");
        if (editableFarm.ADNSRegionID is null)
            ModelState.AddModelError(AdnsRegionField, "Specify an ADNS region for the farm.");
    }

    private async Task ValidateNewFarmMapAndAdnsAsync(FarmEditViewModel editableFarm)
    {
        if (editableFarm.MapReference is { Length: >= 8 } mapRef
            && editableFarm.CPHH.Length >= 5
            && !await MapReferenceWithinParishAsync(editableFarm.CPHH, mapRef))
        {
            TempData["Warning"] = "Map reference does not lie within the parish boundaries for this CPHH.";
        }

        if (!IsAdnsCompatibleWithAuthoritySelection(editableFarm))
        {
            ModelState.AddModelError(AdnsRegionField, "ADNS region does not match the selected local authority. Please select ADNS region again.");
        }
    }

    // Legacy validated the batch on the Home page before redirecting to CaseEntryFarm.aspx.
    private async Task<int> GetOrCreateCaseBatchIdAsync()
    {
        var pendingBatch = await wizardState.GetAsync();
        if (pendingBatch is not null && string.Equals(pendingBatch.RbseNumber, Rbse, StringComparison.OrdinalIgnoreCase))
            return pendingBatch.BatchId;

        var batch = await batchService.GetOrCreateBatchNumberAsync();
        return batch.BatchId;
    }

    private UpdateCaseDetailsCommand BuildNewCaseCommand(string normalisedCphh, int batchId)
    {
        var addCase = new AddCaseCommand(
            Rbse: Rbse.Trim(), Cphh: normalisedCphh,
            EartagCountry: NewEartagCountry, EartagHerdmark: NewEartagHerdmark, Eartag: NewEartag,
            PreviousEartag: null, Bse1ReceivedDate: null, FormADate: NewFormADate,
            FormAResubmittedDate: null, FormBDate: null, Fate: NewFate, FormCDate: null,
            IsPurchaserBse1Received: false, IsBreederBse1Received: false,
            IsVendor1Bse1Received: false, IsHomebredBse1Received: false,
            IsSummarySheetReceived: false, IsPaperworkComplete: false,
            ReportedLocation: null, Survey: NewSurvey, Notes: NewNotes,
            BirthDate: NewBirthDate, IsBirthDateEst: NewBirthDate.HasValue ? false : null, DamStatus: null,
            BirthDateSource: null, ValuationAge: null, Sex: NewSex, Breed: NewBreed,
            Origin: NewOrigin, PurchaseDate: null, PurchaseAgeInMonths: null,
            PurchasedCounty: null, HerdEntryDate: null, OnsetDate: null,
            IsOnsetDateEst: null, MonthsPregnant: null, MonthsPostCalving: null,
            OnsetAgeInMonths: null, SlaughterDate: null, AlternateDiagnosis: null,
            LabComment: null, CaseType: NewCaseType);

        return new UpdateCaseDetailsCommand(
            addCase, batchId,
            Clinical: null, Bab: null,
            Feeds: [], Tests: [], OtherOwners: [],
            DamSire: null, ClinicalVisits: []);
    }

    public async Task<IActionResult> OnPostLookupNewCaseAsync()
    {
        SpolSiteUrl = configuration[SpolSiteUrlConfigKey] ?? string.Empty;
        await LoadAsync();
        if (!CanEditCreateModeForCurrentUser())
            return Forbid();

        var normalisedCphh = CphhNormalizer.Normalize(EditableFarm?.CPHH);

        if (IsNonGbFarmCphh(normalisedCphh))
        {
            ModelState.AddModelError("EditableFarm.CPHH", "The CPHH you have entered is for a non-GB Farm.");
            EditableFarm ??= new FarmEditViewModel();
            EditableFarm.CPHH = normalisedCphh;
            await LoadLookupsForEditAsync();
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(normalisedCphh))
        {
            var farm = await farmService.GetByCphhAsync(normalisedCphh);
            if (farm is not null)
            {
                RequireFarmDetails = false;
                EditableFarm = FarmEditViewModel.FromRecord(farm);
                await LoadLookupsForEditAsync();
                return Page();
            }
        }

        return RedirectToPage("/Case/PickFarm", new { rbse = Rbse, cphh = normalisedCphh });
    }

    private bool CanEditCreateModeForCurrentUser()
    {
        if (User.IsInRole("DEFRAAccess") && !User.IsInRole(DataEntryRole))
            return false;

        return User.IsInRole(DataEntryRole) || User.IsInRole("VLAMaintenance");
    }

    public async Task<IActionResult> OnPostAddLinkedFarmRowAsync()
    {
        if (!User.IsInRole(DataEntryRole))
            return Forbid();

        var postedCphh = NewLinkedCphh;

        SpolSiteUrl = configuration[SpolSiteUrlConfigKey] ?? string.Empty;
        await LoadAsync();

        if (!CanEditDefraControls)
            return RedirectToPage(new { rbse = Rbse });

        var draft = await LoadOrInitializeDraftStateAsync();

        var normalisedCphh = CphhNormalizer.Normalize(postedCphh);

        if (string.IsNullOrWhiteSpace(normalisedCphh))
            ModelState.AddModelError(nameof(NewLinkedCphh), EnterCphhMessage);

        if (!string.IsNullOrWhiteSpace(normalisedCphh) && normalisedCphh.Length != LinkedFarmCphhLength)
            ModelState.AddModelError(nameof(NewLinkedCphh), "Enter CPHH as 11 digits in the format NN/NNN/NNNN/NN.");

        if (!string.IsNullOrWhiteSpace(normalisedCphh)
            && string.Equals(CphhNormalizer.Normalize(Farm?.CPHH), normalisedCphh, StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(NewLinkedCphh), "Cannot link a farm to itself.");

        if (!string.IsNullOrWhiteSpace(normalisedCphh)
            && draft.LinkedFarms.Any(x => string.Equals(CphhNormalizer.Normalize(x.RelatedCphh), normalisedCphh, StringComparison.OrdinalIgnoreCase)))
            ModelState.AddModelError(nameof(NewLinkedCphh), $"CPHH {normalisedCphh} is already in the Linked Farms list.");

        if (!ModelState.IsValid)
        {
            ShowAddLinkedRow = true;
            NewLinkedCphh = postedCphh;
            return Page();
        }

        draft.LinkedFarms.Add(new CaseFarmDraftLinkedFarmItem
        {
            Id = 0,
            RelatedCphh = normalisedCphh,
            RowStampBase64 = string.Empty,
            Status = await GetLinkedFarmStatusAsync(normalisedCphh)
        });

        draft.HasPendingChanges = true;
        await farmDraftState.SetAsync(draft);

        return RedirectToPage(new { rbse = Rbse });
    }

    public async Task<IActionResult> OnPostSaveFarmAsync()
    {
        if (!User.IsInRole(DataEntryRole))
            return Forbid();

        var postedEditableFarm = EditableFarm;
        var postedFarmRowStamp = EditableFarmRowStampBase64;

        SpolSiteUrl = configuration[SpolSiteUrlConfigKey] ?? string.Empty;
        await LoadAsync();
        await LoadOrInitializeDraftStateAsync();
        EditableFarm = postedEditableFarm;
        EditableFarmRowStampBase64 = postedFarmRowStamp;

        ApplyLegacyJointAndVlaEditGuards();

        if (!TryValidateStagedCollections())
        {
            await LoadLookupsForEditAsync();
            return Page();
        }

        if (EditableFarm is not null)
            EditableFarm.CPHH = CphhNormalizer.Normalize(EditableFarm.CPHH);

        await LoadLookupsForEditAsync();

        if (await ValidateFarmForSaveAsync() is { } invalidFarmResult)
            return invalidFarmResult;

        byte[]? rowStamp = null;
        if (!string.IsNullOrWhiteSpace(EditableFarmRowStampBase64))
            rowStamp = Convert.FromBase64String(EditableFarmRowStampBase64);

        var userId = await currentUser.GetUserIdAsync();

        // Cross-tab staging (restores legacy's "one session, one commit" model): stage this
        // tab's edit into the shared draft, then commit *everything* staged for this RBSE
        // (this tab and/or Case (DEFRA)) together, rather than committing only Farm's fields.
        await StageFarmScalarEditAsync(EditableFarm!.ToUpdateCommand(rowStamp));

        EditCaseResult commitResult;
        try
        {
            commitResult = await caseEditOrchestration.CommitAllAsync(Rbse, userId);
        }
        catch (MandatoryCaseFieldsMissingException ex)
        {
            // Legacy parity: CaseEntrySave.aspx shows the consolidated list of missing items with
            // a "Return" button instead of a single inline banner.
            SaveResultModel.Stage(TempData, SaveResultMode.MissingMandatoryFields, ex.Errors);
            return RedirectToPage("/Case/SaveResult", new { rbse = Rbse });
        }

        if (commitResult == EditCaseResult.ConcurrencyConflict)
        {
            // Legacy parity: CaseEntrySave.aspx shows the failure and navigates to Home.aspx on
            // any commit failure, rather than staying on the originating tab.
            TempData["ErrorMessage"] = "Another user has modified this case since you loaded it. " +
                                       "Please reload and try again.";
            return RedirectToPage("/Home");
        }

        if (commitResult != EditCaseResult.Success)
        {
            TempData["ErrorMessage"] = $"Unable to save farm changes: {commitResult}.";
            return RedirectToPage("/Home");
        }

        await PersistStagedCollectionsAsync();
        await farmDraftState.ClearAsync(Rbse);

        // Legacy parity: CaseEntrySave.aspx auto-redirects to Home.aspx on a fully successful
        // save, clearing the session case state — not back to the tab the user was on.
        return RedirectToPage("/Home");
    }

    /// <summary>
    /// Validates the Farm fields and, if valid, stages them into the shared cross-tab draft
    /// (without committing) before navigating to another tab — restores legacy's
    /// "a tab's own validation blocks every navigation attempt, not just Save" behaviour.
    /// </summary>
    public async Task<IActionResult> OnPostStageAndGotoAsync(string targetPage)
    {
        if (!User.IsInRole(DataEntryRole))
            return Forbid();

        var postedEditableFarm = EditableFarm;
        var postedFarmRowStamp = EditableFarmRowStampBase64;

        SpolSiteUrl = configuration[SpolSiteUrlConfigKey] ?? string.Empty;
        await LoadAsync();
        await LoadOrInitializeDraftStateAsync();
        EditableFarm = postedEditableFarm;
        EditableFarmRowStampBase64 = postedFarmRowStamp;

        if (EditableFarm is null)
            return RedirectToPage(targetPage, new { rbse = Rbse });

        ApplyLegacyJointAndVlaEditGuards();
        EditableFarm.CPHH = CphhNormalizer.Normalize(EditableFarm.CPHH);

        await LoadLookupsForEditAsync();

        if (await ValidateFarmForSaveAsync() is not null)
            return Page();

        byte[]? rowStamp = null;
        if (!string.IsNullOrWhiteSpace(EditableFarmRowStampBase64))
            rowStamp = Convert.FromBase64String(EditableFarmRowStampBase64);

        await StageFarmScalarEditAsync(EditableFarm.ToUpdateCommand(rowStamp));

        return RedirectToPage(targetPage, new { rbse = Rbse });
    }

    /// <summary>Writes the Farm tab's current field values into the shared cross-tab scalar
    /// draft (BSE.Host.Services.CaseScalarDraftState), without committing to the database.</summary>
    private async Task StageFarmScalarEditAsync(UpdateFarmCommand command)
    {
        var draft = await caseScalarDraftState.GetAsync(Rbse) ?? new CaseScalarDraftState { Rbse = Rbse };
        draft.FarmBaseRowStampBase64 ??= EditableFarmRowStampBase64;
        draft.Farm = command;
        draft.HasPendingChanges = true;
        await caseScalarDraftState.SetAsync(draft);
    }

    private async Task<IActionResult?> ValidateFarmForSaveAsync()
    {
        if (EditableFarm is null)
            return Page();

        await ValidateFarmMapReferenceAsync(EditableFarm);
        ValidateRequiredFarmFields(EditableFarm);

        if (!ModelState.IsValid)
        {
            await LoadLookupsForEditAsync();
            return Page();
        }

        if (!IsAdnsCompatibleWithAuthoritySelection(EditableFarm))
        {
            ModelState.AddModelError(AdnsRegionField,
                "ADNS region does not match the selected local authority. Please select ADNS region again.");
            await LoadLookupsForEditAsync();
            return Page();
        }

        return null;
    }

    private async Task ValidateFarmMapReferenceAsync(FarmEditViewModel editableFarm)
    {
        if (editableFarm.MapReference is { Length: >= 8 } mapRef && editableFarm.CPHH.Length >= 5
            && !await MapReferenceWithinParishAsync(editableFarm.CPHH, mapRef))
        {
            ModelState.AddModelError("EditableFarm.MapRef1",
                "Map reference does not lie within the parish boundaries for this CPHH.");
        }
    }

    private void ValidateRequiredFarmFields(FarmEditViewModel editableFarm)
    {
        // Legacy parity: CaseEntryFarm.aspx's own Save (UpdateSessionWithCaseDetails) writes every
        // farm field unconditionally with no required-field checks of its own — Owner Name, Address 1,
        // Parish, County, AHO and ADNS Region are only ever enforced by the cross-tab
        // CheckMandatoryFields check on CaseEntrySave.aspx (CaseEditOrchestrationService.
        // CheckMandatoryFieldsAsync), which redirects to the SaveResult screen. Validating them again
        // here would block the user on this tab before that cross-tab check (and its SaveResult
        // redirect) is ever reached, so they are deliberately not repeated in this method.
        if (string.IsNullOrWhiteSpace(editableFarm.CPHH))
            ModelState.AddModelError("EditableFarm.CPHH", EnterCphhMessage);

        if (!string.IsNullOrWhiteSpace(editableFarm.NumericHerdmark1)
            && !IsValidNumericHerdmark(editableFarm.NumericHerdmark1))
            ModelState.AddModelError("EditableFarm.NumericHerdmark1", "Numeric herdmark 1 must be 6 digits.");

        if (!string.IsNullOrWhiteSpace(editableFarm.NumericHerdmark2)
            && !IsValidNumericHerdmark(editableFarm.NumericHerdmark2))
            ModelState.AddModelError("EditableFarm.NumericHerdmark2", "Numeric herdmark 2 must be 6 digits.");
    }

    public async Task<IActionResult> OnPostCancelFarmEditAsync() => await CancelFarmEditAsync();

    public async Task<IActionResult> OnGetCancelFarmEditAsync() => await CancelFarmEditAsync();

    private async Task<IActionResult> CancelFarmEditAsync()
    {
        await farmDraftState.ClearAsync(Rbse);
        await caseScalarDraftState.ClearAsync(Rbse);
        return RedirectToPage(HomePagePath);
    }

    // ── POST: Linked farms ─────────────────────────────────────────────────────

    public async Task<IActionResult> OnPostBeginEditLinkedFarmRowAsync()
    {
        if (!User.IsInRole(DataEntryRole))
            return Forbid();

        var clientKey = EditingLinkedClientKey;

        SpolSiteUrl = configuration[SpolSiteUrlConfigKey] ?? string.Empty;
        await LoadAsync();

        if (!CanEditDefraControls)
            return RedirectToPage(new { rbse = Rbse });

        var draft = await LoadOrInitializeDraftStateAsync();

        var item = draft.LinkedFarms.FirstOrDefault(x => x.ClientKey == clientKey);
        if (item is not null)
        {
            ReopenLinkedEditClientKey = clientKey;
            EditLinkedCphh = item.RelatedCphh;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateLinkedFarmRowAsync()
    {
        if (!User.IsInRole(DataEntryRole))
            return Forbid();

        var clientKey = EditingLinkedClientKey;
        var postedCphh = EditLinkedCphh;

        SpolSiteUrl = configuration[SpolSiteUrlConfigKey] ?? string.Empty;
        await LoadAsync();

        if (!CanEditDefraControls)
            return RedirectToPage(new { rbse = Rbse });

        var draft = await LoadOrInitializeDraftStateAsync();

        var item = draft.LinkedFarms.FirstOrDefault(x => x.ClientKey == clientKey);
        if (item is null)
        {
            TempData[ErrorMessageKey] = "The linked farm row being edited no longer exists.";
            return RedirectToPage(new { rbse = Rbse });
        }

        var normalisedCphh = CphhNormalizer.Normalize(postedCphh);
        if (string.IsNullOrWhiteSpace(normalisedCphh))
            ModelState.AddModelError(nameof(EditLinkedCphh), EnterCphhMessage);

        if (!string.IsNullOrWhiteSpace(normalisedCphh) && normalisedCphh.Length != LinkedFarmCphhLength)
            ModelState.AddModelError(nameof(EditLinkedCphh), "Enter CPHH as 11 digits in the format NN/NNN/NNNN/NN.");

        if (!string.IsNullOrWhiteSpace(normalisedCphh) && Farm is not null &&
            string.Equals(CphhNormalizer.Normalize(Farm.CPHH), normalisedCphh, StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(EditLinkedCphh), "Cannot link a farm to itself.");

        if (!string.IsNullOrWhiteSpace(normalisedCphh) &&
            draft.LinkedFarms.Any(x => x.ClientKey != clientKey && string.Equals(CphhNormalizer.Normalize(x.RelatedCphh), normalisedCphh, StringComparison.OrdinalIgnoreCase)))
            ModelState.AddModelError(nameof(EditLinkedCphh), "CPHH already exists in the Linked Farms list.");

        if (!ModelState.IsValid)
        {
            ReopenLinkedEditClientKey = clientKey;
            EditLinkedCphh = postedCphh;
            return Page();
        }

        item.RelatedCphh = normalisedCphh;
        item.Status = await GetLinkedFarmStatusAsync(normalisedCphh);
        draft.HasPendingChanges = true;
        await farmDraftState.SetAsync(draft);

        return RedirectToPage(new { rbse = Rbse });
    }

    public async Task<IActionResult> OnPostDeleteLinkedFarmAsync(string clientKey)
    {
        if (!User.IsInRole(DataEntryRole))
            return Forbid();

        SpolSiteUrl = configuration[SpolSiteUrlConfigKey] ?? string.Empty;
        await LoadAsync();

        if (!CanEditDefraControls)
            return RedirectToPage(new { rbse = Rbse });

        var draft = await LoadOrInitializeDraftStateAsync();
        var item = draft.LinkedFarms.FirstOrDefault(x => x.ClientKey == clientKey);
        if (item is not null)
        {
            draft.LinkedFarms.Remove(item);
            draft.HasPendingChanges = true;
            await farmDraftState.SetAsync(draft);
        }

        return RedirectToPage(new { rbse = Rbse });
    }


    // ── POST: Herd sizes (inline editable grid — GDS pattern) ─────────────────

    /// <summary>Opens the inline edit view for one staged herd size row (no changes saved yet).</summary>
    public async Task<IActionResult> OnPostBeginEditHerdRowAsync()
    {
        if (!User.IsInRole(DataEntryRole))
            return Forbid();

        var clientKey = EditingClientKey;

        SpolSiteUrl = configuration[SpolSiteUrlConfigKey] ?? string.Empty;
        await LoadAsync();
        var draft = await LoadOrInitializeDraftStateAsync();

        var item = draft.HerdSizes.FirstOrDefault(x => x.ClientKey == clientKey);
        if (item is not null)
        {
            ReopenEditClientKey = clientKey;
            EditHerdRow = new HerdSizeRowInput
            {
                HerdYear = item.HerdYear,
                TotalSize = item.TotalSize,
                Lactation1Size = item.Lactation1Size,
                Lactation2Size = item.Lactation2Size,
                Lactation3Size = item.Lactation3Size,
                Lactation4Size = item.Lactation4Size,
                Lactation5Size = item.Lactation5Size,
                Lactation6Size = item.Lactation6Size,
                Lactation7Size = item.Lactation7Size,
                Lactation8Size = item.Lactation8Size,
                Lactation9Size = item.Lactation9Size,
                Lactation10Size = item.Lactation10Size,
                Lactation10PlusSize = item.Lactation10PlusSize
            };
        }

        return Page();
    }

    /// <summary>Adds a new herd size row to the draft only. Not persisted until Save.</summary>
    public async Task<IActionResult> OnPostAddHerdSizeRowAsync()
    {
        if (!User.IsInRole(DataEntryRole))
            return Forbid();

        var postedRow = NewHerdRow;

        SpolSiteUrl = configuration[SpolSiteUrlConfigKey] ?? string.Empty;
        await LoadAsync();
        var draft = await LoadOrInitializeDraftStateAsync();

        ValidateHerdRow(postedRow, prefix: nameof(NewHerdRow));

        if (!ModelState.IsValid)
        {
            NewHerdRow = postedRow;
            ShowAddHerdRow = true;
            return Page();
        }

        postedRow.Normalize();

        draft.HerdSizes.Add(new CaseFarmDraftHerdSizeItem
        {
            ClientKey = Guid.NewGuid().ToString("N"),
            Id = null,
            HerdYear = postedRow.HerdYear!.Value,
            TotalSize = postedRow.TotalSize!.Value,
            Lactation1Size = postedRow.Lactation1Size,
            Lactation2Size = postedRow.Lactation2Size,
            Lactation3Size = postedRow.Lactation3Size,
            Lactation4Size = postedRow.Lactation4Size,
            Lactation5Size = postedRow.Lactation5Size,
            Lactation6Size = postedRow.Lactation6Size,
            Lactation7Size = postedRow.Lactation7Size,
            Lactation8Size = postedRow.Lactation8Size,
            Lactation9Size = postedRow.Lactation9Size,
            Lactation10Size = postedRow.Lactation10Size,
            Lactation10PlusSize = postedRow.Lactation10PlusSize
        });
        draft.HasPendingChanges = true;
        await farmDraftState.SetAsync(draft);

        return RedirectToPage(new { rbse = Rbse });
    }

    /// <summary>Updates a staged herd size row in the draft only. Not persisted until Save.</summary>
    public async Task<IActionResult> OnPostUpdateHerdSizeRowAsync()
    {
        if (!User.IsInRole(DataEntryRole))
            return Forbid();

        var clientKey = EditingClientKey;
        var postedRow = EditHerdRow;

        SpolSiteUrl = configuration[SpolSiteUrlConfigKey] ?? string.Empty;
        await LoadAsync();
        var draft = await LoadOrInitializeDraftStateAsync();

        var item = draft.HerdSizes.FirstOrDefault(x => x.ClientKey == clientKey);
        if (item is null)
        {
            TempData[ErrorMessageKey] = "The herd size row being edited no longer exists.";
            return RedirectToPage(new { rbse = Rbse });
        }

        ValidateHerdRow(postedRow, prefix: nameof(EditHerdRow));

        if (!ModelState.IsValid)
        {
            EditHerdRow = postedRow;
            ReopenEditClientKey = clientKey;
            return Page();
        }

        item.HerdYear = postedRow.HerdYear!.Value;
        item.TotalSize = postedRow.TotalSize!.Value;
        postedRow.Normalize();
        item.Lactation1Size = postedRow.Lactation1Size;
        item.Lactation2Size = postedRow.Lactation2Size;
        item.Lactation3Size = postedRow.Lactation3Size;
        item.Lactation4Size = postedRow.Lactation4Size;
        item.Lactation5Size = postedRow.Lactation5Size;
        item.Lactation6Size = postedRow.Lactation6Size;
        item.Lactation7Size = postedRow.Lactation7Size;
        item.Lactation8Size = postedRow.Lactation8Size;
        item.Lactation9Size = postedRow.Lactation9Size;
        item.Lactation10Size = postedRow.Lactation10Size;
        item.Lactation10PlusSize = postedRow.Lactation10PlusSize;
        draft.HasPendingChanges = true;
        await farmDraftState.SetAsync(draft);

        return RedirectToPage(new { rbse = Rbse });
    }

    public async Task<IActionResult> OnPostDeleteHerdSizeAsync(string clientKey)
    {
        if (!User.IsInRole(DataEntryRole))
            return Forbid();

        SpolSiteUrl = configuration[SpolSiteUrlConfigKey] ?? string.Empty;
        await LoadAsync();
        var draft = await LoadOrInitializeDraftStateAsync();
        var item = draft.HerdSizes.FirstOrDefault(x => x.ClientKey == clientKey);
        if (item is not null)
        {
            draft.HerdSizes.Remove(item);
            draft.HasPendingChanges = true;
            await farmDraftState.SetAsync(draft);
        }

        return RedirectToPage(new { rbse = Rbse });
    }

    // DB CHECK constraints on [dbo].[HerdSize]: HerdYear between 1975 and the current year;
    // TotalSize between 1 and 2000; each Lactation value fits legacy's 3-digit textbox (0–999).
    private const int MinHerdYear = 1975;
    private const int MinTotalSize = 1;
    private const int MaxTotalSize = 1999;
    private const int MinLactationSize = 0;
    private const int MaxLactationSize = 999;

    /// <summary>Shared validation for the add/edit herd size row inline forms.</summary>
    private void ValidateHerdRow(
        HerdSizeRowInput row,
        string prefix)
    {
        var maxHerdYear = DateTime.UtcNow.Year;

        if (row.HerdYear is null)
            ModelState.AddModelError($"{prefix}.HerdYear", "Enter a year");
        else if (row.HerdYear < MinHerdYear || row.HerdYear > maxHerdYear)
            ModelState.AddModelError($"{prefix}.HerdYear", $"Enter a year between {MinHerdYear} and {maxHerdYear}");

        if (row.TotalSize is null)
            ModelState.AddModelError($"{prefix}.TotalSize", "Enter a total herd size");
        else if (row.TotalSize < MinTotalSize || row.TotalSize > MaxTotalSize)
            ModelState.AddModelError($"{prefix}.TotalSize", $"Enter a total herd size between {MinTotalSize} and {MaxTotalSize}");

        foreach (var (label, value) in LactationValues(row))
        {
            if (value is not null && (value < MinLactationSize || value > MaxLactationSize))
                ModelState.AddModelError($"{prefix}.{label}", $"Lactation {label switch { "Lactation10PlusSize" => "10+", _ => label.Replace("Lactation", "").Replace("Size", "") }} must be between {MinLactationSize} and {MaxLactationSize}");
        }

        // Legacy parity: lactation-total mismatch is shown as a warning marker in the grid,
        // not a blocking validation error.
    }

    private static IEnumerable<(string PropertyName, int? Value)> LactationValues(HerdSizeRowInput row)
    {
        yield return (nameof(row.Lactation1Size), row.Lactation1Size);
        yield return (nameof(row.Lactation2Size), row.Lactation2Size);
        yield return (nameof(row.Lactation3Size), row.Lactation3Size);
        yield return (nameof(row.Lactation4Size), row.Lactation4Size);
        yield return (nameof(row.Lactation5Size), row.Lactation5Size);
        yield return (nameof(row.Lactation6Size), row.Lactation6Size);
        yield return (nameof(row.Lactation7Size), row.Lactation7Size);
        yield return (nameof(row.Lactation8Size), row.Lactation8Size);
        yield return (nameof(row.Lactation9Size), row.Lactation9Size);
        yield return (nameof(row.Lactation10Size), row.Lactation10Size);
        yield return (nameof(row.Lactation10PlusSize), row.Lactation10PlusSize);
    }

    private static IEnumerable<(string PropertyName, int? Value)> LactationValues(HerdSizeFormViewModel row)
    {
        yield return (nameof(row.Lactation1Size), row.Lactation1Size);
        yield return (nameof(row.Lactation2Size), row.Lactation2Size);
        yield return (nameof(row.Lactation3Size), row.Lactation3Size);
        yield return (nameof(row.Lactation4Size), row.Lactation4Size);
        yield return (nameof(row.Lactation5Size), row.Lactation5Size);
        yield return (nameof(row.Lactation6Size), row.Lactation6Size);
        yield return (nameof(row.Lactation7Size), row.Lactation7Size);
        yield return (nameof(row.Lactation8Size), row.Lactation8Size);
        yield return (nameof(row.Lactation9Size), row.Lactation9Size);
        yield return (nameof(row.Lactation10Size), row.Lactation10Size);
        yield return (nameof(row.Lactation10PlusSize), row.Lactation10PlusSize);
    }

    /// <summary>Legacy warning text: lactation-total mismatch is informational, not blocking.</summary>

    // ── POST: Batch assignment (legacy CaseEntryFarm.aspx Save/Cancel) ─────────

    public async Task<IActionResult> OnPostSaveBatchAsync()
    {
        if (!User.IsInRole(VlaAccessRole))
            return Forbid();

        var pending = await wizardState.GetAsync();
        if (pending is null || !string.Equals(pending.RbseNumber, Rbse, StringComparison.OrdinalIgnoreCase))
        {
            TempData[ErrorMessageKey] = "No batch was selected. Return to the home page and choose a batch number.";
            return RedirectToPage(new { rbse = Rbse });
        }

        // Legacy uniqueness is on (BatchID, RBSE, Document), so a case may belong to several
        // batches — only re-adding the same batch is a duplicate.
        var alreadyInPendingBatch = (await batchRepository.GetBatchNumbersByRbseAsync(Rbse))
            .Any(b => b.BatchId == pending.BatchId
                   && string.Equals(b.Document, Bse1Document, StringComparison.OrdinalIgnoreCase));

        if (alreadyInPendingBatch)
        {
            await wizardState.ClearAsync();
            TempData["Warning"] =
                $"Case {RbseHelper.Format(Rbse)} is already assigned to batch {pending.BatchNumber}. No change was made.";
            return RedirectToPage(new { rbse = Rbse });
        }

        var userId = await currentUser.GetUserIdAsync();
        var result = await batchService.AssignCaseToBatchAsync(pending.BatchId, Rbse, Bse1Document);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Batch assignment {Result}: user {UserId} assigned RBSE {Rbse} to batch {BatchId} ({BatchNumber}) for document {Document}",
                result, userId, Rbse, pending.BatchId, pending.BatchNumber, Bse1Document);
        }

        await wizardState.ClearAsync();

        TempData[result switch
        {
            BatchAssignmentResult.Success => "Success",
            BatchAssignmentResult.AlreadyAssigned => "Warning",
            _ => ErrorMessageKey
        }] = result switch
        {
            BatchAssignmentResult.Success =>
                $"Case {RbseHelper.Format(Rbse)} has been assigned to batch {pending.BatchNumber}.",
            BatchAssignmentResult.AlreadyAssigned =>
                $"Case {RbseHelper.Format(Rbse)} is already assigned to batch {pending.BatchNumber}. No change was made.",
            BatchAssignmentResult.BatchNotFound =>
                $"Batch {pending.BatchNumber} no longer exists. The case was not assigned.",
            _ => "The case could not be assigned to the batch."
        };

        return RedirectToPage(new { rbse = Rbse });
    }

    public async Task<IActionResult> OnPostCancelBatchAsync()
    {
        if (!User.IsInRole(VlaAccessRole))
            return Forbid();

        var pending = await wizardState.GetAsync();
        await wizardState.ClearAsync();

        // Return to the batch assignment screen with the previous selections retained.
        var parts = (pending?.BatchNumber ?? "").Split('/');
        if (parts.Length == 2
            && short.TryParse(parts[0], out var year)
            && int.TryParse(parts[1], out var number))
        {
            return RedirectToPage(HomePagePath, new { batchYear = year, batchNumber = number });
        }

        return RedirectToPage(HomePagePath);
    }

    // ── AJAX: farm status for a CPHH (mirrors legacy GetRelatedFarmDetails) ────

    public async Task<IActionResult> OnGetLinkedFarmStatusAsync(string? cphh)
    {
        if (string.IsNullOrWhiteSpace(cphh))
            return new JsonResult(new { status = (string?)null });

        var normalised = cphh.Trim().ToUpperInvariant().Replace("/", "");
        if (normalised.Length == 0 || normalised.Length > 11)
            return new JsonResult(new { status = (string?)null });

        var status = await GetLinkedFarmStatusAsync(normalised);

        return new JsonResult(new { status });
    }

    private async Task<string> GetLinkedFarmStatusAsync(string cphh)
    {
        var farm = await farmService.GetByCphhAsync(cphh);
        return !string.IsNullOrWhiteSpace(farm?.OwnerName)
            ? $"{farm.OwnerName}, {farm.Address1}"
            : "BSE Free";
    }

    public async Task<IActionResult> OnGetAuthoritiesAsync(int? authorityCountyId)
    {
        if (authorityCountyId is null or 0) return new JsonResult(Array.Empty<object>());
        var items = await lookups.GetAuthoritiesByCountyAsync(authorityCountyId.Value);
        return new JsonResult(items.Select(a => new { id = a.Id, name = a.Name }));
    }

    public async Task<IActionResult> OnGetAdnsRegionsAsync(int? authorityId)
    {
        if (authorityId is null or 0) return new JsonResult(Array.Empty<object>());
        var items = await lookups.GetADNSRegionsByAuthorityAsync(authorityId.Value);
        return new JsonResult(items.Select(r => new { id = r.Id, name = r.Name }));
    }

    public async Task<IActionResult> OnGetEstimateMapReferenceAsync(string? cphh)
    {
        if (string.IsNullOrEmpty(cphh) || cphh.Length < 5)
            return new JsonResult(new { error = "CPHH must be at least 5 characters to estimate a map reference." });

        var county = cphh[..2];
        var parish = cphh[2..5];

        var rows = await geoLookup.GetAllParishMapReferencesAsync(county, parish);
        if (rows.Count == 0)
            return new JsonResult(new { error = "No map reference data found for the parish associated with this CPHH." });

        double rowCount = rows.Count;
        double middleIdx = rowCount % 2 != 0 ? rowCount / 2 + 0.5 : rowCount / 2;
        middleIdx -= 1;
        var row = rows[(int)middleIdx];

        var xCoord = row.XReference1;
        var centreY = CentreCoordinate(row.YReference1, row.YReference2);

        var xPrefixCoord = xCoord[1].ToString();
        var yPrefixRaw = centreY[..2];
        var yPrefixCoord = yPrefixRaw[0] == '0' ? yPrefixRaw[1].ToString() : yPrefixRaw;

        var code = await geoLookup.GetPrefixCodeAsync(xPrefixCoord, yPrefixCoord);
        if (code is null)
            return new JsonResult(new { error = "Could not determine OS grid square for this parish." });

        return new JsonResult(new
        {
            mapRef1 = code,
            mapRef2 = xCoord[2..4] + "5",
            mapRef3 = centreY[2..4] + "5"
        });
    }

    public async Task<IActionResult> OnGetValidateMapReferenceAsync(string? cphh, string? mapRef)
    {
        if (string.IsNullOrWhiteSpace(cphh) || string.IsNullOrWhiteSpace(mapRef))
            return new JsonResult(new { valid = true });

        cphh = CphhNormalizer.Normalize(cphh);
        mapRef = mapRef.Trim().ToUpperInvariant();

        if (cphh.Length < 5 || mapRef.Length < 8)
            return new JsonResult(new { valid = true });

        var valid = await MapReferenceWithinParishAsync(cphh, mapRef);
        return valid
            ? new JsonResult(new { valid = true })
            : new JsonResult(new { valid = false, message = "Map reference is outside the parish boundaries." });
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        Case = await caseService.GetCaseAsync(Rbse);
        CanEditCreateMode = Case is null && CanEditCreateModeForCurrentUser();
        var batchTask = batchRepository.GetBatchNumbersByRbseAsync(Rbse);

        var farmCphh = Case?.Cphh;
        if (string.IsNullOrWhiteSpace(farmCphh))
            farmCphh = CphhNormalizer.Normalize(EditableFarm?.CPHH ?? SelectedCphh ?? NewCphh);

        await Task.WhenAll(LoadFromFarmCphhAsync(farmCphh), batchTask);
        BatchNumbers = (await batchTask).ToList().AsReadOnly();
        PendingBatch = await wizardState.GetAsync();
        ApplyLegacyEditPermissions();

        if (Farm is not null)
        {
            EditableFarm = FarmEditViewModel.FromRecord(Farm);
            EditableFarmRowStampBase64 = Farm.RowStamp is null ? string.Empty : Convert.ToBase64String(Farm.RowStamp);
            await LoadLookupsForEditAsync();

            // Cross-tab staging overlay: if another tab's Save (or a Farm-tab navigation)
            // already staged a Farm edit that hasn't been committed yet, show it instead of
            // silently reverting to the last-committed DB values.
            var stagedScalars = await caseScalarDraftState.GetAsync(Rbse);
            if (stagedScalars?.Farm is not null)
                EditableFarm.ApplyStagedCommand(stagedScalars.Farm);
        }
    }

    private void ApplyLegacyEditPermissions()
    {
        // Mirrors legacy CaseEntryFarm:
        // - DEFRA Data Entry / DEFRA Maintenance: joint controls writable, VLA controls read-only.
        // - VLA Data Entry / VLA Maintenance: joint + VLA controls writable only when
        //   IsVLAAllowedMainCaseEdit(Session) is true.
        // - everyone else: read-only.
        if (!User.IsInRole(DataEntryRole))
        {
            CanEditJointControls = false;
            CanEditVlaControls = false;
            CanEditDefraControls = false;
            return;
        }

        CanEditDefraControls = true;

        var isVlaGroup = User.IsInRole(VlaAccessRole);
        if (!isVlaGroup)
        {
            CanEditJointControls = true;
            CanEditVlaControls = false;
            return;
        }

        var canVlaMainEdit = IsVlaAllowedMainCaseEdit();
        CanEditJointControls = canVlaMainEdit;
        CanEditVlaControls = canVlaMainEdit;
    }

    private bool IsVlaAllowedMainCaseEdit()
    {
        // Legacy Common.vb IsVLAAllowedMainCaseEdit(Session):
        // Session[SV_BatchNumber] <> "" OR Session[SV_BatchNumbersTable].Rows.Count > 0
        var hasCurrentBatchSelection = PendingBatch is not null
            && string.Equals(RbseHelper.ParseToRaw(PendingBatch.RbseNumber), RbseHelper.ParseToRaw(Rbse), StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(PendingBatch.BatchNumber);

        var hasAnyBatchHistory = BatchNumbers.Count > 0;
        return hasCurrentBatchSelection || hasAnyBatchHistory;
    }

    private void ApplyLegacyJointAndVlaEditGuards()
    {
        if (EditableFarm is null || Farm is null)
            return;

        if (!CanEditJointControls)
        {
            EditableFarm.Herdmark1 = Farm.Herdmark1;
            EditableFarm.Herdmark2 = Farm.Herdmark2;
            EditableFarm.Herdmark3 = Farm.Herdmark3;
            EditableFarm.NumericHerdmark1 = Farm.NumericHerdmark1;
            EditableFarm.NumericHerdmark2 = Farm.NumericHerdmark2;
        }

        if (!CanEditVlaControls)
        {
            EditableFarm.PedigreeType = Farm.PedigreeType;
        }
    }

    private async Task LoadFromFarmCphhAsync(string? cphh)
    {
        if (string.IsNullOrWhiteSpace(cphh))
            return;

        var farmTask        = farmService.GetByCphhAsync(cphh);
        var confirmedTask   = farmService.GetConfirmedCaseCountAsync(cphh);
        var linkedTask      = farmService.GetRelatedFarmsAsync(cphh);
        var herdTask        = farmService.GetHerdSizesAsync(cphh);
        var adnsTask        = lookups.GetADNSRegionsAsync();
        var countyTask      = lookups.GetLookupAsync(LookupTableId.BSECounty);
        var ahoTask         = lookups.GetLookupAsync(LookupTableId.AHO);
        var herdTypeTask    = lookups.GetHerdTypesAsync();
        var pedigreeTask    = lookups.GetLookupAsync(LookupTableId.PedigreeType);
        var authCountyTask  = lookups.GetLookupAsync(LookupTableId.AuthorityCounty);

        await Task.WhenAll(farmTask, confirmedTask, linkedTask, herdTask, adnsTask,
                           countyTask, ahoTask, herdTypeTask, pedigreeTask, authCountyTask);

        Farm              = await farmTask;
        ConfirmedCaseCount = await confirmedTask;

        ApplyLinkedFarmsSortingAndPaging((await linkedTask).ToList());
        ApplyHerdSizesSortingAndPaging((await herdTask).ToList());

        if (Farm is null) return;

        // All ten tasks above already completed via Task.WhenAll, so these awaits are instant.
        ResolveFarmLookupNames(Farm, await adnsTask, await countyTask, await ahoTask, await herdTypeTask, await pedigreeTask, await authCountyTask);

        // Resolve LocalAuthority ID → name (filter by county so the SP is called with the right county)
        if (Farm.AuthorityID.HasValue && Farm.AuthorityCountyID.HasValue)
        {
            var authorities = await lookups.GetAuthoritiesByCountyAsync(Farm.AuthorityCountyID.Value);
            LocalAuthorityName = authorities.FirstOrDefault(a => a.Id == Farm.AuthorityID.Value)?.Name;
        }
    }

    private void ApplyLinkedFarmsSortingAndPaging(List<FarmRelationRecord> allLinked)
    {
        PersistedLinkedFarms = allLinked;

        Func<FarmRelationRecord, object?> keySelector = LSort == "status"
            ? f => f.Status
            : f => f.RelatedCPHH;
        var sortedLinked = LDir == "desc"
            ? allLinked.OrderByDescending(keySelector)
            : allLinked.OrderBy(keySelector);

        LPage = Math.Clamp(LPage, 1, Math.Max(1, (int)Math.Ceiling(allLinked.Count / (double)PageSize)));
        LinkedFarms = sortedLinked.Skip((LPage - 1) * PageSize).Take(PageSize).ToList().AsReadOnly();
    }

    private void ApplyHerdSizesSortingAndPaging(List<HerdSizeRecord> allHerd)
    {
        PersistedHerdSizes = allHerd;
        var sortedHerd = OrderHerdSizesByColumn(allHerd);
        HPage = Math.Clamp(HPage, 1, Math.Max(1, (int)Math.Ceiling(allHerd.Count / (double)PageSize)));
        HerdSizes = sortedHerd.Skip((HPage - 1) * PageSize).Take(PageSize).ToList().AsReadOnly();
    }

    private IEnumerable<HerdSizeRecord> OrderHerdSizesByColumn(IReadOnlyCollection<HerdSizeRecord> allHerd)
    {
        var ascending = HDir == "asc";
        return HSort switch
        {
            "total"  => OrderHerdSizes(allHerd, h => h.TotalSize, ascending),
            "lac1"   => OrderHerdSizes(allHerd, h => h.Lactation1Size, ascending),
            "lac2"   => OrderHerdSizes(allHerd, h => h.Lactation2Size, ascending),
            "lac3"   => OrderHerdSizes(allHerd, h => h.Lactation3Size, ascending),
            "lac4"   => OrderHerdSizes(allHerd, h => h.Lactation4Size, ascending),
            "lac5"   => OrderHerdSizes(allHerd, h => h.Lactation5Size, ascending),
            "lac6"   => OrderHerdSizes(allHerd, h => h.Lactation6Size, ascending),
            "lac7"   => OrderHerdSizes(allHerd, h => h.Lactation7Size, ascending),
            "lac8"   => OrderHerdSizes(allHerd, h => h.Lactation8Size, ascending),
            "lac9"   => OrderHerdSizes(allHerd, h => h.Lactation9Size, ascending),
            "lac10"  => OrderHerdSizes(allHerd, h => h.Lactation10Size, ascending),
            "lac10p" => OrderHerdSizes(allHerd, h => h.Lactation10PlusSize, ascending),
            _        => OrderHerdSizes(allHerd, h => h.HerdYear, ascending)
        };
    }

    private static IOrderedEnumerable<HerdSizeRecord> OrderHerdSizes<T>(
        IEnumerable<HerdSizeRecord> source,
        Func<HerdSizeRecord, T> keySelector,
        bool ascending)
        => ascending
            ? source.OrderBy(keySelector)
            : source.OrderByDescending(keySelector);

    // Resolves the display names for each FK/code column on the farm.
    private void ResolveFarmLookupNames(
        FarmRecord farm,
        IEnumerable<LuADNSRegion> adnsRegions,
        IEnumerable<LookupItem> counties,
        IEnumerable<LookupItem> ahos,
        IEnumerable<LuHerdType> herdTypes,
        IEnumerable<LookupItem> pedigreeTypes,
        IEnumerable<LookupItem> authCounties)
    {
        // Resolve ADNS region name
        if (farm.ADNSRegionID.HasValue)
            ADNSRegionName = adnsRegions.FirstOrDefault(r => r.Id == farm.ADNSRegionID.Value)?.Name;

        // Resolve County code → description  (Farm.County FK → luBSECounty.Code)
        if (!string.IsNullOrWhiteSpace(farm.County))
            CountyName = counties.FirstOrDefault(c => c.Code == farm.County.Trim())?.Description;

        // Resolve AHO code → name  (Farm.AHO FK → luAHO.Code)
        if (!string.IsNullOrWhiteSpace(farm.AHO))
            AHOName = ahos.FirstOrDefault(a => a.Code == farm.AHO.Trim())?.Description;

        // Resolve HerdType code → description  (Farm.HerdType FK → luHerdType.Code)
        if (!string.IsNullOrWhiteSpace(farm.HerdType))
            HerdTypeName = herdTypes.FirstOrDefault(h => h.Code == farm.HerdType.Trim())?.Description;

        // Resolve PedigreeType code → description  (Farm.PedigreeType FK → luPedigreeType.Code)
        if (!string.IsNullOrWhiteSpace(farm.PedigreeType))
            PedigreeTypeName = pedigreeTypes.FirstOrDefault(p => p.Code == farm.PedigreeType.Trim())?.Description;

        // Resolve AuthorityCounty ID → county name
        if (farm.AuthorityCountyID.HasValue)
            AuthorityCountyName = authCounties.FirstOrDefault(a => a.Id == farm.AuthorityCountyID.Value)?.Description;
    }

    [BindProperty]
    public string EditableFarmRowStampBase64 { get; set; } = string.Empty;

    private bool IsAdnsCompatibleWithAuthoritySelection(FarmEditViewModel model)
    {
        if (model.AuthorityID is not > 0)
            return true;

        if (model.ADNSRegionID is not > 0)
            return true;

        if (ViewData["AdnsOptions"] is IEnumerable<LuADNSRegion> options)
            return options.Any(x => x.Id == model.ADNSRegionID.Value);

        return true;
    }

    private static bool IsNonGbFarmCphh(string? cphh)
    {
        var normalised = CphhNormalizer.Normalize(cphh);
        return normalised.StartsWith("00", StringComparison.Ordinal);
    }

    private static bool IsValidNumericHerdmark(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length == 6 && trimmed.All(char.IsDigit);
    }

    private async Task LoadLookupsForEditAsync()
    {
        var countyTask = lookups.GetLookupAsync(LookupTableId.BSECounty);
        var ahoTask = lookups.GetLookupAsync(LookupTableId.AHO);
        var herdTypeTask = lookups.GetHerdTypesAsync();
        var pedigreeTask = lookups.GetLookupAsync(LookupTableId.PedigreeType);
        var authCountyTask = lookups.GetLookupAsync(LookupTableId.AuthorityCounty);

        await Task.WhenAll(countyTask, ahoTask, herdTypeTask, pedigreeTask, authCountyTask);

        ViewData["CountyOptions"] = await countyTask;
        ViewData["AhoOptions"] = await ahoTask;
        ViewData["HerdTypeOptions"] = await herdTypeTask;
        ViewData["PedigreeOptions"] = await pedigreeTask;
        ViewData["AuthorityCountyOptions"] = await authCountyTask;

        ViewData["AuthorityOptions"] = EditableFarm?.AuthorityCountyID is > 0
            ? await lookups.GetAuthoritiesByCountyAsync(EditableFarm.AuthorityCountyID.Value)
            : (IEnumerable<LuAuthority>)[];

        ViewData["AdnsOptions"] = EditableFarm?.AuthorityID is > 0
            ? await lookups.GetADNSRegionsByAuthorityAsync(EditableFarm.AuthorityID.Value)
            : (IEnumerable<LuADNSRegion>)[];
    }

    private async Task<bool> MapReferenceWithinParishAsync(string cphh, string mapRef)
    {
        if (cphh.Length < 5 || mapRef.Length < 8) return true;

        var county = cphh[..2];
        var parish = cphh[2..5];

        var prefixData = await geoLookup.GetXYCoordsByPrefixCodeAsync(mapRef[..2].ToUpperInvariant());
        if (prefixData is null) return false;

        var rows = await geoLookup.GetAllParishMapReferencesAsync(county, parish);
        if (rows.Count == 0) return true;

        var sXCoord = prefixData.XCoordPrefix + mapRef[2..4];
        var sYCoord = prefixData.YCoordPrefix + mapRef[5..7];

        return rows.Any(r =>
            sXCoord == r.XReference1
            && string.Compare(sYCoord, r.YReference1, StringComparison.Ordinal) >= 0
            && string.Compare(sYCoord, r.YReference2, StringComparison.Ordinal) <= 0);
    }

    private bool TryValidateStagedCollections()
    {
        var hasLinkedErrors = ValidateLinkedFarmCollection();
        var hasHerdErrors = ValidateHerdSizeCollection();
        return !hasLinkedErrors && !hasHerdErrors;
    }

    private bool ValidateLinkedFarmCollection()
    {
        var hasLinkedErrors = false;
        var seenLinked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var currentFarmCphh = CphhNormalizer.Normalize(Farm?.CPHH);

        for (var i = 0; i < StagedLinkedFarms.Count; i++)
        {
            var item = StagedLinkedFarms[i];
            item.RelatedCphh = CphhNormalizer.Normalize(item.RelatedCphh);

            if (string.IsNullOrWhiteSpace(item.RelatedCphh))
            {
                ModelState.AddModelError("", "Enter a CPHH for each linked farm row.");
                hasLinkedErrors = true;
                continue;
            }

            if (item.RelatedCphh.Length != LinkedFarmCphhLength)
            {
                ModelState.AddModelError("", $"Linked farm CPHH {item.RelatedCphh} must be 11 digits.");
                hasLinkedErrors = true;
                continue;
            }

            if (string.Equals(item.RelatedCphh, currentFarmCphh, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("", "Cannot link a farm to itself.");
                hasLinkedErrors = true;
            }

            if (!seenLinked.Add(item.RelatedCphh))
            {
                ModelState.AddModelError("", $"Linked farm CPHH {item.RelatedCphh} is duplicated.");
                hasLinkedErrors = true;
            }
        }

        return hasLinkedErrors;
    }

    private bool ValidateHerdSizeCollection()
    {
        var hasHerdErrors = false;
        var maxHerdYear = DateTime.UtcNow.Year;

        for (var i = 0; i < StagedHerdSizes.Count; i++)
        {
            var item = StagedHerdSizes[i];
            if (item.HerdYear < MinHerdYear || item.HerdYear > maxHerdYear)
            {
                ModelState.AddModelError("", $"Herd size row {i + 1}: year must be {MinHerdYear}\u2013{maxHerdYear}.");
                hasHerdErrors = true;
            }

            if (item.TotalSize < MinTotalSize || item.TotalSize > MaxTotalSize)
            {
                ModelState.AddModelError("", $"Herd size row {i + 1}: total size must be between {MinTotalSize} and {MaxTotalSize}.");
                hasHerdErrors = true;
            }

            if (LactationValues(item).Any(x => x.Value < MinLactationSize || x.Value > MaxLactationSize))
            {
                ModelState.AddModelError("", $"Herd size row {i + 1}: lactation values must be between {MinLactationSize} and {MaxLactationSize}.");
                hasHerdErrors = true;
            }
        }

        return hasHerdErrors;
    }

    private async Task<CaseFarmDraftState> LoadOrInitializeDraftStateAsync()
    {
        if (Farm is null)
            return new CaseFarmDraftState { Rbse = Rbse };

        var draft = await farmDraftState.GetAsync(Rbse);
        if (draft is null)
        {
            draft = new CaseFarmDraftState
            {
                Rbse = Rbse,
                Cphh = Farm.CPHH,
                LinkedFarms = PersistedLinkedFarms.Select(x => new CaseFarmDraftLinkedFarmItem
                {
                    Id = x.ID,
                    RelatedCphh = x.RelatedCPHH,
                    RowStampBase64 = x.RowStamp is null ? string.Empty : Convert.ToBase64String(x.RowStamp),
                    Status = x.Status ?? string.Empty
                }).ToList(),
                HerdSizes = PersistedHerdSizes.Select(x => new CaseFarmDraftHerdSizeItem
                {
                    ClientKey = Guid.NewGuid().ToString("N"),
                    Id = x.ID,
                    HerdYear = x.HerdYear,
                    TotalSize = x.TotalSize,
                    Lactation1Size = x.Lactation1Size,
                    Lactation2Size = x.Lactation2Size,
                    Lactation3Size = x.Lactation3Size,
                    Lactation4Size = x.Lactation4Size,
                    Lactation5Size = x.Lactation5Size,
                    Lactation6Size = x.Lactation6Size,
                    Lactation7Size = x.Lactation7Size,
                    Lactation8Size = x.Lactation8Size,
                    Lactation9Size = x.Lactation9Size,
                    Lactation10Size = x.Lactation10Size,
                    Lactation10PlusSize = x.Lactation10PlusSize,
                    RowStampBase64 = x.RowStamp is null ? string.Empty : Convert.ToBase64String(x.RowStamp)
                }).ToList()
            };
            await farmDraftState.SetAsync(draft);
        }

        StagedLinkedFarms = draft.LinkedFarms.Select(x => new StagedLinkedFarmItem
        {
            ClientKey = x.ClientKey,
            Id = x.Id,
            RelatedCphh = x.RelatedCphh,
            RowStampBase64 = x.RowStampBase64,
            Status = x.Status
        }).ToList();

        StagedHerdSizes = draft.HerdSizes.Select(x => new StagedHerdSizeItem
        {
            Id = x.Id,
            ClientKey = x.ClientKey,
            HerdYear = x.HerdYear,
            TotalSize = x.TotalSize,
            Lactation1Size = x.Lactation1Size,
            Lactation2Size = x.Lactation2Size,
            Lactation3Size = x.Lactation3Size,
            Lactation4Size = x.Lactation4Size,
            Lactation5Size = x.Lactation5Size,
            Lactation6Size = x.Lactation6Size,
            Lactation7Size = x.Lactation7Size,
            Lactation8Size = x.Lactation8Size,
            Lactation9Size = x.Lactation9Size,
            Lactation10Size = x.Lactation10Size,
            Lactation10PlusSize = x.Lactation10PlusSize,
            RowStampBase64 = x.RowStampBase64
        }).ToList();

        HasUnsavedChanges = draft.HasPendingChanges;

        return draft;
    }

    private async Task PersistStagedCollectionsAsync()
    {
        if (Farm is null)
            return;

        await PersistLinkedFarmsAsync(Farm);
        await PersistHerdSizesAsync(Farm);
    }

    private async Task PersistLinkedFarmsAsync(FarmRecord farm)
    {
        var persistedLinkedById = PersistedLinkedFarms.ToDictionary(x => x.ID);
        var stagedLinkedByExistingId = StagedLinkedFarms.Where(x => x.Id is > 0).ToDictionary(x => x.Id!.Value);

        foreach (var removed in PersistedLinkedFarms.Where(x => !stagedLinkedByExistingId.ContainsKey(x.ID) && x.RowStamp is not null))
            await relationRepo.DeleteAsync(removed.ID, removed.RowStamp!);

        foreach (var newRow in StagedLinkedFarms.Where(x => x.Id is null || x.Id <= 0))
            await relationRepo.AddAsync(farm.CPHH, newRow.RelatedCphh);

        foreach (var staged in StagedLinkedFarms.Where(x => x.Id is > 0))
        {
            if (!persistedLinkedById.TryGetValue(staged.Id!.Value, out var persisted))
                continue;

            var persistedCphh = CphhNormalizer.Normalize(persisted.RelatedCPHH);
            var stagedCphh = CphhNormalizer.Normalize(staged.RelatedCphh);

            if (string.Equals(persistedCphh, stagedCphh, StringComparison.OrdinalIgnoreCase))
                continue;

            var rowStamp = string.IsNullOrWhiteSpace(staged.RowStampBase64)
                ? persisted.RowStamp
                : Convert.FromBase64String(staged.RowStampBase64);

            if (rowStamp is not null)
                await relationRepo.UpdateAsync(staged.Id.Value, stagedCphh, rowStamp);
        }
    }

    private async Task PersistHerdSizesAsync(FarmRecord farm)
    {
        var persistedHerdById = PersistedHerdSizes.ToDictionary(x => x.ID);
        var stagedHerdByExistingId = StagedHerdSizes.Where(x => x.Id is > 0).ToDictionary(x => x.Id!.Value);

        foreach (var removed in PersistedHerdSizes.Where(x => !stagedHerdByExistingId.ContainsKey(x.ID) && x.RowStamp is not null))
            await herdSizeRepo.DeleteAsync(removed.ID, removed.RowStamp!);

        foreach (var newRow in StagedHerdSizes.Where(x => x.Id is null || x.Id <= 0))
        {
            await herdSizeRepo.AddAsync(new AddHerdSizeCommand(
                farm.CPHH,
                (short)newRow.HerdYear,
                (short)newRow.TotalSize,
                (short?)newRow.Lactation1Size,
                (short?)newRow.Lactation2Size,
                (short?)newRow.Lactation3Size,
                (short?)newRow.Lactation4Size,
                (short?)newRow.Lactation5Size,
                (short?)newRow.Lactation6Size,
                (short?)newRow.Lactation7Size,
                (short?)newRow.Lactation8Size,
                (short?)newRow.Lactation9Size,
                (short?)newRow.Lactation10Size,
                (short?)newRow.Lactation10PlusSize));
        }

        foreach (var staged in StagedHerdSizes.Where(x => x.Id is > 0))
        {
            if (!persistedHerdById.TryGetValue(staged.Id!.Value, out var persisted))
                continue;

            if (!HasHerdSizeChanges(persisted, staged))
                continue;

            var rowStamp = string.IsNullOrWhiteSpace(staged.RowStampBase64)
                ? persisted.RowStamp
                : Convert.FromBase64String(staged.RowStampBase64);

            if (rowStamp is null)
                continue;

            await herdSizeRepo.UpdateAsync(new UpdateHerdSizeCommand(
                staged.Id.Value,
                (short)staged.HerdYear,
                (short)staged.TotalSize,
                (short?)staged.Lactation1Size,
                (short?)staged.Lactation2Size,
                (short?)staged.Lactation3Size,
                (short?)staged.Lactation4Size,
                (short?)staged.Lactation5Size,
                (short?)staged.Lactation6Size,
                (short?)staged.Lactation7Size,
                (short?)staged.Lactation8Size,
                (short?)staged.Lactation9Size,
                (short?)staged.Lactation10Size,
                (short?)staged.Lactation10PlusSize,
                rowStamp));
        }
    }

    private static bool HasHerdSizeChanges(HerdSizeRecord persisted, StagedHerdSizeItem staged) =>
        persisted.HerdYear != staged.HerdYear
        || persisted.TotalSize != staged.TotalSize
        || persisted.Lactation1Size != staged.Lactation1Size
        || persisted.Lactation2Size != staged.Lactation2Size
        || persisted.Lactation3Size != staged.Lactation3Size
        || persisted.Lactation4Size != staged.Lactation4Size
        || persisted.Lactation5Size != staged.Lactation5Size
        || persisted.Lactation6Size != staged.Lactation6Size
        || persisted.Lactation7Size != staged.Lactation7Size
        || persisted.Lactation8Size != staged.Lactation8Size
        || persisted.Lactation9Size != staged.Lactation9Size
        || persisted.Lactation10Size != staged.Lactation10Size
        || persisted.Lactation10PlusSize != staged.Lactation10PlusSize;

    private static string CentreCoordinate(string start, string end)
    {
        if (!int.TryParse(start, out var s) || !int.TryParse(end, out var e)) return start;
        var middle = (int)Math.Round((s + e) / 2.0, MidpointRounding.AwayFromZero);
        return middle.ToString("0000");
    }

    // ── Sort / pagination URL builders ─────────────────────────────────────────

    public string LinkedFarmsSortUrl(string col)
    {
        var dir = string.Equals(LSort, col, StringComparison.OrdinalIgnoreCase) && LDir == "asc" ? "desc" : "asc";
        return $"?LSort={col}&LDir={dir}&LPage=1&HSort={HSort}&HDir={HDir}&HPage={HPage}";
    }

    public string LinkedFarmsPageUrl(int page) =>
        $"?LPage={page}&LSort={LSort}&LDir={LDir}&HSort={HSort}&HDir={HDir}&HPage={HPage}";

    public string HerdSizeSortUrl(string col)
    {
        var dir = string.Equals(HSort, col, StringComparison.OrdinalIgnoreCase) && HDir == "asc" ? "desc" : "asc";
        return $"?HSort={col}&HDir={dir}&HPage=1&LSort={LSort}&LDir={LDir}&LPage={LPage}";
    }

    public string HerdSizeAriaSort(string col)
    {
        if (!string.Equals(HSort, col, StringComparison.OrdinalIgnoreCase))
            return "none";
        return HDir == "asc" ? "ascending" : "descending";
    }

    public string LinkedFarmAriaSort(string col)
    {
        if (!string.Equals(LSort, col, StringComparison.OrdinalIgnoreCase))
            return "none";
        return LDir == "asc" ? "ascending" : "descending";
    }

    public string HerdSizesPageUrl(int page) =>
        $"?HPage={page}&HSort={HSort}&HDir={HDir}&LPage={LPage}&LSort={LSort}&LDir={LDir}";

    public IReadOnlyList<StagedLinkedFarmItem> SortedStagedLinkedFarms() =>
        GetSortedStagedLinkedFarms();

    public IReadOnlyList<StagedLinkedFarmItem> PagedStagedLinkedFarms() =>
        GetPagedItems(GetSortedStagedLinkedFarms(), LinkedFarmsCurrentPage);

    public IReadOnlyList<StagedHerdSizeItem> PagedSortedStagedHerdSizes() =>
        GetPagedItems(GetSortedStagedHerdSizes(), HerdSizesCurrentPage);

    public IReadOnlyList<StagedHerdSizeItem> SortedStagedHerdSizes() =>
        GetSortedStagedHerdSizes();

    private List<StagedLinkedFarmItem> GetSortedStagedLinkedFarms()
    {
        var ordered = (LSort, LDir) switch
        {
            ("status", "desc") => StagedLinkedFarms.OrderByDescending(x => x.Status),
            ("status", _) => StagedLinkedFarms.OrderBy(x => x.Status),
            (_, "desc") => StagedLinkedFarms.OrderByDescending(x => x.RelatedCphh),
            _ => StagedLinkedFarms.OrderBy(x => x.RelatedCphh)
        };

        return ordered.ToList();
    }

    private static List<T> GetPagedItems<T>(IReadOnlyList<T> source, int pageNumber)
        => source
            .Skip((pageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();

    private List<StagedHerdSizeItem> GetSortedStagedHerdSizes()
    {
        var orderedSource = SortHerdSizesByColumn(StagedHerdSizes);
        return orderedSource.ToList();
    }

    private IEnumerable<StagedHerdSizeItem> SortHerdSizesByColumn(IEnumerable<StagedHerdSizeItem> source)
    {
        return (HSort, HDir) switch
        {
            ("total", "asc") => source.OrderBy(h => h.TotalSize),
            ("total", _) => source.OrderByDescending(h => h.TotalSize),
            ("lac1", "asc") => source.OrderBy(h => h.Lactation1Size),
            ("lac1", _) => source.OrderByDescending(h => h.Lactation1Size),
            ("lac2", "asc") => source.OrderBy(h => h.Lactation2Size),
            ("lac2", _) => source.OrderByDescending(h => h.Lactation2Size),
            ("lac3", "asc") => source.OrderBy(h => h.Lactation3Size),
            ("lac3", _) => source.OrderByDescending(h => h.Lactation3Size),
            ("lac4", "asc") => source.OrderBy(h => h.Lactation4Size),
            ("lac4", _) => source.OrderByDescending(h => h.Lactation4Size),
            ("lac5", "asc") => source.OrderBy(h => h.Lactation5Size),
            ("lac5", _) => source.OrderByDescending(h => h.Lactation5Size),
            ("lac6", "asc") => source.OrderBy(h => h.Lactation6Size),
            ("lac6", _) => source.OrderByDescending(h => h.Lactation6Size),
            ("lac7", "asc") => source.OrderBy(h => h.Lactation7Size),
            ("lac7", _) => source.OrderByDescending(h => h.Lactation7Size),
            ("lac8", "asc") => source.OrderBy(h => h.Lactation8Size),
            ("lac8", _) => source.OrderByDescending(h => h.Lactation8Size),
            ("lac9", "asc") => source.OrderBy(h => h.Lactation9Size),
            ("lac9", _) => source.OrderByDescending(h => h.Lactation9Size),
            ("lac10", "asc") => source.OrderBy(h => h.Lactation10Size),
            ("lac10", _) => source.OrderByDescending(h => h.Lactation10Size),
            ("lac10p", "asc") => source.OrderBy(h => h.Lactation10PlusSize),
            ("lac10p", _) => source.OrderByDescending(h => h.Lactation10PlusSize),
            ("year", "asc") => source.OrderBy(h => h.HerdYear),
            _ => source.OrderByDescending(h => h.HerdYear)
        };
    }

    // ── View models ────────────────────────────────────────────────────────────

    public class StagedLinkedFarmItem
    {
        public string ClientKey { get; set; } = Guid.NewGuid().ToString("N");
        public int? Id { get; set; }
        public string RelatedCphh { get; set; } = string.Empty;
        public string RowStampBase64 { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class StagedHerdSizeItem : HerdSizeFormViewModel
    {
        public int? Id { get; set; }
        public string ClientKey { get; set; } = string.Empty;
        public string RowStampBase64 { get; set; } = string.Empty;

        public string? LactationMismatchWarning
        {
            get
            {
                var lactationTotal = this.Total();

                return lactationTotal > 0 && lactationTotal != TotalSize
                    ? $"the lactation total ({lactationTotal}) does not equal the total herd size ({TotalSize})."
                    : null;
            }
        }

        /// <summary>True for rows staged locally that do not yet exist in the database.</summary>
        public bool IsUnsaved => Id is null or <= 0;
    }

    public class HerdSizeFormViewModel : LactationSizeFields
    {
        public int HerdYear { get; set; }
        public int TotalSize { get; set; }
    }

    /// <summary>
    /// Binding target for the inline add/edit herd size row. Fields are nullable so that
    /// blank optional lactation inputs bind to null instead of tripping ASP.NET Core's
    /// implicit "value must not be null" error for non-nullable value types.
    /// </summary>
    public class HerdSizeRowInput : LactationSizeFields
    {
        public int? HerdYear { get; set; }
        public int? TotalSize { get; set; }
    }
}
