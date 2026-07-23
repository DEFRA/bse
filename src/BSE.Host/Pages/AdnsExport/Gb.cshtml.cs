using BSE.Modules.AdnsExport.Models;
using BSE.Modules.AdnsExport.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace BSE.Host.Pages.AdnsExport;

[Authorize(Policy = "DataEntry")]
public class GbModel(IAdnsExportService adnsExportService) : PageModel
{
    [BindProperty] [Required] public string EmailReference { get; set; } = string.Empty;
    [BindProperty] public int AdnsYear { get; set; } = DateTime.Today.Year;
    [BindProperty] public int StartAdnsNumber { get; set; } = 1;
    [BindProperty] public string UserEmailAddress { get; set; } = string.Empty;
    [BindProperty] public bool SaveAdnsData { get; set; } = true;

    public AdnsExportPreview? Preview { get; private set; }
    public LastAdnsReferenceRecord? LastReference { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        LastReference = await adnsExportService.GetLastReferenceAsync("GB");
        if (LastReference is not null)
        {
            AdnsYear = LastReference.LastAdnsReferenceYear ?? DateTime.Today.Year;
            StartAdnsNumber = (LastReference.LastAdnsReferenceNumber ?? 0) + 1;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostPreviewAsync()
    {
        if (!ModelState.IsValid) return Page();

        try
        {
            Preview = await adnsExportService.PreviewGbExportAsync(EmailReference, AdnsYear, StartAdnsNumber);
            // Store preview in TempData for dispatch
            TempData["AdnsGbPreviewCases"] = System.Text.Json.JsonSerializer.Serialize(Preview.Cases);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Preview generation failed: {ex.Message}";
        }
        return Page();
    }

    public async Task<IActionResult> OnPostDispatchAsync()
    {
        if (string.IsNullOrWhiteSpace(UserEmailAddress))
        {
            ModelState.AddModelError(nameof(UserEmailAddress), "Email address is required.");
            return Page();
        }

        // Rebuild preview from temp data
        var casesJson = TempData["AdnsGbPreviewCases"]?.ToString();
        if (string.IsNullOrEmpty(casesJson))
        {
            ErrorMessage = "Session expired — please regenerate the preview.";
            return Page();
        }

        var cases = System.Text.Json.JsonSerializer.Deserialize<List<AdnsCaseRecord>>(casesJson) ?? [];

        var command = new DispatchAdnsCommand(
            Area: "GB",
            EmailReference: EmailReference,
            Cases: cases,
            UserEmailAddress: UserEmailAddress,
            SaveAdnsData: SaveAdnsData);

        try
        {
            await adnsExportService.DispatchAsync(command);
            TempData["Success"] = "GB ADNS export dispatched successfully.";
            return RedirectToPage("/AdnsExport/Menu");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Dispatch failed: {ex.Message}";
            return Page();
        }
    }
}
