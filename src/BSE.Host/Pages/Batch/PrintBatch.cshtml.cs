using System.Text.RegularExpressions;
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
    // Matches legacy BatchNumber.ascx: 4-digit year, up to 6-digit numeric serial.
    private static readonly Regex YearPattern = new(@"^\d{4}$", RegexOptions.Compiled);
    private static readonly Regex NumberPattern = new(@"^\d{1,6}$", RegexOptions.Compiled);

    public static readonly IReadOnlyList<(string Value, string Label)> ReportTypes =
    [
        ("Clinical",    "Clinical"),
        ("FarmAndCase", "Farm and Case"),
        ("Feeds",       "Feeds"),
        ("Offspring",   "Offspring"),
        ("Pedigree",    "Pedigree"),
    ];

    [BindProperty(SupportsGet = true)]
    public string? BatchYear { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? BatchNumber { get; set; }

    [BindProperty]
    public string? ReportType { get; set; }

    /// <summary>Set after a valid lookup — number of cases in the batch.</summary>
    public int? BatchCaseCount { get; private set; }

    public Task<IActionResult> OnGetAsync() => Task.FromResult<IActionResult>(Page());

    /// <summary>Validates the batch+report selection and redirects to the report page.</summary>
    public async Task<IActionResult> OnPostDownloadAsync()
    {
        var yearText = BatchYear?.Trim();
        var numberText = BatchNumber?.Trim();

        if (string.IsNullOrEmpty(yearText) && string.IsNullOrEmpty(numberText))
        {
            ModelState.AddModelError(nameof(BatchYear), "Enter batch number.");
        }
        else
        {
            if (!YearPattern.IsMatch(yearText ?? string.Empty))
                ModelState.AddModelError(nameof(BatchYear), "Enter a four digit year");
            if (!NumberPattern.IsMatch(numberText ?? string.Empty))
                ModelState.AddModelError(nameof(BatchNumber), "Enter a valid batch number");
        }

        if (string.IsNullOrWhiteSpace(ReportType))
            ModelState.AddModelError(nameof(ReportType), "Select the report type");

        if (!ModelState.IsValid)
            return Page();

        var batchYear = short.Parse(yearText!);
        var batchNumber = int.Parse(numberText!);

        var batchId = await batchRepository.GetBatchIdAsync(batchYear, batchNumber);
        if (batchId is null)
        {
            ModelState.AddModelError(nameof(BatchYear), $"Batch {batchYear}/{batchNumber} was not found.");
            return Page();
        }

        var cases = await batchRepository.GetCasesByBatchIdAsync(batchId.Value);
        if (cases.Count == 0)
        {
            ModelState.AddModelError(string.Empty, $"Batch {batchYear}/{batchNumber} contains no cases.");
            return Page();
        }

        // Redirect to the appropriate report page, passing batch context.
        return RedirectToPage($"/Reports/{ReportType}",
            new { batchYear, batchNumber });
    }
}
