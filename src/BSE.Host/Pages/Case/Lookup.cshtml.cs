using BSE.Modules.CaseManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize]
public class LookupModel : PageModel
{
    private readonly ICaseService _cases;

    public LookupModel(ICaseService cases) => _cases = cases;

    [BindProperty(SupportsGet = true)] public string Rbse { get; set; } = "";
    public bool IsNotFound { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(Rbse))
        {
            if (HttpContext.Request.Query.ContainsKey("Rbse"))
                ModelState.AddModelError(nameof(Rbse), "Enter an RBSE number.");
            return Page();
        }

        var rbse = Rbse.Trim();
        var caseRecord = await _cases.GetCaseAsync(rbse);
        if (caseRecord is null)
        {
            // Non-GB RBSE (prefix 6300 or 2300) with no existing case → non-GB creation flow
            if (IsNonGbRbse(rbse))
                return RedirectToPage("/Case/NewNonGb", new { rbse });

            IsNotFound = true;
            return Page();
        }

        return RedirectToPage("/Case/Details", new { rbse = caseRecord.Rbse });
    }

    private static bool IsNonGbRbse(string rbse)
        => rbse.Length == 9 && (rbse.StartsWith("6300", StringComparison.Ordinal)
                               || rbse.StartsWith("2300", StringComparison.Ordinal));
}
