using BSE.Modules.OssExport.Models;
using BSE.Modules.OssExport.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.OssExport;

[Authorize(Policy = "VLAAccess")]
public class OssExportBse1Model(IOssExportService ossExportService) : PageModel
{
    public BatchNumber1989Result? BatchResult { get; private set; }
    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
        // Display page
    }

    public async Task<IActionResult> OnPostAsync()
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
