using BSE.Host.Helpers;
using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace BSE.Host.Pages.Case;

[Authorize]
public class BabModel(
    IBabRepository babRepository,
    ICaseRepository caseRepository,
    ILookupDataService lookups,
    IBatchRepository batchRepository,
    ICaseEditSessionStateService sessionState,
    ICaseDraftDiscardService caseDraftDiscard,
    ICaseEditOrchestrationService caseEditOrchestration,
    ICurrentUserService currentUser,
    IConfiguration configuration) : PageModel
{
    private static readonly DateTime BabBirthDateThreshold = new(1988, 7, 18, 0, 0, 0, DateTimeKind.Unspecified);

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    public string? RowStampBase64 { get; private set; }
    public string SpolSiteUrl { get; private set; } = string.Empty;
    public IReadOnlyList<BatchNumberEntry> BatchNumbers { get; private set; } = [];
    public bool CanEditBabControls { get; private set; }
    public bool HasPurchaseData { get; private set; }

    public IEnumerable<LookupItem> AnimalOrigins { get; private set; } = [];
    public IEnumerable<LookupItem> FeedRisks { get; private set; } = [];
    public IEnumerable<LookupItem> HorizontalRisks { get; private set; } = [];
    public IEnumerable<LookupItem> MaternalRisks { get; private set; } = [];

    [BindProperty]
    public string? Origin { get; set; }

    [BindProperty]
    public BabFormViewModel Bab { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        // Legacy parity: CaseEntryBAB.aspx.vb's Page_Load redirects to SessionError.aspx when
        // Session(SV_RBSENumber) is missing (session timeout, direct URL access, stale back-button).
        if (string.IsNullOrWhiteSpace(Rbse))
            return RedirectToPage("/SessionError");

        SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;

        await LoadAsync();
        await ApplyStagedBabOverlayAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveBabAsync(string? rowStampBase64)
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        var currentBabTask = babRepository.GetByRbseAsync(Rbse);
        var currentCaseTask = caseRepository.GetCaseByRbseAsync(Rbse);
        await Task.WhenAll(currentBabTask, currentCaseTask);

        var currentBab = await currentBabTask;
        var currentCase = await currentCaseTask;
        var (currentBirthDate, _, _) = await ResolveEffectiveCaseScalarsAsync(currentCase);

        // Legacy parity: BAB only ever becomes editable once Date of Birth is set (or a BAB row
        // already exists) — EvaluateLegacyBabEditPermission already covers "case doesn't exist yet"
        // the same way it covers "no Date of Birth yet", so no separate block is needed here.
        // btnSave_Click still runs unconditionally in legacy even when this tab's own fields are
        // read-only (e.g. VLA Data Entry) — the disabled fieldset means the browser never posts
        // this tab's field values anyway, so only this tab's own staging is skipped here; whatever
        // other tabs have staged is still committed.
        var canEdit = EvaluateLegacyBabEditPermission(currentBirthDate, currentBab);
        if (!canEdit)
            return await CommitStagedChangesOnlyAsync();

        var normalisedNatalCphh = CphhNormalizer.Normalize(Bab.NatalCphh);
        if (!string.IsNullOrWhiteSpace(normalisedNatalCphh) && normalisedNatalCphh.Length != 11)
        {
            ModelState.AddModelError("Bab.NatalCphh", "Enter CPHH as 11 digits in the format NN/NNN/NNNN/NN.");
            SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
            await LoadAsync();
            Bab.NatalCphh = normalisedNatalCphh;
            return Page();
        }

        Bab.NatalCphh = string.IsNullOrWhiteSpace(normalisedNatalCphh) ? null : normalisedNatalCphh;

        if (Origin != "P")
        {
            Bab.NatalCphh = Bab.TracedName = Bab.TracedAddress1 =
                Bab.TracedAddress2 = Bab.TracedAddress3 = Bab.TracedPostcode = null;

            // Legacy EmptyPurchaseFields: changing Origin away from Purchased on this tab also
            // clears the VLA tab's Purchase Date/County/Age fields, even though they aren't
            // rendered here — both pages clear the same shared session Case row.
            await ClearPurchaseFieldsIfPresentAsync();
        }

        // Cross-tab staging (restores legacy's "one session, one commit" model): stage this
        // tab's fields and commit through CommitAllAsync regardless of whether a BAB row already
        // exists — its isNewCase/no-existing-Bab-row branches already Add vs Edit correctly. A
        // brand-new BAB row (no rowStampBase64, i.e. the very first Save for a new case) used to
        // bypass staging entirely and insert BAB in its own standalone transaction — which, for a
        // genuinely new case, ran before Case/Farm/CaseWork had been created (CommitAllAsync is
        // the only thing that creates them), either throwing an FK violation or, when some other
        // tab's Save had already created the case, silently committing only this tab's row while
        // leaving whatever Farm/Case fields were still staged (never committed) uncommitted.
        var rowStamp = string.IsNullOrEmpty(rowStampBase64) ? [] : Convert.FromBase64String(rowStampBase64);
        var edit = new EditCaseBabCommand(
            Rbse, Bab.NatalCphh, Bab.Notes,
            Bab.TracedName, Bab.TracedAddress1, Bab.TracedAddress2, Bab.TracedAddress3,
            Bab.TracedPostcode, Bab.FeedRisk, Bab.HorizontalRisk, Bab.MaternalRisk,
            rowStamp);

        var draft = await sessionState.GetAsync(Rbse) ?? new CaseEditSessionState { Rbse = Rbse };
        // Always trust the freshly posted rowstamp over any earlier-pinned one — a stale pin
        // left over from an earlier stage this session causes a false "modified by another
        // user" error on the very next Save even with a single user involved.
        draft.BabBaseRowStampBase64 = rowStampBase64 ?? string.Empty;
        draft.Bab = edit with { RowStamp = string.IsNullOrEmpty(draft.BabBaseRowStampBase64) ? [] : Convert.FromBase64String(draft.BabBaseRowStampBase64) };
        draft.BabOrigin = Origin;
        StageCaseOrigin(draft, currentCase);
        draft.HasPendingChanges = true;
        await sessionState.SetAsync(draft);

        var userId = await currentUser.GetUserIdAsync();
        var (failureRedirect, commitOutcome) = await CaseCommitHelper.CommitAllAsync(
            this, caseEditOrchestration, Rbse, userId,
            result => $"Unable to save BAB changes: {result}.");
        if (failureRedirect is not null)
            return failureRedirect;

        if (CaseCommitHelper.TryStageWarnings(this, commitOutcome!, Rbse) is { } warningRedirect)
            return warningRedirect;

        // Legacy parity: CaseEntrySave.aspx auto-redirects to Home.aspx on a fully successful
        // save, clearing the session case state — not back to the tab the user was on.
        return RedirectToPage("/Home");
    }

    /// <summary>Commits whatever is already staged for this RBSE across every tab, without
    /// touching BAB's own fields — used when this tab's own fields are read-only (see
    /// <see cref="OnPostSaveBabAsync"/>).</summary>
    private async Task<IActionResult> CommitStagedChangesOnlyAsync()
    {
        var userId = await currentUser.GetUserIdAsync();
        var (failureRedirect, commitOutcome) = await CaseCommitHelper.CommitAllAsync(
            this, caseEditOrchestration, Rbse, userId,
            result => $"Unable to save BAB changes: {result}.");
        if (failureRedirect is not null)
            return failureRedirect;

        if (CaseCommitHelper.TryStageWarnings(this, commitOutcome!, Rbse) is { } warningRedirect)
            return warningRedirect;

        return RedirectToPage("/Home");
    }

    /// <summary>
    /// Validates this tab's fields and, if valid, stages them into the shared cross-tab
    /// draft (without committing) before navigating to another tab. Applies even before a BAB
    /// row (or the Case itself) exists yet — EvaluateLegacyBabEditPermission already covers
    /// "case doesn't exist yet" the same way OnPostSaveBabAsync does, so an edit made on this
    /// tab during brand-new-case creation is staged instead of silently discarded on tab switch.
    /// </summary>
    public async Task<IActionResult> OnPostStageAndGotoAsync(string targetPage, string? rowStampBase64)
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        var currentBabTask = babRepository.GetByRbseAsync(Rbse);
        var currentCaseTask = caseRepository.GetCaseByRbseAsync(Rbse);
        await Task.WhenAll(currentBabTask, currentCaseTask);

        var currentBab = await currentBabTask;
        var currentCase = await currentCaseTask;
        var (currentBirthDate, _, _) = await ResolveEffectiveCaseScalarsAsync(currentCase);

        if (!EvaluateLegacyBabEditPermission(currentBirthDate, currentBab))
            return RedirectToPage(targetPage, new { rbse = Rbse });

        var normalisedNatalCphh = CphhNormalizer.Normalize(Bab.NatalCphh);
        if (!string.IsNullOrWhiteSpace(normalisedNatalCphh) && normalisedNatalCphh.Length != 11)
        {
            ModelState.AddModelError("Bab.NatalCphh", "Enter CPHH as 11 digits in the format NN/NNN/NNNN/NN.");
            SpolSiteUrl = configuration["SpolSiteUrl"] ?? string.Empty;
            await LoadAsync();
            Bab.NatalCphh = normalisedNatalCphh;
            return Page();
        }

        Bab.NatalCphh = string.IsNullOrWhiteSpace(normalisedNatalCphh) ? null : normalisedNatalCphh;

        if (Origin != "P")
        {
            Bab.NatalCphh = Bab.TracedName = Bab.TracedAddress1 =
                Bab.TracedAddress2 = Bab.TracedAddress3 = Bab.TracedPostcode = null;

            // Legacy EmptyPurchaseFields: changing Origin away from Purchased on this tab also
            // clears the VLA tab's Purchase Date/County/Age fields, even though they aren't
            // rendered here — both pages clear the same shared session Case row.
            await ClearPurchaseFieldsIfPresentAsync();
        }

        var rowStamp = string.IsNullOrEmpty(rowStampBase64) ? [] : Convert.FromBase64String(rowStampBase64);
        var edit = new EditCaseBabCommand(
            Rbse, Bab.NatalCphh, Bab.Notes,
            Bab.TracedName, Bab.TracedAddress1, Bab.TracedAddress2, Bab.TracedAddress3,
            Bab.TracedPostcode, Bab.FeedRisk, Bab.HorizontalRisk, Bab.MaternalRisk,
            rowStamp);

        var draft = await sessionState.GetAsync(Rbse) ?? new CaseEditSessionState { Rbse = Rbse };
        // Always trust the freshly posted rowstamp over any earlier-pinned one — see the Save
        // handler above for why a stale pin causes a false concurrency conflict.
        draft.BabBaseRowStampBase64 = rowStampBase64 ?? string.Empty;
        var stagedEdit = edit with { RowStamp = Convert.FromBase64String(draft.BabBaseRowStampBase64) };

        // Legacy parity: merely switching tabs without typing anything must not trip the
        // cross-tab unsaved-changes exit warning — only flag a genuine edit to BAB fields or Origin.
        // For a brand-new case there is no persisted BAB row to compare against, but "nothing to
        // compare against" must mean a blank baseline, not an automatic "different" (which would
        // falsely flag every tab visit, edited or not, as unsaved).
        var baseline = currentBab is not null
            ? new EditCaseBabCommand(
                Rbse, currentBab.NatalCphh, currentBab.Notes, currentBab.TracedName,
                currentBab.TracedAddress1, currentBab.TracedAddress2, currentBab.TracedAddress3,
                currentBab.TracedPostcode, currentBab.FeedRisk, currentBab.HorizontalRisk,
                currentBab.MaternalRisk, Convert.FromBase64String(draft.BabBaseRowStampBase64))
            : new EditCaseBabCommand(
                Rbse, null, null, null, null, null, null, null, null, null, null,
                Convert.FromBase64String(draft.BabBaseRowStampBase64));
        var originChanged = !string.Equals(Origin, currentCase?.Origin, StringComparison.Ordinal);

        draft.Bab = stagedEdit;
        draft.BabOrigin = Origin;
        StageCaseOrigin(draft, currentCase);
        if (originChanged || DraftChangeDetector.IsDifferentFromPersisted(stagedEdit, baseline))
            draft.HasPendingChanges = true;
        await sessionState.SetAsync(draft);

        return RedirectToPage(targetPage, new { rbse = Rbse });
    }

    private async Task ApplyStagedBabOverlayAsync()
    {
        var staged = await sessionState.GetAsync(Rbse);
        if (staged?.Bab is not null)
            Bab.ApplyStagedCommand(staged.Bab);
    }

    /// <summary>Legacy keeps Origin on the single shared Case row (both ddlOrigin_SelectedIndexChanged
    /// handlers write to the same session CASE_TABLE row) — selecting a new Origin here must be
    /// visible on Case (APHA)/Case (DEFRA) immediately on tab switch, not just after Save. BabOrigin
    /// alone (only read at commit time by the BAB repository calls) never reached the shared Case
    /// draft those other tabs' overlay logic reads, so the cascade was invisible until a full
    /// Save-and-reload.</summary>
    private void StageCaseOrigin(CaseEditSessionState draft, CaseRecord? caseRecord)
    {
        var baseCommand = draft.Case;
        if (baseCommand is null)
        {
            draft.CaseBaseRowStampBase64 ??= Convert.ToBase64String(caseRecord?.RowStamp ?? []);
            var baseRowStamp = Convert.FromBase64String(draft.CaseBaseRowStampBase64);
            baseCommand = caseRecord is not null
                ? BSE.Host.Models.ViewModels.CaseEditViewModel.FromRecord(caseRecord).ToEditCommand(baseRowStamp)
                : new BSE.Host.Models.ViewModels.CaseEditViewModel { Rbse = Rbse }.ToEditCommand(baseRowStamp);
        }

        if (!string.Equals(baseCommand.Origin, Origin, StringComparison.Ordinal))
        {
            draft.Case = baseCommand with { Origin = Origin };
            draft.HasPendingChanges = true;
        }
    }

    /// <summary>Legacy EmptyPurchaseFields (CaseEntryBAB.aspx.vb): clears Purchase Date/County/Age
    /// in the shared Case row, merging with whatever another tab may already have staged for it
    /// rather than overwriting it. No-ops when there is nothing to clear, same as legacy's own
    /// row-exists guard.</summary>
    private async Task ClearPurchaseFieldsIfPresentAsync()
    {
        var draft = await sessionState.GetAsync(Rbse) ?? new CaseEditSessionState { Rbse = Rbse };

        var baseCommand = draft.Case;
        if (baseCommand is null)
        {
            var caseRecord = await caseRepository.GetCaseByRbseAsync(Rbse);
            if (caseRecord is null)
                return;
            if (caseRecord.PurchaseDate is null && string.IsNullOrWhiteSpace(caseRecord.PurchasedCounty) && caseRecord.PurchaseAgeInMonths is null)
                return;

            draft.CaseBaseRowStampBase64 ??= Convert.ToBase64String(caseRecord.RowStamp ?? []);
            baseCommand = BSE.Host.Models.ViewModels.CaseEditViewModel.FromRecord(caseRecord)
                .ToEditCommand(Convert.FromBase64String(draft.CaseBaseRowStampBase64));
        }
        else if (baseCommand.PurchaseDate is null && string.IsNullOrWhiteSpace(baseCommand.PurchasedCounty) && baseCommand.PurchaseAgeInMonths is null)
        {
            return;
        }

        draft.Case = baseCommand with { PurchaseDate = null, PurchasedCounty = null, PurchaseAgeInMonths = null };
        draft.HasPendingChanges = true;
        await sessionState.SetAsync(draft);
    }

    public async Task<IActionResult> OnGetCancelBabEdit()
    {
        await caseDraftDiscard.DiscardAllAsync(Rbse);
        return RedirectToPage("/Home");
    }

    private async Task LoadAsync()
    {
        var babTask     = babRepository.GetByRbseAsync(Rbse);
        var caseTask    = caseRepository.GetCaseByRbseAsync(Rbse);
        var batchTask   = batchRepository.GetBatchNumbersByRbseAsync(Rbse);
        var originsTask = lookups.GetAnimalOriginsAsync();
        var frTask      = lookups.GetLookupAsync(LookupTableId.FeedRisk);
        var hrTask      = lookups.GetLookupAsync(LookupTableId.HorizontalRisk);
        var mrTask      = lookups.GetLookupAsync(LookupTableId.MaternalRisk);

        await Task.WhenAll(babTask, caseTask, batchTask, originsTask, frTask, hrTask, mrTask);

        var bab        = await babTask;
        var caseRecord = await caseTask;
        Bab            = bab is not null ? BabFormViewModel.FromRecord(bab) : new BabFormViewModel();
        RowStampBase64 = bab?.RowStamp is not null ? Convert.ToBase64String(bab.RowStamp) : null;

        var (effectiveBirthDate, effectiveOrigin, hasPurchaseData) = await ResolveEffectiveCaseScalarsAsync(caseRecord);
        Origin              = effectiveOrigin;
        HasPurchaseData     = hasPurchaseData;
        CanEditBabControls  = EvaluateLegacyBabEditPermission(effectiveBirthDate, bab);
        BatchNumbers   = (await batchTask).ToList().AsReadOnly();
        AnimalOrigins  = (await originsTask).Select(x => new LookupItem(x.Id, x.Code, x.Description)).ToList();
        FeedRisks      = await frTask;
        HorizontalRisks = await hrTask;
        MaternalRisks  = await mrTask;
    }

    private bool EvaluateLegacyBabEditPermission(DateTime? birthDate, CaseBabRecord? babRecord)
    {
        if (!User.IsInRole("DataEntry"))
            return false;

        // Legacy CaseEntryBAB: VLA Data Entry always read-only; VLA Maintenance can edit.
        if (User.IsInRole("VLAAccess") && !User.IsInRole("VLAMaintenance"))
            return false;

        // Legacy MakeControlsWritable: editable only when BirthDate exists and
        // BirthDate >= 18/07/1988 OR a BAB row already exists.
        if (birthDate is not { } dob)
            return false;

        return dob.Date >= BabBirthDateThreshold || babRecord is not null;
    }

    /// <summary>Effective Case-level scalars (Date of Birth, Origin, Purchase fields) used by
    /// BAB's cascading display rules — overlays the shared cross-tab scalar draft (e.g. an
    /// unsaved Date of Birth just typed on the DEFRA tab, or Purchase data staged from the
    /// APHA/VLA tab) on top of the persisted record, matching legacy's single shared session
    /// dataset where every tab reads the same in-progress values. Only falls back to the
    /// persisted record when nothing at all has been staged yet for this case — never per field,
    /// since a staged Case snapshot represents a full replace, and a field left blank there is a
    /// deliberate clear, not a gap to fill from the stale DB value.</summary>
    private async Task<(DateTime? BirthDate, string? Origin, bool HasPurchaseData)> ResolveEffectiveCaseScalarsAsync(CaseRecord? caseRecord)
    {
        var staged = (await sessionState.GetAsync(Rbse))?.Case;

        var birthDate           = staged is not null ? staged.BirthDate : caseRecord?.BirthDate;
        var origin               = staged is not null ? staged.Origin : caseRecord?.Origin;
        var purchaseDate         = staged is not null ? staged.PurchaseDate : caseRecord?.PurchaseDate;
        var purchasedCounty      = staged is not null ? staged.PurchasedCounty : caseRecord?.PurchasedCounty;
        var purchaseAgeInMonths  = staged is not null ? staged.PurchaseAgeInMonths : caseRecord?.PurchaseAgeInMonths;

        var hasPurchaseData = purchaseDate.HasValue
            || !string.IsNullOrWhiteSpace(purchasedCounty)
            || purchaseAgeInMonths.HasValue;

        return (birthDate, origin, hasPurchaseData);
    }

    public class BabFormViewModel
    {
        public string? NatalCphh { get; set; }
        public string? Notes { get; set; }
        public string? TracedName { get; set; }
        public string? TracedAddress1 { get; set; }
        public string? TracedAddress2 { get; set; }
        public string? TracedAddress3 { get; set; }
        public string? TracedPostcode { get; set; }
        public string? FeedRisk { get; set; }
        public string? HorizontalRisk { get; set; }
        public string? MaternalRisk { get; set; }

        public static BabFormViewModel FromRecord(CaseBabRecord r) => new()
        {
            NatalCphh      = r.NatalCphh,      Notes          = r.Notes,
            TracedName     = r.TracedName,      TracedAddress1 = r.TracedAddress1,
            TracedAddress2 = r.TracedAddress2,  TracedAddress3 = r.TracedAddress3,
            TracedPostcode = r.TracedPostcode,  FeedRisk       = r.FeedRisk,
            HorizontalRisk = r.HorizontalRisk,  MaternalRisk   = r.MaternalRisk
        };

        /// <summary>Overlays a staged-but-not-yet-committed BAB edit (from another tab's
        /// cross-tab draft) so revisiting this tab shows the pending edit instead of the
        /// last-committed DB values.</summary>
        public void ApplyStagedCommand(EditCaseBabCommand c)
        {
            NatalCphh = c.NatalCphh;
            Notes = c.Notes;
            TracedName = c.TracedName;
            TracedAddress1 = c.TracedAddress1;
            TracedAddress2 = c.TracedAddress2;
            TracedAddress3 = c.TracedAddress3;
            TracedPostcode = c.TracedPostcode;
            FeedRisk = c.FeedRisk;
            HorizontalRisk = c.HorizontalRisk;
            MaternalRisk = c.MaternalRisk;
        }
    }
}
