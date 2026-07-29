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
        if (string.IsNullOrWhiteSpace(Rbse)) return Page();

        var caseRecord = await _cases.GetCaseAsync(Rbse.Trim());
        if (caseRecord is null)
        {
            IsNotFound = true;
            return Page();
        }

        return RedirectToPage("/Case/Details", new { rbse = caseRecord.Rbse });
    }
}
