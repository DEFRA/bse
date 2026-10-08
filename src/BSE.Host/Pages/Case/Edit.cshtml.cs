using BSE.Host.Models.ViewModels;
using BSE.Host.Helpers;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.CaseWork.Commands;
using BSE.Modules.CaseWork.Repositories;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace BSE.Host.Pages.Case;

[Authorize]
public class EditModel(
    ICaseService caseService,
    ICurrentUserService currentUserService,
    ILookupDataService lookups,
    ICaseWorkRepository caseWorkRepository,
    ITestRepository testRepository,
    ICaseEditDraftStateService caseEditDraftState,
    ICaseScalarDraftStateService caseScalarDraftState,
    ICaseEditOrchestrationService caseEditOrchestration,
    IBatchRepository batchRepository,
    IConfiguration configuration) : PageModel
{
    private const string RowStampKey = "CaseEdit_RowStamp_{0}";
    private const string DataEntryRole = "DataEntry";
    private const string VlaAccessRole = "VLAAccess";
    private const string VlaMaintenanceRole = "VLAMaintenance";

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    [BindProperty]
    public CaseEditViewModel Case { get; set; } = new();

    public string? ConcurrencyError { get; private set; }
    public IReadOnlyList<BatchNumberEntry> BatchNumbers { get; private set; } = [];
    public bool HasCaseWorkLink { get; private set; }
    public bool CanEditDefraNotes { get; private set; }

    /// <summary>Legacy EnableControls' writable roles: DEFRA Data Entry, DEFRA Maintenance and
    /// VLA Maintenance all call MakeControlsWritable(); DEFRA Viewer and VLA Data Entry call
    /// MakeControlsReadOnly(). Gates both the editable fieldset and the Save/Casework POST handlers.</summary>
    public bool CanEditCaseFields { get; private set; }

    /// <summary>Legacy VLAMaintenanceEnable's 3-way gate: Barcode/AHF Reference are writable only
    /// for VLA Maintenance, only when a CaseWork row exists, and only while the case isn't closed.
    /// DEFRA Data Entry/Maintenance never get these two fields (always disabled there, unambiguous
    /// in legacy). Paperwork Complete Date's own DEFRA-role gating is left as the existing
    /// HasCaseWork-only check — legacy's own behaviour there is genuinely ambiguous/ViewState-
    /// dependent (see Field-level parity review, sixth follow-up) and migrated's simpler, more
    /// consistent rule was a deliberate choice, not revisited here.</summary>
    public bool CanEditVlaMaintenanceCaseworkFields { get; private set; }

    public bool IsNonGbCase { get; private set; }

    // Lookup options for dropdowns
    public IEnumerable<BSE.SharedKernel.ILookupItem> FateOptions { get; private set; } = [];
    public IEnumerable<BSE.SharedKernel.ILookupItem> SurveyOptions { get; private set; } = [];
    public IEnumerable<BSE.SharedKernel.ILookupItem> ReportedLocationOptions { get; private set; } = [];
    public IEnumerable<BSE.SharedKernel.ILookupItem> BirthDateSourceOptions { get; private set; } = [];
    public IEnumerable<BSE.SharedKernel.ILookupItem> ValuationAgeOptions { get; private set; } = [];
    public IEnumerable<BSE.SharedKernel.ILookupItem> CaseTypeOptions { get; private set; } = [];

    // Tests grid
    public IReadOnlyList<CaseTestRecord> Tests { get; private set; } = [];
    [BindProperty] public List<StagedTestItem> StagedTests { get; set; } = [];
    public bool HasUnsavedChanges { get; private set; }

    // View Docs — SharePoint URL (RBSE appended by view, slashes stripped)
    public string SpolSiteUrl { get; private set; } = string.Empty;

    public IEnumerable<ILookupItem> TestTypeOptions { get; private set; } = [];
    public IEnumerable<ILookupItem> TestResultOptions { get; private set; } = [];

    [BindProperty] public string NewTestType { get; set; } = string.Empty;
    [BindProperty] public string? NewTestResult { get; set; }
    [BindProperty] public int EditingTestId { get; set; }
    [BindProperty] public string EditTestType { get; set; } = string.Empty;
    [BindProperty] public string? EditTestResult { get; set; }
    [BindProperty] public string EditTestRowStampBase64 { get; set; } = string.Empty;
    public bool ShowAddTestRow { get; private set; }
    public int? ReopenEditTestId { get; private set; }

    private const int TestsPageSize = 10;

    // Anchor on the test records table, so adding/editing a row returns the user to the grid
    // instead of the top of a long form.
    private const string TestsAnchor = "test-records";

    [BindProperty(SupportsGet = true)] public int    TPage { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string TSort { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public string TDir  { get; set; } = "asc";
    public int TestsTotalPages { get; private set; } = 1;
    public int TestsTotalCount { get; private set; }


    public async Task<IActionResult> OnGetAsync()
    {
        // Legacy parity: CaseEntryDEFRA.aspx.vb's Page_Load redirects to SessionError.aspx when
        // Session(SV_RBSENumber) is missing (session timeout, direct URL access, stale back-button).
        // The migrated app has no session state to check, but a blank Rbse is the exact same
        // "arrived here with no required context" scenario.
        if (string.IsNullOrWhiteSpace(Rbse))
            return RedirectToPage("/SessionError");

        ApplyLegacyDefraPermissions();

        var record = await caseService.GetCaseAsync(Rbse);
        if (record is null)
        {
            Case.Rbse = Rbse;
            var missingCaseBatchTask = batchRepository.GetBatchNumbersByRbseAsync(Rbse);
            SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
            var missingCaseWork = await caseWorkRepository.GetByRbseAsync(Rbse);
            HasCaseWorkLink = missingCaseWork is not null
                              || await caseWorkRepository.GetEntryByRbseAsync(Rbse) is not null;
            await Task.WhenAll(LoadLookupsAsync(), missingCaseBatchTask, LoadOrInitializeDraftStateAsync());
            BatchNumbers = (await missingCaseBatchTask).ToList().AsReadOnly();
            TempData["Warning"] = $"Case '{Rbse}' is not saved yet. Complete Farm first.";
            return Page();
        }

        TempData[string.Format(RowStampKey, Rbse)] = Convert.ToBase64String(record.RowStamp ?? []);
        Case = CaseEditViewModel.FromRecord(record);
        IsNonGbCase = record.IsNonGbCase;

        var caseWork = await caseWorkRepository.GetByRbseAsync(Rbse);
        if (caseWork is not null)
            Case.ApplyCaseWork(caseWork);

        // Re-run now that Case.HasCaseWork/IsCaseClosed reflect the freshly-loaded record (the
        // earlier call near the top of this method only had the role-based flags available).
        ApplyLegacyDefraPermissions();

        HasCaseWorkLink = caseWork is not null
                          || await caseWorkRepository.GetEntryByRbseAsync(Rbse) is not null;

        var batchTask = batchRepository.GetBatchNumbersByRbseAsync(Rbse);
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;

        await Task.WhenAll(LoadLookupsAsync(), batchTask);
        await LoadOrInitializeDraftStateAsync();
        await ApplyStagedCaseOverlayAsync();
        BatchNumbers = (await batchTask).ToList().AsReadOnly();
        return Page();
    }

    public async Task<IActionResult> OnPostBeginEditTestRowAsync(int id)
    {
        if (!CanUserEditCase())
            return Forbid();

        var postedCase = Case;

        await LoadReadonlyPageAsync();
        await RestoreAndRestageCaseEditAsync(postedCase);
        await LoadOrInitializeDraftStateAsync();

        var test = StagedTests.FirstOrDefault(t => t.Id == id);
        if (test is null)
            return RedirectToTestsAnchor();

        ReopenEditTestId = id;
        EditTestType = test.TestType;
        EditTestResult = test.TestResult;
        EditTestRowStampBase64 = test.RowStampBase64;
        return Page();
    }

    public async Task<IActionResult> OnPostAddTestRowAsync()
    {
        if (!CanUserEditCase())
            return Forbid();

        var postedCase = Case;

        await LoadReadonlyPageAsync();
        await RestoreAndRestageCaseEditAsync(postedCase);
        await LoadOrInitializeDraftStateAsync();

        if (string.IsNullOrWhiteSpace(NewTestType))
            ModelState.AddModelError(nameof(NewTestType), "Select a test type.");

        // CaseTest.TestResult is NOT NULL with an FK to luTestResult, so a blank is unsaveable.
        if (string.IsNullOrWhiteSpace(NewTestResult))
            ModelState.AddModelError(nameof(NewTestResult), "Select a test result.");

        if (!ModelState.IsValid)
        {
            ShowAddTestRow = true;
            return Page();
        }

        StagedTests.Add(new StagedTestItem
        {
            Id = NextTemporaryTestId(),
            TestType = NewTestType,
            TestResult = NewTestResult,
            RowStampBase64 = string.Empty
        });

        await SaveDraftStateAsync();
        return RedirectToTestsAnchor();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!CanUserEditCase())
            return Forbid();

        var postedCase = Case;

        await LoadReadonlyPageAsync();
        await RestoreAndRestageCaseEditAsync(postedCase);
        await LoadOrInitializeDraftStateAsync();

        await PersistStagedTestsAsync();
        await caseEditDraftState.ClearAsync(Rbse);
        TempData["Success"] = "Case test changes saved.";
        return RedirectToPage(new { rbse = Rbse });
    }

    public async Task<IActionResult> OnGetCancelEditAsync()
    {
        await caseEditDraftState.ClearAsync(Rbse);
        await caseScalarDraftState.ClearAsync(Rbse);
        return RedirectToPage("/Home");
    }

    /// <summary>Live eartag validation for the help tooltip, reusing the server-side rules so the
    /// hint reflects the same pass/fail result as Save — mirrors legacy's ThreePartEartag postback.</summary>
    public IActionResult OnGetValidateEartag(string? country, string? herdmark, string? animal)
        => new JsonResult(new { error = EartagValidator.Validate(country, herdmark, animal) });

    public async Task<IActionResult> OnPostUpdateTestRowAsync()
    {
        if (!CanUserEditCase())
            return Forbid();

        var postedCase = Case;

        await LoadReadonlyPageAsync();
        await RestoreAndRestageCaseEditAsync(postedCase);
        await LoadOrInitializeDraftStateAsync();

        if (string.IsNullOrWhiteSpace(EditTestType))
            ModelState.AddModelError(nameof(EditTestType), "Select a test type.");

        if (string.IsNullOrWhiteSpace(EditTestResult))
            ModelState.AddModelError(nameof(EditTestResult), "Select a test result.");

        if (!ModelState.IsValid)
        {
            ReopenEditTestId = EditingTestId;
            return Page();
        }

        var test = StagedTests.FirstOrDefault(t => t.Id == EditingTestId);
        if (test is null)
            return RedirectToTestsAnchor();

        test.TestType = EditTestType;
        test.TestResult = EditTestResult;
        await SaveDraftStateAsync();
        return RedirectToTestsAnchor();
    }

    public Task<IActionResult> OnPostAsync() => SaveAsync("/Home");

    /// <summary>
    /// Legacy parity: btnCaseWork_Click (CaseEntryDEFRA.aspx.vb) runs the exact same
    /// UpdateSessionWithCaseDetails + CaseEntrySave.aspx save pipeline as the ordinary Save
    /// button, only redirecting to CaseWorkEntry.aspx instead of Home.aspx on full success —
    /// it is not a bare navigation link. Reuses the same SaveAsync path so mandatory-field and
    /// concurrency handling behave identically to Save.
    /// </summary>
    public Task<IActionResult> OnPostSaveAndGotoCaseworkAsync() => SaveAsync("/CaseWork/Entry");

    private async Task<IActionResult> SaveAsync(string successRedirectPage)
    {
        if (!CanUserEditCase())
            return Forbid();

        ApplyLegacyDefraPermissions();

        var persistedRecord = await caseService.GetCaseAsync(Rbse);
        if (persistedRecord is null)
        {
            Case.Rbse = Rbse;
            var batchTask = batchRepository.GetBatchNumbersByRbseAsync(Rbse);
            SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
            var caseWork = await caseWorkRepository.GetByRbseAsync(Rbse);
            HasCaseWorkLink = caseWork is not null
                              || await caseWorkRepository.GetEntryByRbseAsync(Rbse) is not null;
            await LoadLookupsAsync();
            await LoadOrInitializeDraftStateAsync();
            BatchNumbers = (await batchTask).ToList().AsReadOnly();
            TempData["Warning"] = $"Case '{Rbse}' is not saved yet. Complete Farm first.";
            return Page();
        }

        IsNonGbCase = persistedRecord.IsNonGbCase;

        // Legacy parity: VLA Data Entry's Save button is technically enabled (btnSave.Enabled =
        // True in VLADataEntryEnable) but every field is read-only, so Save is always a no-op for
        // this group. The fieldset being disabled means the posted Case fields are blank/default
        // (disabled inputs are never submitted) — redirecting here without touching anything
        // avoids treating that blank post as a real edit and overwriting the persisted record.
        if (!CanEditCaseFields)
            return RedirectToPage(successRedirectPage == "/CaseWork/Entry" ? successRedirectPage : "/Home",
                successRedirectPage == "/CaseWork/Entry" ? new { rbse = Rbse } : null);

        // Legacy CaseEntryDEFRA behavior: Form A date is read-only for non-GB cases.
        if (IsNonGbCase)
            Case.FormADate = persistedRecord.FormADate;

        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
        await LoadLookupsAsync();
        await LoadOrInitializeDraftStateAsync();

        // Test rows are confirmed individually and live in their own table, so they commit here
        // rather than after the Case row. A validation, mandatory-field or concurrency failure
        // below must not silently discard tests the user has already confirmed.
        await PersistStagedTestsAndResyncDraftAsync();

        ApplyLegacyPreSaveNormalizations();
        ValidateLegacyParityRules();

        if (!ModelState.IsValid)
            return Page();

        var rowStampBase64 = TempData[string.Format(RowStampKey, Rbse)]?.ToString();
        if (string.IsNullOrEmpty(rowStampBase64))
        {
            ConcurrencyError = "Session expired — please reload the page and try again.";
            return Page();
        }

        // Cross-tab staging (restores legacy's "one session, one commit" model): stage this
        // tab's edit into the shared draft, then commit *everything* staged for this RBSE
        // (this tab and/or Farm) together, rather than committing only this page's fields.
        await StageCaseScalarEditAsync(rowStampBase64);

        var userId = await currentUserService.GetUserIdAsync();
        var (failureRedirect, commitOutcome) = await CaseCommitHelper.CommitAllAsync(
            this, caseEditOrchestration, Rbse, userId,
            result => result switch
            {
                EditCaseResult.RbseNotFound     => $"Case '{Rbse}' not found.",
                EditCaseResult.AuditLogError    => "Audit log error during update.",
                EditCaseResult.PostUpdateError  => "Database error after update.",
                _                               => $"Update failed: {result}"
            });
        if (failureRedirect is not null)
            return failureRedirect;

        // Save casework fields if the case has a CaseWork row
        if (Case.HasCaseWork)
        {
            var cwCommand = new EditCaseWorkCommand(
                Rbse:                       Rbse,
                RbseDate:                   Case.RbseDate,
                Barcode:                    Case.Barcode,
                AhfReference:               Case.AhfReference,
                PurchaserBse1ReceivedDate:  Case.PurchaserBse1ReceivedDate,
                BreederBse1ReceivedDate:    Case.BreederBse1ReceivedDate,
                Vendor1Bse1ReceivedDate:    Case.Vendor1Bse1ReceivedDate,
                HomebredBse1ReceivedDate:   Case.HomebredBse1ReceivedDate,
                SummarySheetReceivedDate:   Case.SummarySheetReceivedDate,
                PaperworkCompleteDate:      Case.PaperworkCompleteDate);

            await caseWorkRepository.EditAsync(cwCommand);
        }

        await caseEditDraftState.ClearAsync(Rbse);

        if (CaseCommitHelper.TryStageWarnings(this, commitOutcome!, Rbse) is { } warningRedirect)
            return warningRedirect;

        // Legacy parity: CaseEntrySave.aspx auto-redirects to Home.aspx (or the ?redirect=
        // target, e.g. CaseWorkEntry.aspx, when arrived via the Casework link) on a fully
        // successful save, clearing the session case state — not back to the tab the user was on.
        return successRedirectPage == "/CaseWork/Entry"
            ? RedirectToPage(successRedirectPage, new { rbse = Rbse })
            : RedirectToPage(successRedirectPage);
    }

    /// <summary>
    /// Validates this tab's fields and, if valid, stages them into the shared cross-tab
    /// draft (without committing) before navigating to another tab — mirrors legacy's
    /// <c>UpdateSessionWithCaseDetails()</c> running on every tab-switch, so an invalid
    /// Form A/B/C/DOB chain blocks leaving this tab, not just blocks Save.
    /// </summary>
    public async Task<IActionResult> OnPostStageAndGotoAsync(string targetPage)
    {
        if (!CanUserEditCase())
            return Forbid();

        ApplyLegacyDefraPermissions();

        var persistedRecord = await caseService.GetCaseAsync(Rbse);
        if (persistedRecord is null)
            return RedirectToPage(targetPage, new { rbse = Rbse });

        IsNonGbCase = persistedRecord.IsNonGbCase;
        if (IsNonGbCase)
            Case.FormADate = persistedRecord.FormADate;

        // Same no-op guard as SaveAsync: VLA Data Entry's fieldset is fully disabled, so the
        // posted Case fields are blank — staging them into the shared cross-tab draft would risk
        // a later Save (on any tab) committing blanked-out DEFRA fields.
        if (!CanEditCaseFields)
            return RedirectToPage(targetPage, new { rbse = Rbse });

        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
        await LoadLookupsAsync();
        await LoadOrInitializeDraftStateAsync();

        ApplyLegacyPreSaveNormalizations();
        ValidateLegacyParityRules();

        if (!ModelState.IsValid)
            return Page();

        var rowStampBase64 = TempData[string.Format(RowStampKey, Rbse)]?.ToString();
        if (string.IsNullOrEmpty(rowStampBase64))
        {
            ConcurrencyError = "Session expired — please reload the page and try again.";
            return Page();
        }

        await StageCaseScalarEditAsync(rowStampBase64);

        return RedirectToPage(targetPage, new { rbse = Rbse });
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

    /// <summary>Undoes <c>LoadReadonlyPageAsync()</c>'s overwrite of <see cref="Case"/> with the
    /// last-persisted DB record, and re-stages the posted (in-progress, unsaved) scalar edit —
    /// otherwise a Tests grid operation (add/edit row) silently discards any not-yet-saved edit to
    /// the DEFRA tab's own fields.</summary>
    private async Task RestoreAndRestageCaseEditAsync(CaseEditViewModel? postedCase)
    {
        if (postedCase is null)
            return;

        Case = postedCase;

        var rowStampBase64 = TempData[string.Format(RowStampKey, Rbse)]?.ToString();
        if (!string.IsNullOrEmpty(rowStampBase64))
            await StageCaseScalarEditAsync(rowStampBase64);
    }

    private void ApplyLegacyPreSaveNormalizations()
    {
        if (!Case.BirthDate.HasValue)
        {
            Case.BirthDateSource = null;
            Case.IsBirthDateEst = false;
        }

        // Legacy behavior: when Form B is entered and Slaughter Date is empty,
        // Slaughter Date is set to Form B Date during save mapping.
        if (!Case.SlaughterDate.HasValue && Case.FormBDate.HasValue)
            Case.SlaughterDate = Case.FormBDate;

        // Legacy behavior (ctlXBSE1ReceivedDate_DateChanged): entering a received date auto-ticks
        // its "Is X Received?" checkbox. One-way only — never auto-unticks on its own.
        if (Case.PurchaserBse1ReceivedDate.HasValue) Case.IsPurchaserBse1Received = true;
        if (Case.BreederBse1ReceivedDate.HasValue) Case.IsBreederBse1Received = true;
        if (Case.Vendor1Bse1ReceivedDate.HasValue) Case.IsVendor1Bse1Received = true;
        if (Case.HomebredBse1ReceivedDate.HasValue) Case.IsHomebredBse1Received = true;
        if (Case.SummarySheetReceivedDate.HasValue) Case.IsSummarySheetReceived = true;
        if (Case.PaperworkCompleteDate.HasValue) Case.IsPaperworkComplete = true;
    }

    private void ValidateLegacyParityRules()
    {
        var today = DateTime.Today;

        ValidateEartag();
        ValidateFormADate(today);
        ValidateFormAResubmittedDate(today);
        ValidateFormBDate(today);
        ValidateFormCAndFate();
        ValidateBirthDate(today);
        ValidateCaseWorkDates(today);
    }

    private void ValidateEartag()
    {
        if (string.IsNullOrWhiteSpace(Case.EartagCountry)
            && string.IsNullOrWhiteSpace(Case.EartagHerdmark)
            && string.IsNullOrWhiteSpace(Case.Eartag))
        {
            ModelState.AddModelError("Case.EartagCountry", "Enter an eartag.");
            return;
        }

        // Mirrors BSELib.Eartag.GetEartag's country-specific format/checksum validation.
        var eartagError = EartagValidator.Validate(Case.EartagCountry, Case.EartagHerdmark, Case.Eartag);
        if (eartagError is not null)
            ModelState.AddModelError("Case.EartagCountry", eartagError);
    }

    private void ValidateFormADate(DateTime today)
    {
        if (!IsNonGbCase && !Case.FormADate.HasValue)
            ModelState.AddModelError("Case.FormADate", "Enter a Form A date.");

        if (Case.Bse1ReceivedDate.HasValue && Case.Bse1ReceivedDate.Value.Date > today)
            ModelState.AddModelError("Case.Bse1ReceivedDate", "You must enter a past date.");

        if (!Case.FormADate.HasValue)
            return;

        var latest = Case.SlaughterDate?.Date ?? today;
        var formA = Case.FormADate.Value.Date;
        if (formA <= latest)
            return;

        var message = Case.SlaughterDate.HasValue
            ? "You must enter a date before the Slaughter Date."
            : "You must enter a past date.";
        ModelState.AddModelError("Case.FormADate", message);
    }

    private void ValidateFormAResubmittedDate(DateTime today)
    {
        if (!Case.FormAResubmittedDate.HasValue)
            return;

        if (!Case.FormADate.HasValue)
        {
            ModelState.AddModelError("Case.FormAResubmittedDate", "You must enter a Form A Date first.");
            return;
        }

        var value = Case.FormAResubmittedDate.Value.Date;
        var min = Case.FormADate.Value.Date;
        if (value < min || value > today)
            ModelState.AddModelError("Case.FormAResubmittedDate", "You must enter a date in the past but after the Form A Date.");
    }

    private void ValidateFormBDate(DateTime today)
    {
        if (!Case.FormBDate.HasValue)
            return;

        if (!Case.FormADate.HasValue)
        {
            ModelState.AddModelError("Case.FormBDate", "You must enter a Form A Date first.");
            return;
        }

        var value = Case.FormBDate.Value.Date;
        var min = Case.FormADate.Value.Date;
        if (value < min || value > today)
            ModelState.AddModelError("Case.FormBDate", "You must enter a date in the past but after the Form A Date.");
    }

    private void ValidateFormCAndFate()
    {
        if (Case.FormCDate.HasValue && !Case.FormBDate.HasValue)
            ModelState.AddModelError("Case.FormCDate", "You must enter a Form B Date first.");

        if (Case.FormBDate.HasValue && string.IsNullOrWhiteSpace(Case.Fate))
            ModelState.AddModelError("Case.Fate", "Select a fate.");
    }

    private void ValidateBirthDate(DateTime today)
    {
        if (!Case.BirthDate.HasValue)
            return;

        var birthDate = Case.BirthDate.Value.Date;
        if (birthDate < DateTime.UnixEpoch)
            ModelState.AddModelError("Case.BirthDate", "Date of Birth must be on or after 01/01/1970.");

        var latestForFormA = Case.FormADate?.Date ?? today;
        if (birthDate > latestForFormA)
            ModelState.AddModelError("Case.BirthDate", "Date of Birth must be before the Form A Date");

        if (Case.PurchaseDate.HasValue && birthDate > Case.PurchaseDate.Value.Date)
            ModelState.AddModelError("Case.BirthDate", "Date of Birth must be before the Purchase Date");

        if (Case.OnsetDate.HasValue && birthDate > Case.OnsetDate.Value.Date)
            ModelState.AddModelError("Case.BirthDate", "Date of Birth must be before the Onset Date");
    }

    private void ValidateCaseWorkDates(DateTime today)
    {
        if (!Case.HasCaseWork || !Case.RbseDate.HasValue)
            return;

        var min = Case.RbseDate.Value.Date.AddDays(1);
        var max = today;
        var message = $"You must enter a date in the past but after the RBSE Date ({Case.RbseDate.Value:dd/MM/yyyy})";

        ValidateOptionalRange(Case.PurchaserBse1ReceivedDate, "Case.PurchaserBse1ReceivedDate", min, max, message);
        ValidateOptionalRange(Case.BreederBse1ReceivedDate, "Case.BreederBse1ReceivedDate", min, max, message);
        ValidateOptionalRange(Case.Vendor1Bse1ReceivedDate, "Case.Vendor1Bse1ReceivedDate", min, max, message);
        ValidateOptionalRange(Case.HomebredBse1ReceivedDate, "Case.HomebredBse1ReceivedDate", min, max, message);
        ValidateOptionalRange(Case.SummarySheetReceivedDate, "Case.SummarySheetReceivedDate", min, max, message);
        ValidateOptionalRange(Case.PaperworkCompleteDate, "Case.PaperworkCompleteDate", min, max, message);
    }

    private void ValidateOptionalRange(DateTime? value, string modelKey, DateTime min, DateTime max, string message)
    {
        if (!value.HasValue)
            return;

        var date = value.Value.Date;
        if (date < min || date > max)
            ModelState.AddModelError(modelKey, message);
    }

    private async Task LoadLookupsAsync()
    {
        var fateTask             = lookups.GetLookupAsync(LookupTableId.CaseFate);
        var surveyTask           = lookups.GetLookupAsync(LookupTableId.Survey);
        var reportedLocationTask = lookups.GetLookupAsync(LookupTableId.ReportedLocation);
        var birthDateSourceTask  = lookups.GetLookupAsync(LookupTableId.BirthDateSource);
        var valuationAgeTask     = lookups.GetLookupAsync(LookupTableId.ValuationAge);
        var caseTypeTask         = lookups.GetLookupAsync(LookupTableId.CaseType);
        var testTypeTask         = lookups.GetLookupAsync(LookupTableId.TestType);
        var testResultTask       = lookups.GetLookupAsync(LookupTableId.TestResult);

        await Task.WhenAll(fateTask, surveyTask, reportedLocationTask, birthDateSourceTask,
                           valuationAgeTask, caseTypeTask, testTypeTask, testResultTask);

        FateOptions             = await fateTask;
        SurveyOptions           = await surveyTask;
        ReportedLocationOptions = await reportedLocationTask;
        BirthDateSourceOptions  = await birthDateSourceTask;
        ValuationAgeOptions     = await valuationAgeTask;
        CaseTypeOptions         = await caseTypeTask;
        TestTypeOptions         = await testTypeTask;
        TestResultOptions       = await testResultTask;
    }

    private async Task LoadTestsAsync()
    {
        var all = StagedTests.Select(t => new CaseTestRecord(
            Id: t.Id ?? 0,
            Rbse: Rbse,
            TestType: t.TestType,
            TestTypeDescription: t.TestTypeDescription,
            TestResult: t.TestResult,
            TestResultDescription: t.TestResultDescription,
            RowStamp: string.IsNullOrWhiteSpace(t.RowStampBase64) ? [] : Convert.FromBase64String(t.RowStampBase64)))
            .ToList();

        TestsTotalCount = all.Count;
        TestsTotalPages = Math.Max(1, (int)Math.Ceiling(all.Count / (double)TestsPageSize));
        TPage = Math.Clamp(TPage, 1, TestsTotalPages);
        // No TSort means the order the user entered them in — only sort on an explicit column click.
        IEnumerable<CaseTestRecord> sorted = TSort switch
        {
            "result" => TDir == "desc" ? all.OrderByDescending(t => t.TestResultDescription) : all.OrderBy(t => t.TestResultDescription),
            "type"   => TDir == "desc" ? all.OrderByDescending(t => t.TestTypeDescription)   : all.OrderBy(t => t.TestTypeDescription),
            _        => all,
        };
        Tests = sorted.Skip((TPage - 1) * TestsPageSize).Take(TestsPageSize).ToList().AsReadOnly();
    }

    private async Task LoadReadonlyPageAsync()
    {
        ApplyLegacyDefraPermissions();

        var record = await caseService.GetCaseAsync(Rbse);
        if (record is null)
            return;

        Case = CaseEditViewModel.FromRecord(record);
        IsNonGbCase = record.IsNonGbCase;
        var caseWork = await caseWorkRepository.GetByRbseAsync(Rbse);
        if (caseWork is not null)
            Case.ApplyCaseWork(caseWork);

        ApplyLegacyDefraPermissions();

        HasCaseWorkLink = caseWork is not null
                          || await caseWorkRepository.GetEntryByRbseAsync(Rbse) is not null;

        var batchTask = batchRepository.GetBatchNumbersByRbseAsync(Rbse);
        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;

        await Task.WhenAll(LoadLookupsAsync(), batchTask, LoadTestsAsync());
        await ApplyStagedCaseOverlayAsync();
        BatchNumbers = (await batchTask).ToList().AsReadOnly();
    }

    private void ApplyLegacyDefraPermissions()
    {
        // Legacy EnableControls' 5-group model, collapsed to its actual distinct behaviours:
        // DEFRA Data Entry and DEFRA Maintenance are identical here (both MakeControlsWritable,
        // Barcode/AHF Reference always disabled), so only "DataEntry" needs checking, not a
        // separate DEFRAMaintenance role. VLA Data Entry is read-only; VLA Maintenance is
        // writable and is the only group ever able to edit Barcode/AHF Reference (gated further
        // below by CaseWork-row-exists AND not IsCaseClosed).
        //
        // IMPORTANT: GroupClaimsTransformation.GetPoliciesForGroup grants the "DataEntry" role
        // claim to ALL FOUR writable legacy groups, including "VLA Data Entry" — not just the two
        // DEFRA groups. So "DataEntry" alone cannot distinguish a DEFRA user from a VLA Data Entry
        // user; the "!VLAAccess" exclusion below is load-bearing, not redundant.
        var isVlaGroup = User.IsInRole(VlaAccessRole);
        var isVlaMaintenance = isVlaGroup && User.IsInRole(VlaMaintenanceRole);
        var isDefraDataEntry = User.IsInRole(DataEntryRole) && !isVlaGroup;

        CanEditDefraNotes = isDefraDataEntry;
        CanEditCaseFields = isDefraDataEntry || isVlaMaintenance;
        CanEditVlaMaintenanceCaseworkFields = isVlaMaintenance && Case.HasCaseWork && !Case.IsCaseClosed;
    }

    /// <summary>Legacy parity: every group except DEFRA Viewer holds the "DataEntry" role claim
    /// (see GetPoliciesForGroup), including VLA Data Entry — whose Save button is technically
    /// enabled in legacy too, but every field is read-only there so it is never a meaningful save.
    /// Reaching this handler is therefore harmless as long as <see cref="CanEditCaseFields"/> is
    /// also checked before applying any posted field values (see the no-op guard in SaveAsync).</summary>
    private bool CanUserEditCase() => User.IsInRole(DataEntryRole);

    private async Task<CaseEditDraftState> LoadOrInitializeDraftStateAsync()
    {
        var draft = await caseEditDraftState.GetAsync(Rbse);
        if (draft is null)
        {
            var persistedTests = (await testRepository.GetByRbseAsync(Rbse)).OrderBy(t => t.Id).ToList();
            draft = new CaseEditDraftState
            {
                Rbse = Rbse,
                Tests = persistedTests.Select(t => new CaseEditDraftTestItem
                {
                    Id = t.Id,
                    TestType = t.TestType,
                    TestTypeDescription = t.TestTypeDescription,
                    TestResult = t.TestResult,
                    TestResultDescription = t.TestResultDescription,
                    RowStampBase64 = t.RowStamp is null ? string.Empty : Convert.ToBase64String(t.RowStamp)
                }).ToList(),
                HasPendingChanges = false
            };

            await caseEditDraftState.SetAsync(draft);
        }

        StagedTests = draft.Tests.Select(t => new StagedTestItem
        {
            ClientKey = t.ClientKey,
            Id = t.Id,
            TestType = t.TestType,
            TestTypeDescription = t.TestTypeDescription,
            TestResult = t.TestResult,
            TestResultDescription = t.TestResultDescription,
            RowStampBase64 = t.RowStampBase64
        }).ToList();

        HasUnsavedChanges = draft.HasPendingChanges;
        await LoadTestsAsync();
        return draft;
    }

    private async Task SaveDraftStateAsync(bool hasPendingChanges = true)
    {
        var testTypeByCode = (await lookups.GetLookupAsync(LookupTableId.TestType)).ToDictionary(x => x.Code, x => x.Description, StringComparer.OrdinalIgnoreCase);
        var testResultByCode = (await lookups.GetLookupAsync(LookupTableId.TestResult)).ToDictionary(x => x.Code, x => x.Description, StringComparer.OrdinalIgnoreCase);

        var draft = new CaseEditDraftState
        {
            Rbse = Rbse,
            HasPendingChanges = hasPendingChanges,
            Tests = StagedTests.Select(t => new CaseEditDraftTestItem
            {
                ClientKey = t.ClientKey,
                Id = t.Id,
                TestType = t.TestType,
                TestTypeDescription = testTypeByCode.GetValueOrDefault(t.TestType),
                TestResult = t.TestResult,
                TestResultDescription = string.IsNullOrWhiteSpace(t.TestResult) ? null : testResultByCode.GetValueOrDefault(t.TestResult),
                RowStampBase64 = t.RowStampBase64
            }).ToList()
        };

        await caseEditDraftState.SetAsync(draft);
        HasUnsavedChanges = hasPendingChanges;
    }

    /// <summary>
    /// Commits the staged test rows, then replaces the draft's test list with what is now in the
    /// database so the temporary negative ids are replaced by real ones. Without the resync a
    /// second Save would re-insert the same rows. Other staged collections on the shared draft
    /// (the Case (APHA) tab's other-owner rows) are left untouched.
    /// </summary>
    private async Task PersistStagedTestsAndResyncDraftAsync()
    {
        await PersistStagedTestsAsync();

        var draft = await caseEditDraftState.GetAsync(Rbse);
        if (draft is null)
            return;

        draft.Tests = (await testRepository.GetByRbseAsync(Rbse)).OrderBy(t => t.Id).Select(t => new CaseEditDraftTestItem
        {
            Id = t.Id,
            TestType = t.TestType,
            TestTypeDescription = t.TestTypeDescription,
            TestResult = t.TestResult,
            TestResultDescription = t.TestResultDescription,
            RowStampBase64 = t.RowStamp is null ? string.Empty : Convert.ToBase64String(t.RowStamp)
        }).ToList();

        await caseEditDraftState.SetAsync(draft);

        StagedTests = draft.Tests.Select(t => new StagedTestItem
        {
            ClientKey = t.ClientKey,
            Id = t.Id,
            TestType = t.TestType,
            TestTypeDescription = t.TestTypeDescription,
            TestResult = t.TestResult,
            TestResultDescription = t.TestResultDescription,
            RowStampBase64 = t.RowStampBase64
        }).ToList();

        await LoadTestsAsync();
    }

    private async Task PersistStagedTestsAsync()
    {
        var persisted = (await testRepository.GetByRbseAsync(Rbse)).ToList();
        var persistedById = persisted.ToDictionary(t => t.Id);
        var stagedByExistingId = StagedTests.Where(t => t.Id is > 0).ToDictionary(t => t.Id!.Value);

        foreach (var removed in persisted.Where(p => !stagedByExistingId.ContainsKey(p.Id)))
        {
            if (removed.RowStamp is null)
                continue;

            await testRepository.DeleteAsync(removed.Id, removed.RowStamp);
        }

        foreach (var staged in StagedTests)
        {
            if (staged.Id is null || staged.Id <= 0)
            {
                await testRepository.AddAsync(new AddTestCommand(Rbse.Replace("/", ""), staged.TestType, staged.TestResult));
                continue;
            }

            if (!persistedById.TryGetValue(staged.Id.Value, out var current))
                continue;

            var changed = !string.Equals(current.TestType, staged.TestType, StringComparison.OrdinalIgnoreCase)
                          || !string.Equals(current.TestResult ?? string.Empty, staged.TestResult ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            if (!changed)
                continue;

            var rowStamp = string.IsNullOrWhiteSpace(staged.RowStampBase64)
                ? current.RowStamp ?? []
                : Convert.FromBase64String(staged.RowStampBase64);

            await testRepository.EditAsync(new EditTestCommand(staged.Id.Value, Rbse.Replace("/", ""), staged.TestType, staged.TestResult, rowStamp));
        }
    }

    public async Task<IActionResult> OnPostDeleteTestAsync(int id, string? rowStampBase64)
    {
        if (!CanUserEditCase())
            return Forbid();

        await LoadReadonlyPageAsync();
        await LoadOrInitializeDraftStateAsync();

        var item = StagedTests.FirstOrDefault(t => t.Id == id);
        if (item is not null)
        {
            StagedTests.Remove(item);
            await SaveDraftStateAsync();
        }

        return RedirectToTestsAnchor();
    }

    private RedirectToPageResult RedirectToTestsAnchor() =>
        RedirectToPage(pageName: null, pageHandler: null, routeValues: new { rbse = Rbse }, fragment: TestsAnchor);

    public string TestsSortUrl(string col)
    {
        var dir = string.Equals(TSort, col, StringComparison.OrdinalIgnoreCase) && TDir == "asc" ? "desc" : "asc";
        return $"?rbse={Uri.EscapeDataString(Rbse)}&TSort={col}&TDir={dir}&TPage=1#{TestsAnchor}";
    }

    public string TestsPageUrl(int page) =>
        $"?rbse={Uri.EscapeDataString(Rbse)}&TPage={page}&TSort={TSort}&TDir={TDir}#{TestsAnchor}";

    public sealed class StagedTestItem
    {
        public string ClientKey { get; set; } = Guid.NewGuid().ToString("N");
        public int? Id { get; set; }
        public string TestType { get; set; } = string.Empty;
        public string? TestTypeDescription { get; set; }
        public string? TestResult { get; set; }
        public string? TestResultDescription { get; set; }
        public string RowStampBase64 { get; set; } = string.Empty;
    }

    private int NextTemporaryTestId()
    {
        var minExistingId = StagedTests.Where(t => t.Id.HasValue).Select(t => t.Id!.Value).DefaultIfEmpty(0).Min();
        return minExistingId <= 0 ? minExistingId - 1 : -1;
    }
}
