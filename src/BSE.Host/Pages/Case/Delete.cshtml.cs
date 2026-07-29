using BSE.Host.Services;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DataEntry")]
public class DeleteModel(ICaseService caseService, ICurrentUserService currentUserService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    public CaseRecord? CaseRecord { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        CaseRecord = await caseService.GetCaseAsync(Rbse);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = await currentUserService.GetUserIdAsync();
        var result = await caseService.DeleteCaseAsync(Rbse, userId);

        if (result == DeleteCaseResult.Success)
        {
            TempData["Success"] = $"Case {Rbse} has been deleted.";
            return RedirectToPage("/Case/Lookup");
        }

        var message = result switch
        {
            DeleteCaseResult.RbseNotFound => "Case not found.",
            DeleteCaseResult.HasLinkedRecords => "Cannot delete — case has linked records (feeds, clinical data, batch assignments, etc.).",
            DeleteCaseResult.AuditLogError => "Audit log error during delete.",
            DeleteCaseResult.DeleteError => "Database error during delete.",
            DeleteCaseResult.FarmDeleteError => "Case deleted but orphaned farm cleanup failed.",
            DeleteCaseResult.FarmCountError => "Farm case count error.",
            _ => $"Delete failed: {result}"
        };

        ErrorMessage = message;
        CaseRecord = await caseService.GetCaseAsync(Rbse);
        return Page();
    }
}
