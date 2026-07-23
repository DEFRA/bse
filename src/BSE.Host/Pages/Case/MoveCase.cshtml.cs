using BSE.Host.Services;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class MoveCaseModel(ICaseService caseService, ICurrentUserService currentUserService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "New CPHH is required.")]
    public string NewCphh { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var userId = await currentUserService.GetUserIdAsync();
        var result = await caseService.MoveCaseAsync(Rbse, NewCphh, userId);

        if (result == MoveCaseResult.Success)
        {
            TempData["Success"] = $"Case {Rbse} has been moved to farm {NewCphh}.";
            return RedirectToPage("/Case/Details", new { rbse = Rbse });
        }

        ErrorMessage = result switch
        {
            MoveCaseResult.NewFarmNotFound => $"Farm '{NewCphh}' not found.",
            MoveCaseResult.RbseNotFound => "Case not found.",
            MoveCaseResult.NoOldFarmCases => "No cases found on the original farm.",
            MoveCaseResult.AuditLogError => "Audit log error during move.",
            MoveCaseResult.CaseUpdateError => "Database error updating case farm.",
            MoveCaseResult.OldFarmDeleteError => "Case moved but old farm cleanup failed.",
            _ => $"Move failed: {result}"
        };

        return Page();
    }
}
