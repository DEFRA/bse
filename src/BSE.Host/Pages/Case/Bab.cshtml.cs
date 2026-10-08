using BSE.Infrastructure;
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
    IDbConnectionFactory connectionFactory,
    ICaseScalarDraftStateService caseScalarDraftState,
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

        // Legacy parity: BAB only ever becomes editable once Date of Birth is set (or a BAB row
        // already exists) — EvaluateLegacyBabEditPermission already covers "case doesn't exist yet"
        // the same way it covers "no Date of Birth yet", so no separate block is needed here.
        var canEdit = EvaluateLegacyBabEditPermission(currentCase, currentBab);
        if (!canEdit)
            return RedirectToPage(new { rbse = Rbse });

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
        }

        // Cross-tab staging (restores legacy's "one session, one commit" model): only the edit
        // path (an existing BAB row) stages — first-time creation keeps committing immediately,
        // since there is no earlier row for another tab's save to silently discard.
        if (!string.IsNullOrEmpty(rowStampBase64))
        {
            var rowStamp = Convert.FromBase64String(rowStampBase64);
            var edit = new EditCaseBabCommand(
                Rbse, Bab.NatalCphh, Bab.Notes,
                Bab.TracedName, Bab.TracedAddress1, Bab.TracedAddress2, Bab.TracedAddress3,
                Bab.TracedPostcode, Bab.FeedRisk, Bab.HorizontalRisk, Bab.MaternalRisk,
                rowStamp);

            var draft = await caseScalarDraftState.GetAsync(Rbse) ?? new CaseScalarDraftState { Rbse = Rbse };
            draft.BabBaseRowStampBase64 ??= rowStampBase64;
            draft.Bab = edit with { RowStamp = Convert.FromBase64String(draft.BabBaseRowStampBase64) };
            draft.BabOrigin = Origin;
            draft.HasPendingChanges = true;
            await caseScalarDraftState.SetAsync(draft);

            var userId = await currentUser.GetUserIdAsync();
            var (failureRedirect, commitOutcome) = await CaseCommitHelper.CommitAllAsync(
                this, caseEditOrchestration, Rbse, userId,
                result => $"Unable to save BAB changes: {result}.");
            if (failureRedirect is not null)
                return failureRedirect;

            if (CaseCommitHelper.TryStageWarnings(this, commitOutcome!, Rbse) is { } warningRedirect)
                return warningRedirect;
        }
        else
        {
            using var conn = connectionFactory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            var add = new AddCaseBabCommand(
                Rbse, Bab.NatalCphh, Bab.Notes,
                Bab.TracedName, Bab.TracedAddress1, Bab.TracedAddress2, Bab.TracedAddress3,
                Bab.TracedPostcode, Bab.FeedRisk, Bab.HorizontalRisk, Bab.MaternalRisk);
            await babRepository.AddAsync(add, Origin, conn, tx);

            tx.Commit();
        }

        // Legacy parity: CaseEntrySave.aspx auto-redirects to Home.aspx on a fully successful
        // save, clearing the session case state — not back to the tab the user was on.
        return RedirectToPage("/Home");
    }

    /// <summary>
    /// Validates this tab's fields and, if valid, stages them into the shared cross-tab
    /// draft (without committing) before navigating to another tab. Only applies once a
    /// BAB row already exists — there is nothing to stage before the row is first created.
    /// </summary>
    public async Task<IActionResult> OnPostStageAndGotoAsync(string targetPage, string? rowStampBase64)
    {
        if (!User.IsInRole("DataEntry"))
            return Forbid();

        if (string.IsNullOrEmpty(rowStampBase64))
            return RedirectToPage(targetPage, new { rbse = Rbse });

        var currentBabTask = babRepository.GetByRbseAsync(Rbse);
        var currentCaseTask = caseRepository.GetCaseByRbseAsync(Rbse);
        await Task.WhenAll(currentBabTask, currentCaseTask);

        var currentBab = await currentBabTask;
        var currentCase = await currentCaseTask;

        if (currentCase is null || !EvaluateLegacyBabEditPermission(currentCase, currentBab))
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
        }

        var rowStamp = Convert.FromBase64String(rowStampBase64);
        var edit = new EditCaseBabCommand(
            Rbse, Bab.NatalCphh, Bab.Notes,
            Bab.TracedName, Bab.TracedAddress1, Bab.TracedAddress2, Bab.TracedAddress3,
            Bab.TracedPostcode, Bab.FeedRisk, Bab.HorizontalRisk, Bab.MaternalRisk,
            rowStamp);

        var draft = await caseScalarDraftState.GetAsync(Rbse) ?? new CaseScalarDraftState { Rbse = Rbse };
        draft.BabBaseRowStampBase64 ??= rowStampBase64;
        draft.Bab = edit with { RowStamp = Convert.FromBase64String(draft.BabBaseRowStampBase64) };
        draft.BabOrigin = Origin;
        draft.HasPendingChanges = true;
        await caseScalarDraftState.SetAsync(draft);

        return RedirectToPage(targetPage, new { rbse = Rbse });
    }

    private async Task ApplyStagedBabOverlayAsync()
    {
        var staged = await caseScalarDraftState.GetAsync(Rbse);
        if (staged?.Bab is not null)
            Bab.ApplyStagedCommand(staged.Bab);
    }

    public async Task<IActionResult> OnGetCancelBabEdit()
    {
        await caseScalarDraftState.ClearAsync(Rbse);
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
        Origin         = caseRecord?.Origin;
        HasPurchaseData = caseRecord is not null
            && (caseRecord.PurchaseDate.HasValue
                || !string.IsNullOrWhiteSpace(caseRecord.PurchasedCounty)
                || caseRecord.PurchaseAgeInMonths.HasValue);
        CanEditBabControls = EvaluateLegacyBabEditPermission(caseRecord, bab);
        BatchNumbers   = (await batchTask).ToList().AsReadOnly();
        AnimalOrigins  = (await originsTask).Select(x => new LookupItem(x.Id, x.Code, x.Description)).ToList();
        FeedRisks      = await frTask;
        HorizontalRisks = await hrTask;
        MaternalRisks  = await mrTask;
    }

    private bool EvaluateLegacyBabEditPermission(CaseRecord? caseRecord, CaseBabRecord? babRecord)
    {
        if (!User.IsInRole("DataEntry"))
            return false;

        // Legacy CaseEntryBAB: VLA Data Entry always read-only; VLA Maintenance can edit.
        if (User.IsInRole("VLAAccess") && !User.IsInRole("VLAMaintenance"))
            return false;

        // Legacy MakeControlsWritable: editable only when BirthDate exists and
        // BirthDate >= 18/07/1988 OR a BAB row already exists.
        if (caseRecord?.BirthDate is not DateTime birthDate)
            return false;

        return birthDate.Date >= BabBirthDateThreshold || babRecord is not null;
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
