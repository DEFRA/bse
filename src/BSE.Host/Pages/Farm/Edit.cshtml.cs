using BSE.Host.Models.ViewModels;
using BSE.Host.Services;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using IGeoLookupService = BSE.Host.Services.IGeoLookupService;
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

        // Validate map reference is within the parish for the CPHH (mirrors legacy MapReference1_MapReferenceChanged)
        if (Farm is not null
            && Farm.MapReference is { Length: >= 8 } mapRef
            && Farm.CPHH.Length >= 5)
        {
            if (!await MapReferenceWithinParishAsync(Farm.CPHH, mapRef))
                ModelState.AddModelError("Farm.MapRef1",
                    "Map reference does not lie within the parish boundaries for this CPHH.");
        }

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

    /// <summary>AJAX GET: estimates the map reference from the centre of the CPHH parish.</summary>
    public async Task<IActionResult> OnGetEstimateMapReferenceAsync(string? cphh)
    {
        if (string.IsNullOrEmpty(cphh) || cphh.Length < 5)
            return new JsonResult(new { error = "CPHH must be at least 5 characters to estimate a map reference." });

        var county = cphh[..2];
        var parish = cphh[2..5];

        var rows = await _geoLookup.GetAllParishMapReferencesAsync(county, parish);
        if (rows.Count == 0)
            return new JsonResult(new { error = "No map reference data found for the parish associated with this CPHH." });

        // Find middle row — mirrors clsCase.EstimateMapReference (VB): odd row count rounds up
        double rowCount = rows.Count;
        double middleIdx = rowCount % 2 != 0 ? rowCount / 2 + 0.5 : rowCount / 2;
        middleIdx -= 1; // make 0-based
        var row = rows[(int)middleIdx];

        var xCoord = row.XReference1;                               // 4-digit string e.g. "0345"
        var centreY = CentreCoordinate(row.YReference1, row.YReference2); // 4-digit string

        // Convert coords → OS grid prefix (mirrors clsCase.ConvertToMapReference)
        var xPrefixCoord = xCoord[1].ToString();                    // VB: Mid$(xCoord, 2, 1)
        var yPrefixRaw   = centreY[..2];                            // VB: Mid$(yCoord, 1, 2)
        var yPrefixCoord = yPrefixRaw[0] == '0' ? yPrefixRaw[1].ToString() : yPrefixRaw;

        var code = await _geoLookup.GetPrefixCodeAsync(xPrefixCoord, yPrefixCoord);
        if (code is null)
            return new JsonResult(new { error = "Could not determine OS grid square for this parish." });

        // VB: sCode & Mid$(xCoord,3,2) & "5" & Mid$(yCoord,3,2) & "5"
        return new JsonResult(new
        {
            mapRef1 = code,
            mapRef2 = xCoord[2..4] + "5",
            mapRef3 = centreY[2..4] + "5"
        });
    }

    /// <summary>AJAX GET: returns whether the supplied map reference lies within the parish boundaries.</summary>
    public async Task<IActionResult> OnGetValidateMapReferenceAsync(string? cphh, string? mapRef)
    {
        if (string.IsNullOrEmpty(cphh) || cphh.Length < 5
            || string.IsNullOrEmpty(mapRef) || mapRef.Length < 8)
            return new JsonResult(new { valid = false, message = "Map reference must be 8 characters (e.g. HP345125)." });

        var isValid = await MapReferenceWithinParishAsync(cphh, mapRef);
        return new JsonResult(isValid
            ? new { valid = true, message = (string?)null }
            : new { valid = false, message = "Map reference does not lie within the parish boundaries for this CPHH." });
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    // Mirrors clsCase.ValidateMapReference
    private async Task<bool> MapReferenceWithinParishAsync(string cphh, string mapRef)
    {
        var county = cphh[..2];
        var parish = cphh[2..5];

        var prefixData = await _geoLookup.GetXYCoordsByPrefixCodeAsync(mapRef[..2].ToUpperInvariant());
        if (prefixData is null) return false;

        var rows = await _geoLookup.GetAllParishMapReferencesAsync(county, parish);
        if (rows.Count == 0) return true; // no boundary data — allow

        // VB: sXCoord & Mid$(mapRef,3,2); sYCoord & Mid$(mapRef,6,2)  (1-indexed)
        var sXCoord = prefixData.XCoordPrefix + mapRef[2..4];
        var sYCoord = prefixData.YCoordPrefix + mapRef[5..7];

        return rows.Any(r =>
            sXCoord == r.XReference1
            && string.Compare(sYCoord, r.YReference1, StringComparison.Ordinal) >= 0
            && string.Compare(sYCoord, r.YReference2, StringComparison.Ordinal) <= 0);
    }

    // Mirrors clsCase.GetCentreCoordinate
    private static string CentreCoordinate(string y1, string y2)
    {
        int iY1 = int.Parse(y1);
        int iY2 = int.Parse(y2);
        int output = (iY2 - iY1) % 2 != 0
            ? (int)(iY1 + (iY2 - iY1) / 2.0 + 0.5)
            : iY1 + (iY2 - iY1) / 2;
        return output.ToString("D4");
    }
}
