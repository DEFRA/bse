using BSE.Host.Helpers;
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

    private Task LoadLookupsAsync() =>
        // Load authority and ADNS options pre-filtered for the currently selected values
        FarmFormLookups.LoadOptionsAsync(ViewData, _lookups, Farm?.AuthorityCountyID, Farm?.AuthorityID);

    /// <summary>AJAX handler: returns authorities for a given authority county.</summary>
    public Task<IActionResult> OnGetAuthoritiesAsync(int? authorityCountyId) =>
        FarmFormLookups.AuthoritiesAsync(_lookups, authorityCountyId);

    /// <summary>AJAX handler: returns ADNS regions for a given local authority.</summary>
    public Task<IActionResult> OnGetAdnsRegionsAsync(int? authorityId) =>
        FarmFormLookups.AdnsRegionsAsync(_lookups, authorityId);

    /// <summary>AJAX GET: estimates the map reference from the centre of the CPHH parish.</summary>
    public Task<IActionResult> OnGetEstimateMapReferenceAsync(string? cphh) =>
        FarmFormLookups.EstimateMapReferenceAsync(_geoLookup, cphh);

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
