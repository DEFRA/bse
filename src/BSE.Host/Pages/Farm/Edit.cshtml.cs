using BSE.Host.Models.ViewModels;
using BSE.Host.Services;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using IGeoLookupService = BSE.Host.Services.IGeoLookupService;
using BSE.SharedKernel;
using Microsoft.Data.SqlClient;
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
    public int ConfirmedCaseCount { get; private set; }

    public async Task<IActionResult> OnGetAsync(string cphh, string? rbse = null)
    {
        cphh = CphhNormalizer.Normalize(cphh);
        Rbse = rbse ?? string.Empty;
        var record = await _farm.GetByCphhAsync(cphh);
        if (record is null) return NotFound();

        Farm = FarmEditViewModel.FromRecord(record);
        ConfirmedCaseCount = await _farm.GetConfirmedCaseCountAsync(cphh);
        TempData["FarmRowStamp"] = record.RowStamp != null ? Convert.ToBase64String(record.RowStamp) : null;
        await LoadLookupsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Farm is not null)
        {
            Farm.CPHH = CphhNormalizer.Normalize(Farm.CPHH);
        }

        await LoadLookupsAsync();

        if (Farm is not null && !string.IsNullOrWhiteSpace(Farm.CPHH))
        {
            ConfirmedCaseCount = await _farm.GetConfirmedCaseCountAsync(Farm.CPHH);
        }

        // Validate map reference is within the parish for the CPHH (mirrors legacy MapReference1_MapReferenceChanged)
        if (Farm is not null
            && Farm.MapReference is { Length: >= 8 } mapRef
            && Farm.CPHH.Length >= 5
            && !await _geoLookup.IsMapReferenceWithinParishAsync(Farm.CPHH, mapRef))
        {
            ModelState.AddModelError("Farm.MapRef1",
                "Map reference does not lie within the parish boundaries for this CPHH.");
        }

        if (!ModelState.IsValid || Farm is null) return Page();

        var rowStampBase64 = TempData["FarmRowStamp"] as string;
        var rowStamp = rowStampBase64 != null ? Convert.FromBase64String(rowStampBase64) : null;

        var userId = await _currentUser.GetUserIdAsync();
        try
        {
            await _farm.UpdateAsync(Farm.ToUpdateCommand(rowStamp), userId);
        }
        catch (SqlException ex) when (ex.Number == 547 && ex.Message.Contains("CK_Farm_ADNSRegionID", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Farm.ADNSRegionID", "ADNS Region is required for GB farms.");
            TempData["FarmRowStamp"] = rowStampBase64;
            return Page();
        }

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
            : (IEnumerable<LuAdnsRegion>)[];
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

    /// <summary>AJAX GET: estimates the map reference from the centre of the CPHH parish.</summary>
    public async Task<IActionResult> OnGetEstimateMapReferenceAsync(string? cphh)
    {
        if (string.IsNullOrEmpty(cphh) || cphh.Length < 5)
            return new JsonResult(new { error = "CPHH must be at least 5 characters to estimate a map reference." });

        var estimate = await _geoLookup.EstimateMapReferenceAsync(cphh);
        if (estimate.Error is not null)
            return new JsonResult(new { error = estimate.Error });

        return new JsonResult(new { mapRef1 = estimate.MapRef1, mapRef2 = estimate.MapRef2, mapRef3 = estimate.MapRef3 });
    }

    /// <summary>AJAX GET: returns whether the supplied map reference lies within the parish boundaries.</summary>
    public async Task<IActionResult> OnGetValidateMapReferenceAsync(string? cphh, string? mapRef)
    {
        // Legacy MapReference1_MapReferenceChanged only ever touches lblInvalidMapReference once
        // both the CPHH and a full map reference are present — incomplete input leaves the
        // indicator untouched (i.e. not flagged invalid), never shows an error for it.
        if (string.IsNullOrEmpty(cphh) || cphh.Length < 5 || string.IsNullOrEmpty(mapRef) || mapRef.Length < 8)
            return new JsonResult(new { valid = true });

        var isValid = await _geoLookup.IsMapReferenceWithinParishAsync(cphh, mapRef);
        return new JsonResult(isValid
            ? new { valid = true, message = (string?)null }
            : new { valid = false, message = (string?)"Map reference does not lie within the parish boundaries for this CPHH." });
    }
}
