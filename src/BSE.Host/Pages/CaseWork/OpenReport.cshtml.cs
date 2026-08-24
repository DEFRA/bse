using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.CaseWork;

[Authorize(Policy = "DEFRAMaintenance")]
public class OpenReportModel(ICaseWorkService caseWorkService) : PageModel
{
    public IReadOnlyList<CaseWorkEntryRecord> Cases { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Cases = (await caseWorkService.GetOpenCasesAsync()).ToList();
    }

    public bool IsOverdue(DateTime? dueDate)
        => dueDate.HasValue && dueDate.Value.Date < DateTime.Today;
}
