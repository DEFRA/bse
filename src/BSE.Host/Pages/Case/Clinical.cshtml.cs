using BSE.Host.Helpers;
using BSE.Host.Services;
using BSE.Host.ModelBinding;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace BSE.Host.Pages.Case;

[Authorize]
public class ClinicalModel(
    IClinicalRepository clinicalRepository,
    ICaseRepository caseRepository,
    IBatchRepository batchRepository,
    ICaseWizardStateService wizardState,
    ICaseEditSessionStateService sessionState,
    ICaseDraftDiscardService caseDraftDiscard,
    ICaseEditOrchestrationService caseEditOrchestration,
    ICurrentUserService currentUser,
    IConfiguration configuration) : PageModel
{
    private const string DataEntryRole = "DataEntry";

    private const int PageSize = 10;
    private const string VlaAccessRole = "VLAAccess";
    private List<ClinicalVisitRecord> _persistedVisits = [];

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)] public int    VPage { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string VDir  { get; set; } = "asc";
    public int VisitsTotalPages { get; private set; } = 1;
    public int VisitsTotalCount { get; private set; }

    public ClinicalSignsViewModel Signs { get; private set; } = new();
    public DateTime? BirthDate { get; private set; }
    public string? ClinicalRowStampBase64 { get; private set; }
    public string SpolSiteUrl { get; private set; } = string.Empty;
    public IReadOnlyList<BatchNumberEntry> BatchNumbers { get; private set; } = [];

    /// <summary>Legacy IsVLAAllowedAdditionalCaseEdit: a VLA user can edit Clinical signs/visits
    /// freely unless a batch is currently selected AND this case is already linked to it — once
    /// batch-processed under the active batch, these fields lock for VLA roles.</summary>
    public bool IsVlaAllowedAdditionalCaseEdit { get; private set; } = true;

    // ── Inline "add/edit clinical visit" row state (GDS editable-grid pattern, matches /Case/Farm) ──
    [BindProperty] public List<StagedVisitItem> StagedVisits { get; set; } = [];
    [BindProperty, ModelBinder(BinderType = typeof(DatePickerModelBinder))] public DateTime? NewVisitDate { get; set; }
    [BindProperty, ModelBinder(BinderType = typeof(DatePickerModelBinder))] public DateTime? EditVisitDate { get; set; }
    [BindProperty] public string? EditingClientKey { get; set; }
    public bool ShowAddVisitRow { get; private set; }
    public string? ReopenEditClientKey { get; private set; }

    /// <summary>True when the draft has staged visit changes not yet committed by Save.</summary>
    public bool HasUnsavedChanges { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Legacy parity: CaseEntryClinical.aspx.vb's Page_Load redirects to SessionError.aspx when
        // Session(SV_RBSENumber) is missing (session timeout, direct URL access, stale back-button).
        if (string.IsNullOrWhiteSpace(Rbse))
            return RedirectToPage("/SessionError");

        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
        await LoadAsync();
        await LoadOrInitializeDraftStateAsync();
        await ApplyStagedSignsOverlayAsync();
        return Page();
    }

    /// <summary>Commits clinical signs and all staged visit changes to the database in one go.</summary>
    public async Task<IActionResult> OnPostSaveSignsAsync(string? clinicalRowStampBase64)
    {
        // Legacy parity: DEFRA Data Entry/Maintenance are always MakeControlsReadOnly on this tab
        // (VLA-only edit) — matches the same DataEntry+VLAAccess gate already used by every other
        // handler in this file (Add/Update/Delete/BeginEdit visit row). A plain DataEntry check
        // alone would let a DEFRA-only POST of blank/disabled clinical-sign fields overwrite them.
        if (!User.IsInRole(DataEntryRole) || !User.IsInRole(VlaAccessRole))
            return Forbid();

        var signs = BindSignsFromForm();

        await LoadAsync();
        await LoadOrInitializeDraftStateAsync();

        // Cross-tab staging (restores legacy's "one session, one commit" model): stage this
        // tab's fields and commit through CommitAllAsync regardless of whether a Clinical row
        // or even the Case itself already exists — its isNewCase/no-existing-Clinical-row
        // branches already Add vs Edit correctly, and Visits persist in the same transaction via
        // PersistStagedClinicalVisitsAsync. A missing-Clinical-row save used to bypass staging and
        // insert directly in its own transaction instead — safe only once the Case row already
        // existed, but for a genuinely new case this ran before Case/Farm/CaseWork were created
        // (CommitAllAsync is the only thing that creates them) and violated FK_CaseClinical_Case;
        // even when the case did already exist, it silently left any other tab's staged-but-
        // uncommitted edits behind while still redirecting Home as if the whole save succeeded.
        var rowStamp = string.IsNullOrEmpty(clinicalRowStampBase64) ? [] : Convert.FromBase64String(clinicalRowStampBase64);
        var edit = signs.ToEditCommand(Rbse, rowStamp);

        var draft = await sessionState.GetAsync(Rbse) ?? new CaseEditSessionState { Rbse = Rbse };
        // Always trust the freshly posted rowstamp over any earlier-pinned one — a stale pin
        // left over from an earlier stage this session causes a false "modified by another
        // user" error on the very next Save even with a single user involved.
        draft.ClinicalBaseRowStampBase64 = clinicalRowStampBase64 ?? string.Empty;
        draft.Clinical = edit with { RowStamp = string.IsNullOrEmpty(draft.ClinicalBaseRowStampBase64) ? [] : Convert.FromBase64String(draft.ClinicalBaseRowStampBase64) };
        draft.HasPendingChanges = true;
        await sessionState.SetAsync(draft);

        var userId = await currentUser.GetUserIdAsync();
        var (failureRedirect, commitOutcome) = await CaseCommitHelper.CommitAllAsync(
            this, caseEditOrchestration, Rbse, userId,
            result => $"Unable to save clinical signs: {result}.");
        if (failureRedirect is not null)
            return failureRedirect;

        // Visits persist inside CommitAllAsync itself (same transaction as the Clinical scalar
        // row), so no separate step is needed here regardless of warnings.
        if (CaseCommitHelper.TryStageWarnings(this, commitOutcome!, Rbse) is { } warningRedirect)
            return warningRedirect;

        // Legacy parity: CaseEntrySave.aspx auto-redirects to Home.aspx on a fully successful
        // save, clearing the session case state — not back to the tab the user was on.
        return RedirectToPage("/Home");
    }

    /// <summary>
    /// Validates this tab's fields and, if valid, stages them into the shared cross-tab
    /// draft (without committing) before navigating to another tab. Only applies once a
    /// Clinical row already exists — there is nothing to stage before the row is first created.
    /// </summary>
    public async Task<IActionResult> OnPostStageAndGotoAsync(string targetPage, string? clinicalRowStampBase64)
    {
        // Legacy parity: UpdateSessionWithCaseDetails() has no role check at all — every role,
        // including DEFRA Viewer (no "DataEntry" claim), can always navigate away from this tab.
        if (User.IsInRole(DataEntryRole))
            await StageSignsEditAsync(clinicalRowStampBase64);

        return RedirectToPage(targetPage, new { rbse = Rbse });
    }

    /// <summary>Visits sort-header link: a plain same-page query-string link, which — unlike
    /// Save/tab-switch — never staged the in-progress Signs edit, silently discarding it.</summary>
    public async Task<IActionResult> OnPostResortVisitsAsync(string? clinicalRowStampBase64)
    {
        if (User.IsInRole(DataEntryRole))
            await StageSignsEditAsync(clinicalRowStampBase64);

        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
        await LoadAsync();
        await LoadOrInitializeDraftStateAsync();
        await ApplyStagedSignsOverlayAsync();
        return Page();
    }

    /// <summary>Stages the posted Signs edit into the shared cross-tab draft, shared by every
    /// handler that navigates away from (or stays on, re-sorting) this tab.</summary>
    private async Task StageSignsEditAsync(string? clinicalRowStampBase64)
    {
        // Signs checkboxes are editable from the very start of brand-new-case creation (no
        // existing Clinical row required) — a blank rowstamp means "no DB row yet", not "nothing
        // to stage"; bailing out here silently discarded in-progress Signs edits on tab switch.
        var signs = BindSignsFromForm();
        var rowStamp = string.IsNullOrEmpty(clinicalRowStampBase64) ? [] : Convert.FromBase64String(clinicalRowStampBase64);
        var edit = signs.ToEditCommand(Rbse, rowStamp);

        var draft = await sessionState.GetAsync(Rbse) ?? new CaseEditSessionState { Rbse = Rbse };
        // Always trust the freshly posted rowstamp over any earlier-pinned one — see OnPostSaveSignsAsync.
        draft.ClinicalBaseRowStampBase64 = clinicalRowStampBase64 ?? string.Empty;
        var stagedEdit = edit with { RowStamp = Convert.FromBase64String(draft.ClinicalBaseRowStampBase64) };

        // Legacy parity: merely switching tabs without typing anything must not trip the
        // cross-tab unsaved-changes exit warning — only flag a genuine edit to the signs. For a
        // brand-new case there is no persisted Clinical row yet, but that must compare against an
        // all-unchecked baseline, not an automatic "different" (which would falsely flag every tab
        // visit, edited or not, as unsaved).
        var persistedClinical = await clinicalRepository.GetByRbseAsync(Rbse);
        var baseline = (persistedClinical is not null
            ? ClinicalSignsViewModel.FromRecord(persistedClinical)
            : new ClinicalSignsViewModel()).ToEditCommand(Rbse, Convert.FromBase64String(draft.ClinicalBaseRowStampBase64));

        draft.Clinical = stagedEdit;
        if (DraftChangeDetector.IsDifferentFromPersisted(stagedEdit, baseline))
            draft.HasPendingChanges = true;
        await sessionState.SetAsync(draft);
    }

    /// <summary>Manually bound from the posted form — avoids ambiguous binding with the Signs property.</summary>
    private ClinicalSignsViewModel BindSignsFromForm() => new()
    {
        Apprehension = Request.Form["Signs.Apprehension"] == "true",
        HypersensitiveTouch = Request.Form["Signs.HypersensitiveTouch"] == "true",
        HypersensitiveSound = Request.Form["Signs.HypersensitiveSound"] == "true",
        Maniacal = Request.Form["Signs.Maniacal"] == "true",
        PanicStricken = Request.Form["Signs.PanicStricken"] == "true",
        TemperamentChange = Request.Form["Signs.TemperamentChange"] == "true",
        AbnormalHeadCarriage = Request.Form["Signs.AbnormalHeadCarriage"] == "true",
        EarTwitching = Request.Form["Signs.EarTwitching"] == "true",
        EarsOddAngle = Request.Form["Signs.EarsOddAngle"] == "true",
        AbnormalBehaviour = Request.Form["Signs.AbnormalBehaviour"] == "true",
        HeadShyness = Request.Form["Signs.HeadShyness"] == "true",
        LickingFlank = Request.Form["Signs.LickingFlank"] == "true",
        LickingNose = Request.Form["Signs.LickingNose"] == "true",
        Kicking = Request.Form["Signs.Kicking"] == "true",
        ReluctantDoorways = Request.Form["Signs.ReluctantDoorways"] == "true",
        HeadPressing = Request.Form["Signs.HeadPressing"] == "true",
        HeadRubbing = Request.Form["Signs.HeadRubbing"] == "true",
        TeethGrinding = Request.Form["Signs.TeethGrinding"] == "true",
        Blindness = Request.Form["Signs.Blindness"] == "true",
        Circling = Request.Form["Signs.Circling"] == "true",
        HindAtaxia = Request.Form["Signs.HindAtaxia"] == "true",
        Falling = Request.Form["Signs.Falling"] == "true",
        Paresis = Request.Form["Signs.Paresis"] == "true",
        ForeAtaxia = Request.Form["Signs.ForeAtaxia"] == "true",
        Recumbent = Request.Form["Signs.Recumbent"] == "true",
        Tremor = Request.Form["Signs.Tremor"] == "true",
        KnucklingFetlock = Request.Form["Signs.KnucklingFetlock"] == "true",
        WeightLoss = Request.Form["Signs.WeightLoss"] == "true",
        ConditionLoss = Request.Form["Signs.ConditionLoss"] == "true",
        MilkYield = Request.Form["Signs.MilkYield"] == "true"
    };

    private async Task ApplyStagedSignsOverlayAsync()
    {
        var staged = await sessionState.GetAsync(Rbse);
        if (staged?.Clinical is not null)
            Signs.ApplyStagedCommand(staged.Clinical);
    }

    /// <summary>Re-stages the posted (in-progress, unsaved) clinical signs edit — otherwise a Visits
    /// grid operation (add/edit/delete) silently discards any not-yet-saved checkbox edit, since
    /// <c>LoadAsync()</c> always reloads <see cref="Signs"/> from the database and <see cref="Signs"/>
    /// is never itself model-bound from the post. Also applies when no Clinical row exists yet
    /// (blank <paramref name="clinicalRowStampBase64"/>) — a brand-new, not-yet-saved clinical
    /// entry must survive a grid operation too, same as legacy's shared session DataSet would.</summary>
    private async Task RestoreAndRestageSignsEditAsync(string? clinicalRowStampBase64)
    {
        var signs = BindSignsFromForm();
        Signs = signs;

        var draft = await sessionState.GetAsync(Rbse) ?? new CaseEditSessionState { Rbse = Rbse };

        if (string.IsNullOrEmpty(clinicalRowStampBase64))
        {
            draft.Clinical = signs.ToEditCommand(Rbse, []);
        }
        else
        {
            var rowStamp = Convert.FromBase64String(clinicalRowStampBase64);
            var edit = signs.ToEditCommand(Rbse, rowStamp);
            // Always trust the freshly posted rowstamp over any earlier-pinned one — see OnPostSaveSignsAsync.
            draft.ClinicalBaseRowStampBase64 = clinicalRowStampBase64;
            draft.Clinical = edit with { RowStamp = Convert.FromBase64String(draft.ClinicalBaseRowStampBase64) };
        }

        draft.HasPendingChanges = true;
        await sessionState.SetAsync(draft);
    }

    /// <summary>Discards all staged visit changes without persisting them.</summary>
    public async Task<IActionResult> OnPostCancelClinicalEditAsync()
    {
        await caseDraftDiscard.DiscardAllAsync(Rbse);
        return RedirectToPage("/Home");
    }

    public async Task<IActionResult> OnGetCancelClinicalEditAsync()
    {
        await caseDraftDiscard.DiscardAllAsync(Rbse);
        return RedirectToPage("/Home");
    }

    /// <summary>Adds a clinical visit to the draft only. Not persisted until Save.</summary>
    public async Task<IActionResult> OnPostAddVisitRowAsync(string? clinicalRowStampBase64)
    {
        if (!User.IsInRole(DataEntryRole) || !User.IsInRole(VlaAccessRole))
            return Forbid();

        var postedDate = NewVisitDate;

        await LoadAsync();
        await RestoreAndRestageSignsEditAsync(clinicalRowStampBase64);
        var draft = await LoadOrInitializeDraftStateAsync();

        ValidateVisitDate(postedDate, nameof(NewVisitDate));

        if (ModelState.IsValid && postedDate.HasValue
            && draft.Visits.Any(v => v.VisitDate?.Date == postedDate.Value.Date))
            ModelState.AddModelError(nameof(NewVisitDate), "A visit on this date already exists. The visit date must be unique.");

        if (!ModelState.IsValid)
        {
            NewVisitDate = postedDate;
            ShowAddVisitRow = true;
            return Page();
        }

        draft.Visits.Add(new CaseClinicalDraftVisitItem
        {
            ClientKey = Guid.NewGuid().ToString("N"),
            Id = null,
            VisitDate = postedDate,
            RowStampBase64 = string.Empty
        });
        draft.HasClinicalVisitsChanges = true;
        await sessionState.SetAsync(draft);

        return RedirectToPage(new { rbse = Rbse });
    }

    /// <summary>Opens the inline edit view for one staged clinical visit row (no changes saved yet).</summary>
    public async Task<IActionResult> OnPostBeginEditVisitRowAsync(string clientKey, string? clinicalRowStampBase64)
    {
        if (!User.IsInRole(DataEntryRole) || !User.IsInRole(VlaAccessRole))
            return Forbid();

        await LoadAsync();
        await RestoreAndRestageSignsEditAsync(clinicalRowStampBase64);
        var draft = await LoadOrInitializeDraftStateAsync();

        var visit = draft.Visits.FirstOrDefault(v => v.ClientKey == clientKey);
        if (visit is not null)
        {
            ReopenEditClientKey = clientKey;
            EditingClientKey = clientKey;
            EditVisitDate = visit.VisitDate;
        }

        return Page();
    }

    /// <summary>Updates a staged clinical visit row in the draft only. Not persisted until Save.</summary>
    public async Task<IActionResult> OnPostUpdateVisitRowAsync(string? clinicalRowStampBase64)
    {
        if (!User.IsInRole(DataEntryRole) || !User.IsInRole(VlaAccessRole))
            return Forbid();

        var clientKey = EditingClientKey;
        var postedDate = EditVisitDate;

        await LoadAsync();
        await RestoreAndRestageSignsEditAsync(clinicalRowStampBase64);
        var draft = await LoadOrInitializeDraftStateAsync();

        var item = draft.Visits.FirstOrDefault(v => v.ClientKey == clientKey);
        if (item is null)
        {
            TempData["ErrorMessage"] = "The clinical visit row being edited no longer exists.";
            return RedirectToPage(new { rbse = Rbse });
        }

        ValidateVisitDate(postedDate, nameof(EditVisitDate));

        if (ModelState.IsValid && postedDate.HasValue
            && draft.Visits.Any(v => v.ClientKey != clientKey && v.VisitDate?.Date == postedDate.Value.Date))
            ModelState.AddModelError(nameof(EditVisitDate), "A visit on this date already exists. The visit date must be unique.");

        if (!ModelState.IsValid)
        {
            EditingClientKey = clientKey;
            EditVisitDate = postedDate;
            ReopenEditClientKey = clientKey;
            return Page();
        }

        item.VisitDate = postedDate;
        draft.HasClinicalVisitsChanges = true;
        await sessionState.SetAsync(draft);

        return RedirectToPage(new { rbse = Rbse });
    }

    /// <summary>Removes a staged clinical visit row from the draft only. Not persisted until Save.</summary>
    public async Task<IActionResult> OnPostDeleteVisitAsync(string clientKey, string? clinicalRowStampBase64)
    {
        if (!User.IsInRole(DataEntryRole) || !User.IsInRole(VlaAccessRole))
            return Forbid();

        await LoadAsync();
        await RestoreAndRestageSignsEditAsync(clinicalRowStampBase64);
        var draft = await LoadOrInitializeDraftStateAsync();

        var item = draft.Visits.FirstOrDefault(v => v.ClientKey == clientKey);
        if (item is not null)
        {
            draft.Visits.Remove(item);
            draft.HasClinicalVisitsChanges = true;
            await sessionState.SetAsync(draft);
        }

        return RedirectToPage(new { rbse = Rbse });
    }

    // Mirrors legacy: visit date must be after birth date (default 1 Jan 1970) and not in the future
    private void ValidateVisitDate(DateTime? date, string key)
    {
        if (!date.HasValue)
        {
            // The DatePickerModelBinder already added "Please enter a valid date" when the posted
            // text failed to parse — don't stack a second, contradictory "blank" message on it.
            if (ModelState[key] is not { Errors.Count: > 0 })
                ModelState.AddModelError(key, "Enter a visit date.");
            return;
        }

        // Legacy's CalendarDate.Validate(ByRef, earliest, latest) treats the earliest bound as
        // inclusive (valid when dDate >= dEarliestDate) — a visit on the birth date itself is valid.
        var minDate = BirthDate?.Date ?? DateTime.UnixEpoch;
        if (date.Value.Date < minDate)
            ModelState.AddModelError(key, BirthDate.HasValue
                ? "The visit date must be on or after the birth date."
                : "The visit date must be on or after 1 January 1970.");

        if (date.Value.Date > DateTime.Today)
            ModelState.AddModelError(key, "The visit date must not be in the future.");
    }

    private async Task LoadAsync()
    {
        var clinicalTask = clinicalRepository.GetByRbseAsync(Rbse);
        var visitsTask   = clinicalRepository.GetVisitsByRbseAsync(Rbse);
        var batchTask    = batchRepository.GetBatchNumbersByRbseAsync(Rbse);
        var caseTask     = caseRepository.GetCaseByRbseAsync(Rbse);

        await Task.WhenAll(clinicalTask, visitsTask, batchTask, caseTask);

        var clinical = await clinicalTask;
        if (clinical is not null)
        {
            Signs = ClinicalSignsViewModel.FromRecord(clinical);
            ClinicalRowStampBase64 = clinical.RowStamp is not null
                ? Convert.ToBase64String(clinical.RowStamp)
                : null;
        }

        _persistedVisits = (await visitsTask).ToList();
        var caseRecord = await caseTask;
        // Legacy parity: the shared session dataset means an unsaved Date of Birth typed on the
        // DEFRA tab is instantly visible to this tab's visit-date validation bound — not just the
        // last-persisted value.
        var stagedCase = (await sessionState.GetAsync(Rbse))?.Case;
        BirthDate        = stagedCase is not null ? stagedCase.BirthDate : caseRecord?.BirthDate;
        BatchNumbers     = (await batchTask).ToList().AsReadOnly();

        var pendingBatch = await wizardState.GetAsync();
        IsVlaAllowedAdditionalCaseEdit = pendingBatch is null
            || !BatchNumbers.Any(b => b.BatchId == pendingBatch.BatchId);
    }

    private async Task<CaseEditSessionState> LoadOrInitializeDraftStateAsync()
    {
        var draft = await sessionState.GetAsync(Rbse) ?? new CaseEditSessionState { Rbse = Rbse };
        if (draft.Visits.Count == 0 && !draft.HasClinicalVisitsChanges)
        {
            draft.Visits = _persistedVisits.Select(v => new CaseClinicalDraftVisitItem
            {
                ClientKey = Guid.NewGuid().ToString("N"),
                Id = v.Id,
                VisitDate = v.VisitDate,
                RowStampBase64 = v.RowStamp is null ? string.Empty : Convert.ToBase64String(v.RowStamp)
            }).ToList();
            await sessionState.SetAsync(draft);
        }

        StagedVisits = draft.Visits.Select(v => new StagedVisitItem
        {
            ClientKey = v.ClientKey,
            Id = v.Id,
            VisitDate = v.VisitDate,
            RowStampBase64 = v.RowStampBase64
        }).ToList();

        HasUnsavedChanges = draft.HasClinicalVisitsChanges;

        IEnumerable<StagedVisitItem> sorted = VDir == "desc"
            ? StagedVisits.OrderByDescending(v => v.VisitDate)
            : StagedVisits.OrderBy(v => v.VisitDate);

        VisitsTotalCount = StagedVisits.Count;
        VisitsTotalPages = Math.Max(1, (int)Math.Ceiling(StagedVisits.Count / (double)PageSize));
        VPage = Math.Clamp(VPage, 1, VisitsTotalPages);
        Visits = sorted.Skip((VPage - 1) * PageSize).Take(PageSize).ToList();

        return draft;
    }

    public string VisitsSortUrl() =>
        $"?rbse={Uri.EscapeDataString(Rbse)}&VDir={(VDir == "asc" ? "desc" : "asc")}&VPage=1";

    public IReadOnlyList<StagedVisitItem> Visits { get; private set; } = [];

    public sealed class StagedVisitItem
    {
        public string ClientKey { get; set; } = string.Empty;
        public int? Id { get; set; }
        public DateTime? VisitDate { get; set; }
        public string RowStampBase64 { get; set; } = string.Empty;

        /// <summary>True for rows staged locally that do not yet exist in the database.</summary>
        public bool IsUnsaved => Id is null or <= 0;
    }

    // ── View model ────────────────────────────────────────────────────────────

    public class ClinicalSignsViewModel
    {
        public bool Apprehension { get; set; }
        public bool HypersensitiveTouch { get; set; }
        public bool HypersensitiveSound { get; set; }
        public bool Maniacal { get; set; }
        public bool PanicStricken { get; set; }
        public bool TemperamentChange { get; set; }
        public bool AbnormalHeadCarriage { get; set; }
        public bool EarTwitching { get; set; }
        public bool EarsOddAngle { get; set; }
        public bool AbnormalBehaviour { get; set; }
        public bool HeadShyness { get; set; }
        public bool LickingFlank { get; set; }
        public bool LickingNose { get; set; }
        public bool Kicking { get; set; }
        public bool ReluctantDoorways { get; set; }
        public bool HeadPressing { get; set; }
        public bool HeadRubbing { get; set; }
        public bool TeethGrinding { get; set; }
        public bool Blindness { get; set; }
        public bool Circling { get; set; }
        public bool HindAtaxia { get; set; }
        public bool Falling { get; set; }
        public bool Paresis { get; set; }
        public bool ForeAtaxia { get; set; }
        public bool Recumbent { get; set; }
        public bool Tremor { get; set; }
        public bool KnucklingFetlock { get; set; }
        public bool WeightLoss { get; set; }
        public bool ConditionLoss { get; set; }
        public bool MilkYield { get; set; }

        public static ClinicalSignsViewModel FromRecord(CaseClinicalRecord r) => new()
        {
            Apprehension = r.Apprehension, HypersensitiveTouch = r.HypersensitiveTouch,
            HypersensitiveSound = r.HypersensitiveSound, Maniacal = r.Maniacal,
            PanicStricken = r.PanicStricken, TemperamentChange = r.TemperamentChange,
            AbnormalHeadCarriage = r.AbnormalHeadCarriage, EarTwitching = r.EarTwitching,
            EarsOddAngle = r.EarsOddAngle, AbnormalBehaviour = r.AbnormalBehaviour,
            HeadShyness = r.HeadShyness, LickingFlank = r.LickingFlank, LickingNose = r.LickingNose,
            Kicking = r.Kicking, ReluctantDoorways = r.ReluctantDoorways, HeadPressing = r.HeadPressing,
            HeadRubbing = r.HeadRubbing, TeethGrinding = r.TeethGrinding, Blindness = r.Blindness,
            Circling = r.Circling, HindAtaxia = r.HindAtaxia, Falling = r.Falling, Paresis = r.Paresis,
            ForeAtaxia = r.ForeAtaxia, Recumbent = r.Recumbent, Tremor = r.Tremor,
            KnucklingFetlock = r.KnucklingFetlock, WeightLoss = r.WeightLoss,
            ConditionLoss = r.ConditionLoss, MilkYield = r.MilkYield
        };

        public AddCaseClinicalCommand ToAddCommand(string rbse) => new(
            rbse, Apprehension, HypersensitiveTouch, HypersensitiveSound, Maniacal, PanicStricken,
            TemperamentChange, AbnormalHeadCarriage, EarTwitching, EarsOddAngle, AbnormalBehaviour,
            HeadShyness, LickingFlank, LickingNose, Kicking, ReluctantDoorways, HeadPressing,
            HeadRubbing, TeethGrinding, Blindness, Circling, HindAtaxia, Falling, Paresis,
            ForeAtaxia, Recumbent, Tremor, KnucklingFetlock, WeightLoss, ConditionLoss, MilkYield);

        public EditCaseClinicalCommand ToEditCommand(string rbse, byte[] rowStamp) => new(
            rbse, Apprehension, HypersensitiveTouch, HypersensitiveSound, Maniacal, PanicStricken,
            TemperamentChange, AbnormalHeadCarriage, EarTwitching, EarsOddAngle, AbnormalBehaviour,
            HeadShyness, LickingFlank, LickingNose, Kicking, ReluctantDoorways, HeadPressing,
            HeadRubbing, TeethGrinding, Blindness, Circling, HindAtaxia, Falling, Paresis,
            ForeAtaxia, Recumbent, Tremor, KnucklingFetlock, WeightLoss, ConditionLoss, MilkYield,
            rowStamp);

        /// <summary>Overlays a staged-but-not-yet-committed signs edit (from another tab's
        /// cross-tab draft) so revisiting this tab shows the pending edit instead of the
        /// last-committed DB values.</summary>
        public void ApplyStagedCommand(EditCaseClinicalCommand c)
        {
            Apprehension = c.Apprehension; HypersensitiveTouch = c.HypersensitiveTouch;
            HypersensitiveSound = c.HypersensitiveSound; Maniacal = c.Maniacal;
            PanicStricken = c.PanicStricken; TemperamentChange = c.TemperamentChange;
            AbnormalHeadCarriage = c.AbnormalHeadCarriage; EarTwitching = c.EarTwitching;
            EarsOddAngle = c.EarsOddAngle; AbnormalBehaviour = c.AbnormalBehaviour;
            HeadShyness = c.HeadShyness; LickingFlank = c.LickingFlank; LickingNose = c.LickingNose;
            Kicking = c.Kicking; ReluctantDoorways = c.ReluctantDoorways; HeadPressing = c.HeadPressing;
            HeadRubbing = c.HeadRubbing; TeethGrinding = c.TeethGrinding; Blindness = c.Blindness;
            Circling = c.Circling; HindAtaxia = c.HindAtaxia; Falling = c.Falling; Paresis = c.Paresis;
            ForeAtaxia = c.ForeAtaxia; Recumbent = c.Recumbent; Tremor = c.Tremor;
            KnucklingFetlock = c.KnucklingFetlock; WeightLoss = c.WeightLoss;
            ConditionLoss = c.ConditionLoss; MilkYield = c.MilkYield;
        }
    }
}
