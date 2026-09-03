using BSE.Modules.AdnsExport.Models;
using BSE.Modules.AdnsExport.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace BSE.Host.Pages.AdnsExport;

[Authorize(Policy = "DEFRAMaintenance")]
public class NiModel(IAdnsExportService adnsExportService) : PageModel
{
    private const int PageSize = 10;
    private const string PreviewTempDataKey = "AdnsNiPreview";

    [BindProperty] [Required] public string EmailReference { get; set; } = string.Empty;
    [BindProperty] public List<NiCaseInput> Cases { get; set; } = [];
    [BindProperty] public string UserEmailAddress { get; set; } = string.Empty;
    [BindProperty] public bool SaveAdnsData { get; set; } = true;

    [BindProperty(SupportsGet = true)] public string SortColumn { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public AdnsExportPreview? Preview { get; private set; }
    public string? ErrorMessage { get; private set; }

    public int TotalCount => Preview?.Cases.Count ?? 0;
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public IReadOnlyList<AdnsCaseRecord> PagedCases =>
        ApplySorting(Preview?.Cases ?? []).Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    private IEnumerable<AdnsCaseRecord> ApplySorting(IEnumerable<AdnsCaseRecord> cases) => SortColumn switch
    {
        "Region" => SortDesc ? cases.OrderByDescending(c => c.AdnsRegionName) : cases.OrderBy(c => c.AdnsRegionName),
        "AdnsReference" => SortDesc ? cases.OrderByDescending(c => c.AdnsReference) : cases.OrderBy(c => c.AdnsReference),
        "ConfirmationDate" => SortDesc ? cases.OrderByDescending(c => c.ConfirmationDate) : cases.OrderBy(c => c.ConfirmationDate),
        "Rbse" => SortDesc ? cases.OrderByDescending(c => c.Rbse) : cases.OrderBy(c => c.Rbse),
        _ => cases.OrderBy(c => c.Rbse),
    };

    public IActionResult OnGet()
    {
        var previewJson = TempData.Peek(PreviewTempDataKey)?.ToString();
        if (!string.IsNullOrEmpty(previewJson))
        {
            Preview = System.Text.Json.JsonSerializer.Deserialize<AdnsExportPreview>(previewJson);
        }
        return Page();
    }

    public IActionResult OnPostPreview()
    {
        if (!ModelState.IsValid) return Page();

        var validCases = Cases.Where(c => !string.IsNullOrWhiteSpace(c.Rbse)).ToList();
        Preview = adnsExportService.PreviewNiExport(EmailReference, validCases);
        TempData[PreviewTempDataKey] = System.Text.Json.JsonSerializer.Serialize(Preview);
        return Page();
    }

    public async Task<IActionResult> OnPostDispatchAsync()
    {
        if (string.IsNullOrWhiteSpace(UserEmailAddress))
        {
            ModelState.AddModelError(nameof(UserEmailAddress), "Email address is required.");
            return Page();
        }

        var previewJson = TempData.Peek(PreviewTempDataKey)?.ToString();
        if (string.IsNullOrEmpty(previewJson))
        {
            ErrorMessage = "Session expired — please regenerate the preview.";
            return Page();
        }

        var preview = System.Text.Json.JsonSerializer.Deserialize<AdnsExportPreview>(previewJson);
        var cases = preview?.Cases.ToList() ?? [];
        var command = new DispatchAdnsCommand("NI", EmailReference, cases, UserEmailAddress, SaveAdnsData);

        try
        {
            await adnsExportService.DispatchAsync(command);
            TempData.Remove(PreviewTempDataKey);
            TempData["Success"] = "NI ADNS export dispatched successfully.";
            return RedirectToPage("/AdnsExport/Menu");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Dispatch failed: {ex.Message}";
            return Page();
        }
    }
}
