using BSE.Modules.AdnsExport.Models;
using BSE.Modules.AdnsExport.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace BSE.Host.Pages.AdnsExport;

[Authorize(Policy = "DataEntry")]
public class NiModel(IAdnsExportService adnsExportService) : PageModel
{
    [BindProperty] [Required] public string EmailReference { get; set; } = string.Empty;
    [BindProperty] public List<NiCaseInput> Cases { get; set; } = [];
    [BindProperty] public string UserEmailAddress { get; set; } = string.Empty;
    [BindProperty] public bool SaveAdnsData { get; set; } = true;

    public AdnsExportPreview? Preview { get; private set; }
    public string? ErrorMessage { get; private set; }

    public IActionResult OnGet() => Page();

    public IActionResult OnPostPreview()
    {
        if (!ModelState.IsValid) return Page();

        var validCases = Cases.Where(c => !string.IsNullOrWhiteSpace(c.Rbse)).ToList();
        Preview = adnsExportService.PreviewNiExport(EmailReference, validCases);
        TempData["AdnsNiPreviewCases"] = System.Text.Json.JsonSerializer.Serialize(Preview.Cases);
        return Page();
    }

    public async Task<IActionResult> OnPostDispatchAsync()
    {
        if (string.IsNullOrWhiteSpace(UserEmailAddress))
        {
            ModelState.AddModelError(nameof(UserEmailAddress), "Email address is required.");
            return Page();
        }

        var casesJson = TempData["AdnsNiPreviewCases"]?.ToString();
        if (string.IsNullOrEmpty(casesJson))
        {
            ErrorMessage = "Session expired — please regenerate the preview.";
            return Page();
        }

        var cases = System.Text.Json.JsonSerializer.Deserialize<List<AdnsCaseRecord>>(casesJson) ?? [];
        var command = new DispatchAdnsCommand("NI", EmailReference, cases, UserEmailAddress, SaveAdnsData);

        try
        {
            await adnsExportService.DispatchAsync(command);
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
