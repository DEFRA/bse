using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.CaseWork;

public class CaseWorkMenuModel(ICaseWorkService caseWorkService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    public CaseWorkRecord? Record { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Record = await caseWorkService.GetCaseWorkAsync(Rbse);
        return Page();
    }
}
