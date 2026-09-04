using BSE.Host.Models.ViewModels;
using BSE.Host.Services;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using IGeoLookupService = BSE.Modules.ReferenceData.Services.IGeoLookupService;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Farm;

[Authorize(Policy = "FarmCreation")]
public class NewModel(IFarmService farmService, ICurrentUserService currentUserService, ILookupDataService lookups, IGeoLookupService geoLookup) : PageModel
{
    [BindProperty]
    public FarmEditViewModel Farm { get; set; } = new();

    /// <summary>When set, redirect back to MoveCase after farm creation.</summary>
    [BindProperty(SupportsGet = true)]
    public string? ReturnRbse { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnCphh { get; set; }

    public async Task<IActionResult> OnGetAsync(string? cphh = null)
    {
        Farm.CPHH = CphhNormalizer.Normalize(ReturnCphh ?? cphh);
        await LoadLookupsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadLookupsAsync();
        if (!ModelState.IsValid)
            return Page();

        Farm.CPHH = CphhNormalizer.Normalize(Farm.CPHH);

        var userId = await currentUserService.GetUserIdAsync();
        var command = Farm.ToAddCommand();
        await farmService.AddAsync(command, userId);

        TempData["Success"] = $"Farm {Farm.CPHH} has been created.";

        if (!string.IsNullOrEmpty(ReturnRbse))
            return RedirectToPage("/Case/MoveCase", new { rbse = ReturnRbse });

        return RedirectToPage("/Farm/Details", new { cphh = Farm.CPHH });
    }

    private async Task LoadLookupsAsync()
    {
        var countyTask     = lookups.GetLookupAsync(LookupTableId.BSECounty);
        var ahoTask        = lookups.GetLookupAsync(LookupTableId.AHO);
        var herdTypeTask   = lookups.GetHerdTypesAsync();
        var pedigreeTask   = lookups.GetLookupAsync(LookupTableId.PedigreeType);
        var authCountyTask = lookups.GetLookupAsync(LookupTableId.AuthorityCounty);

        await Task.WhenAll(countyTask, ahoTask, herdTypeTask, pedigreeTask, authCountyTask);

        ViewData["CountyOptions"]          = await countyTask;
        ViewData["AhoOptions"]             = await ahoTask;
        ViewData["HerdTypeOptions"]        = await herdTypeTask;
        ViewData["PedigreeOptions"]        = await pedigreeTask;
        ViewData["AuthorityCountyOptions"] = await authCountyTask;

        // New farm has no county/authority selected yet; dropdowns start empty and cascade via AJAX
        ViewData["AuthorityOptions"] = Farm.AuthorityCountyID is > 0
            ? await lookups.GetAuthoritiesByCountyAsync(Farm.AuthorityCountyID.Value)
            : (IEnumerable<LuAuthority>)[];

        ViewData["AdnsOptions"] = Farm.AuthorityID is > 0
            ? await lookups.GetADNSRegionsByAuthorityAsync(Farm.AuthorityID.Value)
            : (IEnumerable<LuADNSRegion>)[];
    }

    /// <summary>AJAX handler: returns authorities for a given authority county.</summary>
    public async Task<IActionResult> OnGetAuthoritiesAsync(int? authorityCountyId)
    {
        if (authorityCountyId is null or 0) return new JsonResult(Array.Empty<object>());
        var items = await lookups.GetAuthoritiesByCountyAsync(authorityCountyId.Value);
        return new JsonResult(items.Select(a => new { id = a.Id, name = a.Name }));
    }

    /// <summary>AJAX handler: returns ADNS regions for a given local authority.</summary>
    public async Task<IActionResult> OnGetAdnsRegionsAsync(int? authorityId)
    {
        if (authorityId is null or 0) return new JsonResult(Array.Empty<object>());
        var items = await lookups.GetADNSRegionsByAuthorityAsync(authorityId.Value);
        return new JsonResult(items.Select(r => new { id = r.Id, name = r.Name }));
    }

    /// <summary>AJAX handler: estimates the map reference from the parish centre for the given CPHH.</summary>
    public async Task<IActionResult> OnGetEstimateMapReferenceAsync(string? cphh)
    {
        if (string.IsNullOrEmpty(cphh) || cphh.Length < 5)
            return new JsonResult(new { error = "CPHH must be at least 5 characters to estimate a map reference." });

        var county = cphh[..2];
        var parish = cphh[2..5];

        var geo = await geoLookup.GetMapReferenceAsync(county, parish);
        if (geo is null)
            return new JsonResult(new { error = "No map reference data found for the parish associated with this CPHH." });

        var centreX = (int.Parse(geo.XReference1) + int.Parse(geo.XReference2)) / 2;
        var centreY = (int.Parse(geo.YReference1) + int.Parse(geo.YReference2)) / 2;
        var mapRef = centreX.ToString("D4") + centreY.ToString("D4");

        return new JsonResult(new { mapReference = mapRef });
    }
}
