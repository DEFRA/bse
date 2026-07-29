using BSE.Modules.OssExport.Models;
using BSE.Modules.OssExport.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.OssExport;

[Authorize(Policy = "DataEntry")]
public class OssExportMenuModel(IOssExportService ossExportService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    public OssExportRecord? ExportRecord { get; private set; }
    public BatchNumber1989Result? BatchResult { get; private set; }
    public bool HasSearched { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(Rbse))
        {
            HasSearched = true;
            ExportRecord = await ossExportService.GetExportDetailsByRbseAsync(Rbse);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostPopulateAsync()
    {
        try
        {
            await ossExportService.PopulateStagingTablesAsync();
            BatchResult = await ossExportService.CreateBatchNumber1989Async();
            if (BatchResult is null)
                ErrorMessage = "Staging tables populated but batch number creation failed.";
            else
                TempData["Success"] = $"Staging tables populated. Batch {BatchResult.BatchYear}/{BatchResult.BatchNumber} created.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Export failed: {ex.Message}";
        }
        return Page();
    }
}
