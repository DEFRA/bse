using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Services;
using BSE.Modules.CaseManagement.Repositories;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages;

[Authorize]
public class HomeModel(IBatchService batchService, ICaseRepository caseRepository) : PageModel
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
        var currentYear  = (short)DateTime.Today.Year;
        var previousYear = (short)(currentYear - 1);

        var tasks = new List<Task>();

        if (User.IsInRole("VLAAccess"))
            tasks.Add(LoadBatchDataAsync());

        if (User.IsInRole("DEFRAAccess") || User.IsInRole("VLAAccess"))
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
    public async Task<IActionResult> OnPostLookupBatchAsync()
    {
        if (BatchYear is null)
            ModelState.AddModelError(nameof(BatchYear), "Enter a batch year.");

        if (BatchNumber is null)
            ModelState.AddModelError(nameof(BatchNumber), "Enter a batch number.");

        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        var batchId = await batchService.GetBatchIdAsync(BatchYear!.Value, BatchNumber!.Value);
        if (batchId is null)
        {
            BatchNotFound = true;
            ModelState.AddModelError(nameof(BatchYear), $"Batch {BatchYear}/{BatchNumber} was not found.");
            await OnGetAsync();
            return Page();
        }

        // Redirect to case entry; the batch year/number are passed so case entry
        // can pre-select the correct batch context.
        return RedirectToPage("/Case/New", new { batchYear = BatchYear, batchNumber = BatchNumber });
    }

    /// <summary>Validates, zero-pads (e.g. "9/87" → "000900087"), and redirects to the case lookup page; shows an inline error if the field is empty.</summary>
    public async Task<IActionResult> OnPostRbseLookupAsync()
    {
        if (string.IsNullOrWhiteSpace(LookupRbse))
        {
            ModelState.AddModelError(nameof(LookupRbse), "You must enter a RBSE number");
            await OnGetAsync();
            return Page();
        }
        var normalized = RbseHelper.ParseToRaw(LookupRbse);
        return RedirectToPage("/Case/Farm", new { Rbse = normalized });
    }

    /// <summary>Batch fields are pre-filled via redirect so the user sees the assigned number — matches legacy behaviour.</summary>
    public async Task<IActionResult> OnPostCreateBatchAsync()
    {
        var batch = await batchService.GetOrCreateBatchNumberAsync();
        TempData["SuccessMessage"] = $"Batch {batch.BatchYear}/{batch.BatchNumber} has been assigned. Enter the year and number above and select Go to add cases to this batch.";
        return RedirectToPage("/Home", new { batchYear = batch.BatchYear, batchNumber = batch.BatchNumber });
    }
}
