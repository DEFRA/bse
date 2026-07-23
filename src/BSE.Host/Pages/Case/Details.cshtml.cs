using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly ICaseService _cases;

    public DetailsModel(ICaseService cases) => _cases = cases;

    public CaseRecord? Case { get; private set; }

    public async Task<IActionResult> OnGetAsync(string rbse)
    {
        Case = await _cases.GetCaseAsync(rbse);
        return Page();
    }
}
