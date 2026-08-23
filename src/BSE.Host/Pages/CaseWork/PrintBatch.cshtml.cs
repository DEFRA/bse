using BSE.Modules.Batch.Models;
using BSE.Modules.Batch.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.CaseWork;

[Authorize(Policy = "VLAAccess")]
public class PrintBatchModel(IBatchService batchService) : PageModel
{
    [BindProperty(SupportsGet = true)] public short? BatchYear { get; set; }
    [BindProperty(SupportsGet = true)] public int? BatchNumber { get; set; }

    public IReadOnlyList<BatchCaseSummaryRecord> Cases { get; private set; } = [];
    public bool IsNotFound { get; private set; }
    public bool HasSearched { get; private set; }

    public async Task OnGetAsync()
    {
        if (BatchYear is null || BatchNumber is null) return;

        HasSearched = true;
        var batchId = await batchService.GetBatchIdAsync(BatchYear.Value, BatchNumber.Value);
        if (batchId is null)
        {
            IsNotFound = true;
            return;
        }

        Cases = await batchService.GetCasesByBatchIdAsync(batchId.Value);
    }

    public IActionResult OnPost()
        => RedirectToPage(new { BatchYear, BatchNumber });
}
