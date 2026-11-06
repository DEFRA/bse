using BSE.Host.Services;
using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Services;
using BSE.Modules.CaseManagement.Repositories;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages;

[Authorize]
public class HomeModel(
    IBatchService batchService,
    ICaseRepository caseRepository,
    ICaseWizardStateService wizardState) : PageModel
{
    // ── Batch Number panel (VLAAccess role) ──────────────────────────────────

    /// <summary>Top-3 most recent batches with BSE1 case counts.</summary>
    public IReadOnlyList<LatestBatchRecord> LatestBatches { get; private set; } = [];

    /// <summary>Batch year entered in the lookup form (e.g. 2026).</summary>
    [BindProperty(SupportsGet = true)]
    public short? BatchYear { get; set; }

    /// <summary>Batch number entered in the lookup form (e.g. 1).</summary>
    [BindProperty(SupportsGet = true)]
    public int? BatchNumber { get; set; }
    /// <summary>True when a batch lookup was attempted but the batch was not found.</summary>
    public bool BatchNotFound { get; private set; }

    // ── RBSE Number panel (DEFRAAccess or VLAAccess role) ──────────────────────

    /// <summary>RBSE value entered in the lookup form on the home page.</summary>
    [BindProperty]
    public string? LookupRbse { get; set; }

    // ── RBSE Number panel (DEFRAMaintenance or VLAAccess role) ───────────────

    /// <summary>Latest RBSE reference in the current year.</summary>
    public string? LatestRbseCurrentYear { get; private set; }

    /// <summary>Latest RBSE reference in the previous year.</summary>
    public string? LatestRbsePreviousYear { get; private set; }

    /// <summary>Latest DBSE reference in the current year.</summary>
    public string? LatestDbseCurrentYear { get; private set; }

    /// <summary>Latest DBSE reference in the previous year.</summary>
    public string? LatestDbsePreviousYear { get; private set; }

    public async Task OnGetAsync()
    {
        await RestoreBatchFromWizardStateAsync();
        await LoadPageDataAsync();
    }

    /// <summary>Legacy Home.aspx.vb's Page_Load (Not IsPostBack) re-shows the batch number from
    /// session then immediately clears it — a batch picked on an earlier visit is shown exactly
    /// once when the user lands back on Home (e.g. the breadcrumb after visiting a case), then
    /// forgotten. Only runs on a genuine fresh navigation, not the internal re-renders POST
    /// handlers do after a validation failure (those must keep showing what was just submitted).</summary>
    private async Task RestoreBatchFromWizardStateAsync()
    {
        if (BatchYear is not null || BatchNumber is not null)
            return;

        var pending = await wizardState.GetAsync();
        if (pending is null)
            return;

        var parts = pending.BatchNumber.Split('/', 2);
        if (parts.Length == 2 && int.TryParse(parts[1], out var number))
        {
            BatchYear = short.TryParse(parts[0], out var year) ? year : null;
            BatchNumber = number;
        }

        await wizardState.ClearAsync();
    }

    private async Task LoadPageDataAsync()
    {
        var currentYear  = (short)DateTime.Today.Year;
        var previousYear = (short)(currentYear - 1);

        var tasks = new List<Task>();

        if (User.IsInRole("VLAAccess"))
            tasks.Add(LoadBatchDataAsync());

        if (User.IsInRole("DEFRAAccess") || User.IsInRole("VLAAccess") || User.IsInRole("DEFRAMaintenance") || User.IsInRole("VLAMaintenance"))
            tasks.Add(LoadRbseDataAsync(currentYear, previousYear));

        await Task.WhenAll(tasks);
    }

    private async Task LoadBatchDataAsync()
        => LatestBatches = await batchService.GetLatestBatchNumbersAsync();

    private async Task LoadRbseDataAsync(short currentYear, short previousYear)
    {
        var results = await Task.WhenAll(
            caseRepository.GetLatestRbseForYearAsync(currentYear),
            caseRepository.GetLatestRbseForYearAsync(previousYear),
            caseRepository.GetLatestDbseForYearAsync(currentYear),
            caseRepository.GetLatestDbseForYearAsync(previousYear));

        LatestRbseCurrentYear  = FormatRbse(results[0]);
        LatestRbsePreviousYear = FormatRbse(results[1]);
        LatestDbseCurrentYear  = FormatDbse(results[2]);
        LatestDbsePreviousYear = FormatDbse(results[3]);
    }

    // Mirrors legacy Common.vb FormatRBSE: strips slashes then inserts CC/YY/NNNNN.
    private static string? FormatRbse(string? raw)
        => RbseHelper.Format(raw);

    // Mirrors legacy Common.vb FormatDBSE: strips slashes then inserts YY/NNNNN.
    private static string? FormatDbse(string? raw)
        => RbseHelper.FormatDbse(raw);

    /// <summary>
    /// Validates the entered batch year/number. If found, redirects to New Case
    /// (the batch context is set by the case-entry page). Shows an error if not found.
    /// </summary>
    [Authorize] // lookup only; the New Case page it redirects to enforces DataEntry
    public async Task<IActionResult> OnPostLookupBatchAsync()
    {
        if (BatchYear is null)
            ModelState.AddModelError(nameof(BatchYear), "Enter a batch year.");

        if (BatchNumber is null)
            ModelState.AddModelError(nameof(BatchNumber), "Enter a batch number.");

        if (!ModelState.IsValid)
        {
            await LoadPageDataAsync();
            return Page();
        }

        var batchId = await batchService.GetBatchIdAsync(BatchYear!.Value, BatchNumber!.Value);
        if (batchId is null)
        {
            BatchNotFound = true;
            ModelState.AddModelError(nameof(BatchYear), $"Batch {BatchYear}/{BatchNumber} was not found.");
            await LoadPageDataAsync();
            return Page();
        }

        // Redirect to case entry; the batch year/number are passed so case entry
        // can pre-select the correct batch context.
        return RedirectToPage("/Case/New", new { batchYear = BatchYear, batchNumber = BatchNumber });
    }

    /// <summary>
    /// Legacy Home.aspx had a single Go button that took the batch number and the RBSE
    /// together: the batch was validated, held in session, and the case opened in case entry.
    /// Mirrors legacy btnGo_Click's full branching: existing GB/non-GB case, new GB/non-GB
    /// case, and the per-group restrictions on each path.
    /// </summary>
    [Authorize] // per-group rules are applied inside the handler (legacy btnGo_Click branching)
    public async Task<IActionResult> OnPostRbseLookupAsync()
    {
        // Legacy btnGo_Click validates the batch (BatchNumber1.Validate) before ever looking at
        // the RBSE box, so a simultaneously-invalid batch wins over an empty-RBSE error — matched
        // here by running this check first too. A batch is optional: blank year+number is valid.
        var (batchFailure, batchId) = await ResolveBatchAsync();
        if (batchFailure is not null)
            return batchFailure;

        if (string.IsNullOrWhiteSpace(LookupRbse))
        {
            ModelState.AddModelError(nameof(LookupRbse), "Enter an RBSE number in the format 00/00/00000");
            await LoadPageDataAsync();
            return Page();
        }

        var normalized = RbseHelper.ParseToRaw(LookupRbse);

        if (batchId.HasValue)
        {
            await wizardState.SetAsync(new CaseWizardState(
                RbseNumber: normalized,
                BatchNumber: $"{BatchYear}/{BatchNumber}",
                BatchId: batchId.Value));
        }
        else
        {
            await wizardState.ClearAsync();
        }

        var isVlaMaintenance = User.IsInRole("VLAMaintenance");
        var isVlaDataEntry = User.IsInRole("VLAAccess") && !isVlaMaintenance;
        var isDefraViewer = User.IsInRole("DEFRAAccess") && !User.IsInRole("DataEntry");

        var existing = await caseRepository.GetCaseByRbseAsync(normalized);

        if (existing is not null)
        {
            // Legacy: an existing non-GB case can only be opened by the VLA groups.
            if (existing.IsNonGbCase && !isVlaMaintenance && !isVlaDataEntry)
            {
                ModelState.AddModelError(nameof(LookupRbse), "This is not a GB case");
                await LoadPageDataAsync();
                return Page();
            }

            return RedirectToPage("/Case/Farm", new { Rbse = normalized });
        }

        // Case doesn't exist — legacy's non-GB reference block is "63/00" or "23/00".
        if (IsNonGbReference(normalized) && isVlaMaintenance)
            return RedirectToPage("/Case/NewNonGb", new { rbse = normalized });

        var rejection = GetNewCaseRejection(normalized, isVlaDataEntry, isDefraViewer);
        if (rejection is not null)
        {
            ModelState.AddModelError(nameof(LookupRbse), rejection);
            await LoadPageDataAsync();
            return Page();
        }

        // Legacy redirects a brand-new GB case straight to CaseEntryFarm.aspx, not a separate page.
        return RedirectToPage("/Case/Farm", new { rbse = normalized });
    }

    private static bool IsNonGbReference(string normalized) =>
        normalized.StartsWith("6300", StringComparison.Ordinal)
        || normalized.StartsWith("2300", StringComparison.Ordinal);

    private static string? GetNewCaseRejection(string normalized, bool isVlaDataEntry, bool isDefraViewer)
    {
        if (IsNonGbReference(normalized))
            return isVlaDataEntry ? "This case does not exist" : "This is not a GB case";

        // New GB case — DEFRA Viewer and VLA Data Entry cannot create new GB cases.
        if (isDefraViewer || isVlaDataEntry)
            return "This case does not exist";

        // Legacy: the RBSE year part (raw positions 2-3) must not contain 'X'.
        if (normalized.Length >= 4 && (normalized[2] is 'X' or 'x' || normalized[3] is 'X' or 'x'))
            return "This RBSE is not valid for a new case";

        return null;
    }

    private async Task<(IActionResult? Failure, int? BatchId)> ResolveBatchAsync()
    {
        if (BatchNumber.HasValue)
        {
            // Legacy parity: a blank year is valid when only the number is entered (BatchYear is nullable).
            var batchId = await batchService.GetBatchIdAsync(BatchYear, BatchNumber.Value);
            if (batchId is null)
            {
                BatchNotFound = true;
                ModelState.AddModelError(nameof(BatchYear), $"Batch {BatchYear}/{BatchNumber} was not found.");
                await LoadPageDataAsync();
                return (Page(), null);
            }

            return (null, batchId);
        }

        if (BatchYear.HasValue)
        {
            // Legacy parity: a year with no number never attempts a lookup; it just fails validation.
            ModelState.AddModelError(nameof(BatchNumber), "Enter a batch number.");
            await LoadPageDataAsync();
            return (Page(), null);
        }

        return (null, null);
    }

    /// <summary>Batch fields are pre-filled via redirect so the user sees the assigned number — matches legacy behaviour.</summary>
    [Authorize(Policy = "VLAAccess")] // the Batch Number panel is only rendered for VLA users
    public async Task<IActionResult> OnPostCreateBatchAsync()
    {
        var batch = await batchService.GetOrCreateBatchNumberAsync();
        TempData["SuccessMessage"] = $"Batch {batch.BatchYear}/{batch.BatchNumber} has been created. To add a case to this batch, enter an RBSE number in the RBSE Number section and select Go.";
        return RedirectToPage("/Home", new { batchYear = batch.BatchYear, batchNumber = batch.BatchNumber });
    }
}
