using BSE.Modules.CaseWork.Services;
using Microsoft.AspNetCore.Authorization;

namespace BSE.Host.Pages.CaseWork;

[Authorize(Policy = "VLAMaintenance")]
public class OpenCasesModel(ICaseWorkService caseWorkService) : CaseWorkReportPageModelBase
{
    protected override bool HasDueDateColumns => true;

    public async Task OnGetAsync() => LoadCases(await caseWorkService.GetOpenCasesAsync());
}
