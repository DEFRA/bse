using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.CaseWork;

[Authorize(Policy = "VLAMaintenance")]
public class CaseWorkMenuModel(ICaseWorkService caseWorkService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Rbse { get; set; }

    public CaseWorkRecord? Record { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(Rbse))
            Record = await caseWorkService.GetCaseWorkAsync(Rbse);
        return Page();
    }
}
