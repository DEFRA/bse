using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using BSE.Modules.AdnsExport.Configuration;
using BSE.Modules.AdnsExport.Models;
using BSE.Modules.AdnsExport.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace BSE.Host.Pages.AdnsExport;

/// <summary>State and helpers shared by the CI, GB and NI ADNS export pages.</summary>
public abstract class AdnsExportPageModelBase(
    IAdnsExportService adnsExportService,
    IOptions<AdnsMsGraphOptions> msGraphOptions) : PageModel, IAdnsExportPageModel
{
    private readonly AdnsMsGraphOptions _msGraphOptions = msGraphOptions.Value;

    protected IAdnsExportService ExportService { get; } = adnsExportService;

    [BindProperty]
    public string UserEmailAddress { get; set; } = string.Empty;

    [BindProperty]
    public string Message { get; set; } = string.Empty;

    public AdnsExportPreview? Preview { get; protected set; }
    public string? ErrorMessage { get; protected set; }

    public string FromEmailAddress => _msGraphOptions.FromAddress;
    public string DefaultToEmailAddress => _msGraphOptions.ToAddress;

    protected void ApplyDefaultRecipientIfBlank()
    {
        if (string.IsNullOrWhiteSpace(UserEmailAddress))
            UserEmailAddress = DefaultToEmailAddress;
    }

    protected static AdnsExportPreview? DeserializePreview(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<AdnsExportPreview>(json);

    protected bool HasValidRecipient()
    {
        if (string.IsNullOrWhiteSpace(UserEmailAddress))
        {
            ModelState.AddModelError(nameof(UserEmailAddress), "Enter your email address.");
            return false;
        }

        if (!new EmailAddressAttribute().IsValid(UserEmailAddress))
        {
            ModelState.AddModelError(nameof(UserEmailAddress), "Enter an email address in the correct format, like name@example.com.");
            return false;
        }

        return true;
    }

    protected void SetSessionExpired() =>
        ErrorMessage = "Session expired — please generate the report again.";

    /// <summary>Sends the export; on success queues the confirmation message, on failure sets <see cref="ErrorMessage"/>.</summary>
    protected async Task<bool> TryDispatchAsync(DispatchAdnsCommand command)
    {
        try
        {
            await ExportService.DispatchAsync(command);
            TempData["SuccessMessage"] = "The message has been successfully sent. You should receive a confirmation e-mail shortly.";
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Dispatch failed: {ex.Message}";
            return false;
        }
    }
}
