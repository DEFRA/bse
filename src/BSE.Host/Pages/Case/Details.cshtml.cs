using BSE.Modules.CaseManagement.Models;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

[Authorize]
public class DetailsModel(ICaseService cases, IFarmService farms) : PageModel
{
    public CaseRecord? Case { get; private set; }
    public FarmRecord? Farm { get; private set; }
    public IReadOnlyList<FarmRelationRecord> RelatedFarms { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string rbse)
    {
        Case = await cases.GetCaseAsync(rbse);
        if (Case is not null && !string.IsNullOrWhiteSpace(Case.Cphh))
        {
            Farm = await farms.GetByCphhAsync(Case.Cphh);
            var related = await farms.GetRelatedFarmsAsync(Case.Cphh);
            RelatedFarms = related.ToList().AsReadOnly();
        }
        return Page();
    }
}
