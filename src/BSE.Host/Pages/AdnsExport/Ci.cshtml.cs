using BSE.Modules.AdnsExport.Models;
using BSE.Modules.AdnsExport.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace BSE.Host.Pages.AdnsExport;

[Authorize(Policy = "DataEntry")]
public class CiModel(IAdnsExportService adnsExportService) : PageModel
{
    [BindProperty] [Required] public string EmailReference { get; set; } = string.Empty;
    [BindProperty] public int AdnsYear { get; set; } = DateTime.Today.Year;
    [BindProperty] public int StartAdnsNumber { get; set; } = 1;
    [BindProperty] public int JerseyCases { get; set; }
    [BindProperty] public int GuernseyCases { get; set; }
    [BindProperty] public int IsleOfManCases { get; set; }
    [BindProperty] public DateTime ConfirmationDate { get; set; } = DateTime.Today;
    [BindProperty] public string UserEmailAddress { get; set; } = string.Empty;

    public AdnsExportPreview? Preview { get; private set; }
    public string? ErrorMessage { get; private set; }

    public IActionResult OnGet() => Page();

    public IActionResult OnPostPreview()
    {
        if (!ModelState.IsValid) return Page();

        Preview = adnsExportService.PreviewCiExport(
            EmailReference, AdnsYear, StartAdnsNumber,
            JerseyCases, GuernseyCases, IsleOfManCases, ConfirmationDate);

        TempData["AdnsCiPreviewCases"] = System.Text.Json.JsonSerializer.Serialize(Preview.Cases);
        return Page();
    }

    public async Task<IActionResult> OnPostDispatchAsync()
    {
        if (string.IsNullOrWhiteSpace(UserEmailAddress))
        {
            ModelState.AddModelError(nameof(UserEmailAddress), "Email address is required.");
            return Page();
        }

        var casesJson = TempData["AdnsCiPreviewCases"]?.ToString();
        if (string.IsNullOrEmpty(casesJson))
        {
            ErrorMessage = "Session expired — please regenerate the preview.";
            return Page();
        }

        var cases = System.Text.Json.JsonSerializer.Deserialize<List<AdnsCaseRecord>>(casesJson) ?? [];

        var command = new DispatchAdnsCommand("CI", EmailReference, cases, UserEmailAddress, false);

        try
        {
            await adnsExportService.DispatchAsync(command);
            TempData["Success"] = "CI ADNS export dispatched successfully.";
            return RedirectToPage("/AdnsExport/Menu");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Dispatch failed: {ex.Message}";
            return Page();
        }
    }
}
