using BSE.Host.Services;
using BSE.Modules.CaseManagement.Commands;
using BSE.Modules.CaseManagement.Enums;
using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize(Policy = "DEFRAMaintenance")]
public class FinalResultEntryModel(
    ICaseService cases,
    IFarmService farmService,
    ILookupDataService lookupService,
    ICurrentUserService currentUser) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? LookupRbse { get; set; }

    public CaseDetailRecord? CaseDetails { get; private set; }
    public FarmRecord? Farm { get; private set; }
    public IReadOnlyDictionary<string, string> TestTypeDescriptions { get; private set; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> TestResultDescriptions { get; private set; } = new Dictionary<string, string>();
    public bool IsNotFound { get; private set; }

    [BindProperty] public string Rbse { get; set; } = "";
    [BindProperty] public string? FinalResult { get; set; }
    [BindProperty] public DateTime? FinalResultDate { get; set; }
    [BindProperty] public string? Dbse { get; set; }

    public async Task OnGetAsync()
    {
        await LoadLookupsAsync();
        if (string.IsNullOrWhiteSpace(LookupRbse)) return;

        var details = await cases.GetCaseDetailsAsync(LookupRbse.Trim());
        if (details is null)
        {
            IsNotFound = true;
            return;
        }

        CaseDetails     = details;
        Farm            = await farmService.GetByCphhAsync(details.Case.Cphh);
        Rbse            = details.Case.Rbse;
        FinalResult     = details.Case.FinalResult;
        FinalResultDate = details.Case.FinalResultDate;
        Dbse            = details.Case.Dbse;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadLookupsAsync();

        var details = await cases.GetCaseDetailsAsync(Rbse.Trim());
        if (details is null)
        {
            ModelState.AddModelError(nameof(Rbse), "Case not found.");
            return Page();
        }

        CaseDetails = details;
        Farm = await farmService.GetByCphhAsync(details.Case.Cphh);

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

        var userId  = await currentUser.GetUserIdAsync();
        var command = new EditFinalResultCommand(
            Rbse: Rbse.Trim(),
            FinalResult: FinalResult,
            FinalResultDate: FinalResultDate,
            RetrospectiveTestType: null,
            RetrospectiveResult: null,
            RetrospectiveResultDate: null,
            RetrospectiveComment: null,
            LabComment: details.Case?.LabComment,
            RowStamp: details.Case?.RowStamp ?? []);

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

    private async Task LoadLookupsAsync()
    {
        var types   = await lookupService.GetTestTypesAsync();
        var results = await lookupService.GetTestResultsAsync();
        TestTypeDescriptions   = types.ToDictionary(t => t.Code, t => t.Description);
        TestResultDescriptions = results.ToDictionary(t => t.Code, t => t.Description);
    }
}
