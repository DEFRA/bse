using BSE.Modules.Batch.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Batch;

/// <summary>
/// Migrated equivalent of legacy PrintBatch.aspx.
/// Accessible to VLA Data Entry and VLA Maintenance only (VLAAccess policy).
/// Allows the user to select a batch and a report type then download the report.
/// </summary>
[Authorize(Policy = "VLAAccess")]
public class PrintBatchModel(IBatchRepository batchRepository) : PageModel
{
    public static readonly IReadOnlyList<(string Value, string Label)> ReportTypes =
    [
        ("Clinical",    "Clinical"),
        ("FarmAndCase", "Farm and Case"),
        ("Feeds",       "Feeds"),
        ("Offspring",   "Offspring"),
        ("Pedigree",    "Pedigree"),
    ];

    [BindProperty(SupportsGet = true)]
    public short? BatchYear { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? BatchNumber { get; set; }

    [BindProperty]
    public string? ReportType { get; set; }

    /// <summary>Set after a valid lookup — number of cases in the batch.</summary>
    public int? BatchCaseCount { get; private set; }

    public Task<IActionResult> OnGetAsync() => Task.FromResult<IActionResult>(Page());

    /// <summary>Validates the batch+report selection and redirects to the report page.</summary>
    public async Task<IActionResult> OnPostDownloadAsync()
    {
        if (BatchYear is null)
            ModelState.AddModelError(nameof(BatchYear), "Enter a batch year.");
        if (BatchNumber is null)
            ModelState.AddModelError(nameof(BatchNumber), "Enter a batch number.");
        if (string.IsNullOrWhiteSpace(ReportType))
            ModelState.AddModelError(nameof(ReportType), "Select a report type.");

        if (!ModelState.IsValid)
            return Page();

        var batchId = await batchRepository.GetBatchIdAsync(BatchYear!.Value, BatchNumber!.Value);
        if (batchId is null)
        {
            ModelState.AddModelError(nameof(BatchYear), $"Batch {BatchYear}/{BatchNumber} was not found.");
            return Page();
        }

        var cases = await batchRepository.GetCasesByBatchIdAsync(batchId.Value);
        if (cases.Count == 0)
        {
            ModelState.AddModelError(string.Empty, $"Batch {BatchYear}/{BatchNumber} contains no cases.");
            return Page();
        }

        // Redirect to the appropriate report page, passing batch context.
        return RedirectToPage($"/Reports/{ReportType}",
            new { batchYear = BatchYear, batchNumber = BatchNumber });
    }
}
