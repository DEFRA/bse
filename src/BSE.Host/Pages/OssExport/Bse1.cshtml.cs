using System.Text;
using System.ComponentModel.DataAnnotations;
using BSE.Modules.Batch.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.OssExport;

[Authorize(Policy = "VLAAccess")]
public class OssExportBse1Model(IBatchService batchService) : PageModel
{
    [BindProperty]
    [Range(1900, 9999, ErrorMessage = "Enter a valid batch year.")]
    public short? BatchYear { get; set; } = (short)DateTime.Today.Year;

    [BindProperty]
    [Range(1, int.MaxValue, ErrorMessage = "Enter a valid batch number.")]
    public int? BatchNumber { get; set; }

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
        // Display page
    }

    public async Task<IActionResult> OnPostDownloadAsync()
    {
        if (BatchNumber is null)
            ModelState.AddModelError(nameof(BatchNumber), "Enter a batch number.");

        if (!ModelState.IsValid)
            return Page();

        try
        {
            var batchId = await batchService.GetBatchIdAsync(BatchYear!.Value, BatchNumber!.Value);
            if (batchId is null)
            {
                ErrorMessage = $"Batch {BatchYear}/{BatchNumber} was not found.";
                return Page();
            }

            var cases = await batchService.GetCasesByBatchIdAsync(batchId.Value);
            if (cases.Count == 0)
            {
                ErrorMessage = $"Batch {BatchYear}/{BatchNumber} contains no cases.";
                return Page();
            }

            return BuildLegacyBse1File(BatchYear.Value, BatchNumber.Value, cases);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Export failed: {ex.Message}";
        }

        return Page();
    }

    private static FileContentResult BuildLegacyBse1File(
        short batchYear,
        int batchNumber,
        IReadOnlyList<BSE.Modules.Batch.Models.BatchCaseSummaryRecord> cases)
    {
        var batchSuffix = $"{batchYear % 100:00}{batchNumber % 1000:000}";
        var sb = new StringBuilder();

        foreach (var row in cases)
        {
            sb.Append('|')
              .Append(batchSuffix)
              .Append('|')
              .Append(row.Rbse)
              .Append('|')
              .Append(row.Cphh ?? string.Empty)
              .Append('|')
              .AppendLine();
        }

        var fileName = $"EPBSE_{batchYear % 100:00}.{batchNumber % 1000:000}";
        return new FileContentResult(Encoding.UTF8.GetBytes(sb.ToString()), "text/plain")
        {
            FileDownloadName = fileName
        };
    }
}
