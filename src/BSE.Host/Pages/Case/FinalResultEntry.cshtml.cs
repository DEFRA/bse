using BSE.Host.Services;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DEFRAMaintenance")]
public class FinalResultEntryModel(ICaseService cases, ICurrentUserService currentUser) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? LookupRbse { get; set; }

    public CaseRecord? CurrentCase { get; private set; }
    public bool IsNotFound { get; private set; }

    [BindProperty] public string Rbse { get; set; } = "";
    [BindProperty] public string? FinalResult { get; set; }
    [BindProperty] public DateTime? FinalResultDate { get; set; }
    [BindProperty] public string? Dbse { get; set; }

    public async Task OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(LookupRbse)) return;

        CurrentCase = await cases.GetCaseAsync(LookupRbse.Trim());
        if (CurrentCase is null)
        {
            IsNotFound = true;
            return;
        }

        Rbse = CurrentCase.Rbse;
        FinalResult = CurrentCase.FinalResult;
        FinalResultDate = CurrentCase.FinalResultDate;
        Dbse = CurrentCase.Dbse;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        CurrentCase = await cases.GetCaseAsync(Rbse.Trim());
        if (CurrentCase is null)
        {
            ModelState.AddModelError(nameof(Rbse), "Case not found.");
            return Page();
        }

        if (string.IsNullOrWhiteSpace(FinalResult))
        {
            ModelState.AddModelError(nameof(FinalResult), "Enter a final result.");
            return Page();
        }

        if (FinalResultDate is null)
        {
            ModelState.AddModelError(nameof(FinalResultDate), "Enter a final result date.");
            return Page();
        }

        var userId = await currentUser.GetUserIdAsync();
        var command = new EditFinalResultCommand(
            Rbse: Rbse.Trim(),
            FinalResult: FinalResult,
            FinalResultDate: FinalResultDate,
            Dbse: Dbse);

        var result = await cases.SaveFinalResultAsync(command, userId);

        if (result == EditCaseResult.Success)
        {
            TempData["SuccessMessage"] = $"Final result for {Rbse} saved successfully.";
            return RedirectToPage("/Case/Details", new { rbse = Rbse.Trim() });
        }

        ModelState.AddModelError(string.Empty, result switch
        {
            EditCaseResult.RbseNotFound => $"Case '{Rbse}' not found.",
            EditCaseResult.ConcurrencyConflict => "This case was modified by another user. Reload and try again.",
            _ => $"Failed to save final result (error {(int)result})."
        });

        return Page();
    }
}
