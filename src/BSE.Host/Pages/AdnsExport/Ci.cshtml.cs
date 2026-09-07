using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using BSE.Modules.AdnsExport.Configuration;
using BSE.Modules.AdnsExport.Models;
using BSE.Modules.AdnsExport.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace BSE.Host.Pages.AdnsExport;

[Authorize(Policy = "DEFRAMaintenance")]
public class CiModel(
    IAdnsExportService adnsExportService,
    IOptions<AdnsSmtpOptions> smtpOptions) : PageModel
{
    private const string PreviewTempDataKey = "AdnsCiPreview";
    private readonly AdnsSmtpOptions _smtpOptions = smtpOptions.Value;

    [BindProperty]
    [Required(ErrorMessage = "Enter an email reference.")]
    [StringLength(50, ErrorMessage = "Email reference must be 50 characters or fewer.")]
    public string EmailReference { get; set; } = string.Empty;

    [BindProperty]
    [Range(2000, 2100, ErrorMessage = "ADNS year must be between 2000 and 2100.")]
    public int AdnsYear { get; set; } = DateTime.Today.Year;

    [BindProperty]
    [Range(1, 99999, ErrorMessage = "Start ADNS number must be between 1 and 99999.")]
    public int StartAdnsNumber { get; set; } = 1;

    [BindProperty]
    [Range(0, 9999, ErrorMessage = "Jersey cases must be 0 or more.")]
    public int JerseyCases { get; set; }

    [BindProperty]
    [Range(0, 9999, ErrorMessage = "Guernsey cases must be 0 or more.")]
    public int GuernseyCases { get; set; }

    [BindProperty]
    [Range(0, 9999, ErrorMessage = "Isle of Man cases must be 0 or more.")]
    public int IsleOfManCases { get; set; }

    [BindProperty]
    public DateTime ConfirmationDate { get; set; } = DateTime.Today;

    [BindProperty]
    public string UserEmailAddress { get; set; } = string.Empty;

    // CI must remain false (no persisted Case rows for manual CI entries).
    [BindProperty]
    public bool SaveAdnsData { get; set; } = false;

    public AdnsExportPreview? Preview { get; private set; }
    public string? ErrorMessage { get; private set; }

    public string FromEmailAddress => _smtpOptions.FromAddress;
    public string DefaultToEmailAddress => _smtpOptions.ToAddress;

    public IActionResult OnGet()
    {
        if (TryLoadPreview(out var preview))
        {
            Preview = preview;
            if (string.IsNullOrWhiteSpace(UserEmailAddress))
                UserEmailAddress = DefaultToEmailAddress;
        }

        return Page();
    }

    public IActionResult OnPostGenerateReport()
    {
        // Generate report should not require recipient email.
        ModelState.Remove(nameof(UserEmailAddress));

        if (!ModelState.IsValid)
        {
            if (string.IsNullOrWhiteSpace(UserEmailAddress))
                UserEmailAddress = DefaultToEmailAddress;

            return Page();
        }

        try
        {
            Preview = adnsExportService.PreviewCiExport(
                EmailReference,
                AdnsYear,
                StartAdnsNumber,
                JerseyCases,
                GuernseyCases,
                IsleOfManCases,
                ConfirmationDate);

            TempData[PreviewTempDataKey] = JsonSerializer.Serialize(Preview);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Report generation failed: {ex.Message}";
        }

        if (string.IsNullOrWhiteSpace(UserEmailAddress))
            UserEmailAddress = DefaultToEmailAddress;

        return Page();
    }

    public async Task<IActionResult> OnPostDispatchAsync()
    {
        if (!TryLoadPreview(out var preview))
        {
            ErrorMessage = "Session expired — please generate the report again.";
            return Page();
        }

        Preview = preview;

        if (string.IsNullOrWhiteSpace(UserEmailAddress))
        {
            ModelState.AddModelError(nameof(UserEmailAddress), "Enter your email address.");
            return Page();
        }

        if (!new EmailAddressAttribute().IsValid(UserEmailAddress))
        {
            ModelState.AddModelError(nameof(UserEmailAddress), "Enter an email address in the correct format, like name@example.com.");
            return Page();
        }

        var command = new DispatchAdnsCommand(
            Area: "CI",
            EmailReference: EmailReference,
            Cases: preview!.Cases.ToList(),
            UserEmailAddress: UserEmailAddress,
            SaveAdnsData: SaveAdnsData);

        try
        {
            await adnsExportService.DispatchAsync(command);
            TempData.Remove(PreviewTempDataKey);
            TempData["Success"] = "CI ADNS export dispatched successfully.";
            return RedirectToPage("/AdnsExport/Menu");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Dispatch failed: {ex.Message}";
            return Page();
        }
    }

    private bool TryLoadPreview(out AdnsExportPreview? preview)
    {
        var json = TempData.Peek(PreviewTempDataKey)?.ToString();
        preview = string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<AdnsExportPreview>(json);

        return preview is not null;
    }
}