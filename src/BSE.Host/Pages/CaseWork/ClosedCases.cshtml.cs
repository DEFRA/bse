using BSE.Modules.CaseWork.Services;
using Microsoft.AspNetCore.Authorization;

namespace BSE.Host.Pages.CaseWork;

[Authorize(Policy = "VLAMaintenance")]
public class ClosedCasesModel(ICaseWorkService caseWorkService) : CaseWorkReportPageModelBase
{
    public async Task OnGetAsync() => LoadCases(await caseWorkService.GetClosedCasesAsync());
}
