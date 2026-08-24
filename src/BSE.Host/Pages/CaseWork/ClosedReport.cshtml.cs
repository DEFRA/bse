using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.CaseWork;

[Authorize(Policy = "DEFRAMaintenance")]
public class ClosedReportModel(ICaseWorkService caseWorkService) : PageModel
{
    public IReadOnlyList<CaseWorkEntryRecord> Cases { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Cases = (await caseWorkService.GetClosedCasesAsync()).ToList();
    }
}
