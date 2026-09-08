using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages;

/// <summary>
/// Migrated equivalent of legacy MaintenanceConfirmation.aspx. Legacy passed the title and
/// message on the query string; they are carried in TempData here so the confirmation text
/// cannot be spoofed through a crafted link.
/// </summary>
[Authorize]
public class MaintenanceConfirmationModel : PageModel
{
    public const string TitleKey = "ConfirmationTitle";
    public const string SummaryKey = "ConfirmationSummary";
    public const string MessageKey = "ConfirmationMessage";

    public string ConfirmationTitle { get; private set; } = string.Empty;

    /// <summary>Short outcome line shown inside the green panel, below the title.</summary>
    public string ConfirmationSummary { get; private set; } = string.Empty;

    public string ConfirmationMessage { get; private set; } = string.Empty;

    public IActionResult OnGet()
    {
        ConfirmationTitle = TempData[TitleKey] as string ?? string.Empty;
        ConfirmationSummary = TempData[SummaryKey] as string ?? string.Empty;
        ConfirmationMessage = TempData[MessageKey] as string ?? string.Empty;

        // Legacy redirected to SessionError.aspx when the page was reached without either value.
        if (ConfirmationTitle.Length == 0 && ConfirmationMessage.Length == 0)
        {
            return RedirectToPage("/Home");
        }

        return Page();
    }
}
