using BSE.Modules.CaseManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace BSE.Host.Pages.Case;

[Authorize]
public class LookupModel : PageModel
{
    private readonly ICaseService _cases;

    public LookupModel(ICaseService cases) => _cases = cases;

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = "";
    public bool IsNotFound { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(Rbse)) return Page();

        // Normalise: strip slashes in case user enters formatted form (e.g. 00/26/00001 → 002600001)
        var normalised = Rbse.Trim().Replace("/", "");

        if (!System.Text.RegularExpressions.Regex.IsMatch(normalised, @"^\d{9}$"))
        {
            ModelState.AddModelError(nameof(Rbse), "Enter RBSE as 9 digits (for example 000260001).");
            return Page();
        }

        var caseRecord = await _cases.GetCaseAsync(normalised);
        if (caseRecord is null)
        {
            IsNotFound = true;
            return Page();
        }

        return RedirectToPage("/Case/Farm", new { rbse = caseRecord.Rbse });
    }
}
