using BSE.Modules.CaseWork.Models;
using BSE.Modules.CaseWork.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.CaseWork;

[Authorize(Policy = "VLAMaintenance")]
public class ClosedCasesModel(ICaseWorkService caseWorkService) : PageModel
{
    private const int PageSize = 10;

    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public IEnumerable<CaseWorkEntryRecord> Cases { get; private set; } = [];

    public int TotalCount => Cases.Count();
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public IReadOnlyList<CaseWorkEntryRecord> PagedCases =>
        Cases.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    public async Task OnGetAsync()
    {
        Cases = await caseWorkService.GetClosedCasesAsync();

        if (PageNumber < 1) PageNumber = 1;
        if (PageNumber > TotalPages) PageNumber = TotalPages;
    }
}
