using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Farm;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IFarmService _farm;

    public DetailsModel(IFarmService farm) => _farm = farm;

    public FarmRecord? Farm { get; private set; }
    public IReadOnlyList<FarmRelationRecord> RelatedFarms { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string cphh)
    {
        Farm = await _farm.GetByCphhAsync(cphh);
        if (Farm is null) return Page();

        var related = await _farm.GetRelatedFarmsAsync(cphh);
        RelatedFarms = related.ToList().AsReadOnly();

        return Page();
    }
}
