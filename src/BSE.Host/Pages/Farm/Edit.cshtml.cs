using BSE.Host.Models.ViewModels;
using BSE.Host.Services;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Farm;

[Authorize(Policy = "DataEntry")]
public class EditModel : PageModel
{
    private readonly IFarmService _farm;
    private readonly ICurrentUserService _currentUser;
    private readonly ILookupDataService _lookups;
    private readonly IGeoLookupService _geoLookup;

    public EditModel(IFarmService farm, ICurrentUserService currentUser, ILookupDataService lookups, IGeoLookupService geoLookup)
    {
        _farm = farm;
        _currentUser = currentUser;
        _lookups = lookups;
        _geoLookup = geoLookup;
    }

    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    [BindProperty] public FarmEditViewModel? Farm { get; set; }

    public async Task<IActionResult> OnGetAsync(string cphh, string rbse)
    {
        Rbse = rbse;
        var record = await _farm.GetByCphhAsync(cphh);
        if (record is null) return NotFound();

        Farm = FarmEditViewModel.FromRecord(record);
        TempData["FarmRowStamp"] = record.RowStamp != null ? Convert.ToBase64String(record.RowStamp) : null;
        await LoadLookupsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadLookupsAsync();
        if (!ModelState.IsValid || Farm is null) return Page();

        var rowStampBase64 = TempData["FarmRowStamp"] as string;
        var rowStamp = rowStampBase64 != null ? Convert.FromBase64String(rowStampBase64) : null;

        var userId = await _currentUser.GetUserIdAsync();
        await _farm.UpdateAsync(Farm.ToUpdateCommand(rowStamp), userId);

        TempData["Success"] = "Farm updated successfully.";
        return RedirectToPage("/Case/Farm", new { rbse = Rbse });
    }

    private async Task LoadLookupsAsync()
    {
        var countyTask     = _lookups.GetLookupAsync(LookupTableId.BSECounty);
        var ahoTask        = _lookups.GetLookupAsync(LookupTableId.AHO);
        var herdTypeTask   = _lookups.GetHerdTypesAsync();
        var pedigreeTask   = _lookups.GetLookupAsync(LookupTableId.PedigreeType);
        var authCountyTask = _lookups.GetLookupAsync(LookupTableId.AuthorityCounty);

        await Task.WhenAll(countyTask, ahoTask, herdTypeTask, pedigreeTask, authCountyTask);

        ViewData["CountyOptions"]          = await countyTask;
        ViewData["AhoOptions"]             = await ahoTask;
        ViewData["HerdTypeOptions"]        = await herdTypeTask;
        ViewData["PedigreeOptions"]        = await pedigreeTask;
        ViewData["AuthorityCountyOptions"] = await authCountyTask;

        // Load authority and ADNS options pre-filtered for the currently selected values
        ViewData["AuthorityOptions"] = Farm?.AuthorityCountyID is > 0
            ? await _lookups.GetAuthoritiesByCountyAsync(Farm.AuthorityCountyID.Value)
            : (IEnumerable<LuAuthority>)[];

        ViewData["AdnsOptions"] = Farm?.AuthorityID is > 0
            ? await _lookups.GetADNSRegionsByAuthorityAsync(Farm.AuthorityID.Value)
            : (IEnumerable<LuADNSRegion>)[];
    }

    /// <summary>AJAX handler: returns authorities for a given authority county.</summary>
    public async Task<IActionResult> OnGetAuthoritiesAsync(int? authorityCountyId)
    {
        if (authorityCountyId is null or 0) return new JsonResult(Array.Empty<object>());
        var items = await _lookups.GetAuthoritiesByCountyAsync(authorityCountyId.Value);
        return new JsonResult(items.Select(a => new { id = a.Id, name = a.Name }));
    }

    /// <summary>AJAX handler: returns ADNS regions for a given local authority.</summary>
    public async Task<IActionResult> OnGetAdnsRegionsAsync(int? authorityId)
    {
        if (authorityId is null or 0) return new JsonResult(Array.Empty<object>());
        var items = await _lookups.GetADNSRegionsByAuthorityAsync(authorityId.Value);
        return new JsonResult(items.Select(r => new { id = r.Id, name = r.Name }));
    }

    /// <summary>AJAX handler: estimates the map reference from the parish centre for the given CPHH.</summary>
    public async Task<IActionResult> OnGetEstimateMapReferenceAsync(string? cphh)
    {
        if (string.IsNullOrEmpty(cphh) || cphh.Length < 5)
            return new JsonResult(new { error = "CPHH must be at least 5 characters to estimate a map reference." });

        var county = cphh[..2];
        var parish = cphh[2..5];

        var geo = await _geoLookup.GetMapReferenceAsync(county, parish);
        if (geo is null)
            return new JsonResult(new { error = "No map reference data found for the parish associated with this CPHH." });

        var centreX = (int.Parse(geo.XReference1) + int.Parse(geo.XReference2)) / 2;
        var centreY = (int.Parse(geo.YReference1) + int.Parse(geo.YReference2)) / 2;
        var mapRef = centreX.ToString("D4") + centreY.ToString("D4");

        return new JsonResult(new { mapReference = mapRef });
    }
}
