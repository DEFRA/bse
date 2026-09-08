using System.Text.Json;
using BSE.Modules.OssExport.Models;
using BSE.Modules.OssExport.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.OssExport;

[Authorize(Policy = "VLAAccess")]
public class OssExportBse1bModel(IOssExportService ossExportService) : PageModel
{
    private const string GridEntriesSessionKey = "OssExportBSE1bGridEntries";
    private const string BatchResultSessionKey = "OssExportBSE1bBatchResult";
    private const string NextGridIdSessionKey = "OssExportBSE1bNextGridId";

    [BindProperty]
    public string RbseInput { get; set; } = string.Empty;

    public List<OssExportBatchEntryRecord> GridEntries { get; private set; } = new();
    public string? ValidationMessage { get; private set; }
    public BatchNumber1989Result? BatchResult { get; private set; }
    public bool IsGridEmpty => GridEntries.Count == 0;

    public async Task<IActionResult> OnGetAsync()
    {
        // Load grid from session if it exists
        LoadGridState();
        return Page();
    }

    public async Task<IActionResult> OnPostAddToGridAsync()
    {
        LoadGridState();

        // Validate RBSE format
        if (string.IsNullOrWhiteSpace(RbseInput))
        {
            ValidationMessage = "Please enter an RBSE number.";
            return Page();
        }

        // Parse RBSE to raw format (legacy-compatible)
        var normalized = RbseHelper.ParseToRaw(RbseInput);
        if (!RbseHelper.IsValid(normalized))
        {
            ValidationMessage = "Please enter a valid RBSE number (9 digits).";
            return Page();
        }

        // Check for duplicates in grid
        if (GridEntries.Any(e => e.Rbse == normalized))
        {
            ValidationMessage = $"The RBSE {RbseHelper.Format(normalized)} is already in the table.";
            return Page();
        }

        // Query database for RBSE details
        var details = await ossExportService.ValidateAndGetRbseDetailsAsync(normalized);
        if (details?.Cphh is null)
        {
            ValidationMessage = $"The RBSE {RbseHelper.Format(normalized)} was not found in the database.";
            return Page();
        }

        // Generate batch number if this is the first entry
        if (BatchResult is null)
        {
            BatchResult = await ossExportService.CreateBatchNumber1989Async();
            if (BatchResult is null)
            {
                ValidationMessage = "Failed to create batch number.";
                SaveGridState();
                return Page();
            }
        }

        // Add to grid
        var nextId = GetNextGridId();
        GridEntries.Add(new OssExportBatchEntryRecord
        {
            Id = nextId,
            Rbse = normalized,
            Cphh = details.Cphh,
            OwnerName = details.OwnerName,
            Address1 = details.Address1,
            BatchId = BatchResult.BatchId
        });

        // Clear input
        RbseInput = string.Empty;
        ValidationMessage = null;

        SaveGridState();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        LoadGridState();

        var entry = GridEntries.FirstOrDefault(e => e.Id == id);
        if (entry is not null)
        {
            GridEntries.Remove(entry);
        }

        // If grid is now empty, clear batch
        if (GridEntries.Count == 0)
        {
            BatchResult = null;
            HttpContext.Session.Remove(NextGridIdSessionKey);
        }

        SaveGridState();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostExportAsync()
    {
        LoadGridState();

        if (GridEntries.Count == 0 || BatchResult is null)
        {
            ValidationMessage = "The grid is empty. Please add RBSE records before exporting.";
            return Page();
        }

        try
        {
            // Generate file content
            var fileContent = await ossExportService.GenerateOssExportFileAsync(
                BatchResult.BatchId,
                BatchResult.BatchYear,
                BatchResult.BatchNumber,
                GridEntries
            );

            // Generate filename: EPBSE_YY.NNN (e.g., EPBSE_89.123)
            var yearSuffix = BatchResult.BatchYear.ToString("00").Substring(
                Math.Max(0, BatchResult.BatchYear.ToString("00").Length - 2));
            var batchNumberPadded = BatchResult.BatchNumber.ToString("000");
            var filename = $"EPBSE_{yearSuffix}.{batchNumberPadded}";

            // Return file for download
            var bytes = System.Text.Encoding.ASCII.GetBytes(fileContent);
            return File(bytes, "application/octet-stream", filename);
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Export failed: {ex.Message}";
            SaveGridState();
            return Page();
        }
    }

    private void SaveGridState()
    {
        if (GridEntries.Count > 0)
        {
            var json = JsonSerializer.Serialize(GridEntries);
            HttpContext.Session.SetString(GridEntriesSessionKey, json);
        }
        else
        {
            HttpContext.Session.Remove(GridEntriesSessionKey);
        }

        if (BatchResult is not null)
        {
            var json = JsonSerializer.Serialize(BatchResult);
            HttpContext.Session.SetString(BatchResultSessionKey, json);
        }
        else
        {
            HttpContext.Session.Remove(BatchResultSessionKey);
        }
    }

    private void LoadGridState()
    {
        GridEntries.Clear();

        var gridJson = HttpContext.Session.GetString(GridEntriesSessionKey);
        if (!string.IsNullOrEmpty(gridJson))
        {
            GridEntries = JsonSerializer.Deserialize<List<OssExportBatchEntryRecord>>(gridJson) ?? new();
        }

        var batchJson = HttpContext.Session.GetString(BatchResultSessionKey);
        if (!string.IsNullOrEmpty(batchJson))
        {
            BatchResult = JsonSerializer.Deserialize<BatchNumber1989Result>(batchJson);
        }
    }

    private int GetNextGridId()
    {
        var currentId = HttpContext.Session.GetInt32(NextGridIdSessionKey) ?? 0;
        currentId++;
        HttpContext.Session.SetInt32(NextGridIdSessionKey, currentId);
        return currentId;
    }
}
