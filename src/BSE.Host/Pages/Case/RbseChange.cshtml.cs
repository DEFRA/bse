using BSE.Host.Services;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class RbseChangeModel(ICaseService caseService, ICurrentUserService currentUserService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string OldRbse { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "New RBSE is required.")]
    public string NewRbse { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var userId = await currentUserService.GetUserIdAsync();
        var result = await caseService.ChangeRbseAsync(OldRbse, NewRbse, userId);

        if (result == ChangeRbseResult.Success)
        {
            TempData["Success"] = $"RBSE changed from {OldRbse} to {NewRbse}.";
            return RedirectToPage("/Case/Details", new { rbse = NewRbse });
        }

        ErrorMessage = result switch
        {
            ChangeRbseResult.OldRbseNotFound => $"Case '{OldRbse}' not found.",
            ChangeRbseResult.NewRbseAlreadyExists => $"Case '{NewRbse}' already exists.",
            _ => $"RBSE change failed: {result}"
        };

        return Page();
    }
}
