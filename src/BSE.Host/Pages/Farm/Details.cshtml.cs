using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Farm;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IFarmService _farm;
    private readonly ILookupDataService _lookups;

    public DetailsModel(IFarmService farm, ILookupDataService lookups)
    {
        _farm = farm;
        _lookups = lookups;
    }

    public FarmRecord? Farm { get; private set; }
    public IReadOnlyList<FarmRelationRecord> RelatedFarms { get; private set; } = [];
    public IReadOnlyList<HerdSizeRecord> HerdSizes { get; private set; } = [];
    public int ConfirmedCaseCount { get; private set; }
    public string? ADNSRegionName { get; private set; }

    public async Task<IActionResult> OnGetAsync(string cphh)
    {
        Farm = await _farm.GetByCphhAsync(cphh);
        if (Farm is null) return Page();

        var relatedTask = _farm.GetRelatedFarmsAsync(cphh);
        var herdTask = _farm.GetHerdSizesAsync(cphh);
        var countTask = _farm.GetConfirmedCaseCountAsync(cphh);
        var adnsTask = Farm.ADNSRegionID.HasValue ? _lookups.GetADNSRegionsAsync() : Task.FromResult<IEnumerable<LuADNSRegion>>(Array.Empty<LuADNSRegion>());

        await Task.WhenAll(relatedTask, herdTask, countTask, adnsTask);

        RelatedFarms = (await relatedTask).ToList().AsReadOnly();
        HerdSizes = (await herdTask).OrderByDescending(h => h.HerdYear).ToList().AsReadOnly();
        ConfirmedCaseCount = await countTask;

        if (Farm.ADNSRegionID.HasValue)
        {
            var regions = await adnsTask;
            ADNSRegionName = regions.FirstOrDefault(r => r.Id == Farm.ADNSRegionID.Value)?.Name;
        }

        return Page();
    }
}
