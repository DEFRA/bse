using BSE.Host.Models.ViewModels;
using BSE.Host.Services;
using BSE.Modules.FarmManagement.Models;
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
    private const string VetnetNotFoundMessage = "The CPHH you entered was not Found on Vetnet";
    private const string BrusselsWarningMessage = "Local authority not on file – case won’t be notifiable to Brussels.  Please investigate.";
    private const string FarmAlreadyExistsMessage = "A farm with this CPHH already exists.";
    private const string InvalidCphhMessage = "Enter CPHH as 11 digits in the format NN/NNN/NNNN/NN.";
    private const string SaveFailedMessage = "Unable to save farm details. Please check the entered values and try again.";

    [BindProperty]
    public FarmEditViewModel Farm { get; set; } = new();

    /// <summary>When set, redirect back to Case/Farm after farm creation.</summary>
    [BindProperty(SupportsGet = true)]
    public string? ReturnRbse { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnCphh { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool ReturnToCaseFarm { get; set; }

    public string? VetnetMessage { get; private set; }

    [BindProperty]
    public bool AcceptVetnetDetails { get; set; }

    public string? VetnetHerdmark { get; private set; }
    public string? VetnetNumericHerdmark { get; private set; }
    public bool VetnetFound => !string.IsNullOrWhiteSpace(VetnetHerdmark) && !string.IsNullOrWhiteSpace(VetnetNumericHerdmark);
    public bool ShowBrusselsWarning { get; private set; }
    public string BrusselsWarningText => BrusselsWarningMessage;

    private string? DerivedParish { get; set; }
    private string? DerivedCounty { get; set; }
    private int? DerivedAdnsRegionId { get; set; }
    private int? DerivedAuthorityId { get; set; }
    private int? DerivedAuthorityCountyId { get; set; }

    public async Task<IActionResult> OnGetAsync(string? cphh = null)
    {
        Farm.CPHH = CphhNormalizer.Normalize(ReturnCphh ?? cphh);
        await ApplyPickFarmVetnetDefaultsAsync();
        await LoadLookupsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var isPickFarmJourney = ReturnToCaseFarm || !string.IsNullOrWhiteSpace(ReturnRbse);

        if (isPickFarmJourney)
            Farm.CPHH = CphhNormalizer.Normalize(ReturnCphh ?? Farm.CPHH);

        Farm.CPHH = CphhNormalizer.Normalize(Farm.CPHH);
        await ApplyPickFarmVetnetDefaultsAsync();

        if (string.IsNullOrWhiteSpace(Farm.CPHH) || Farm.CPHH.Length != 11)
            ModelState.AddModelError("Farm.CPHH", InvalidCphhMessage);

        await LoadLookupsAsync();
        if (!ModelState.IsValid)
            return Page();

        var existingFarm = await farmService.GetByCphhAsync(Farm.CPHH);

        // Legacy PickFarm -> NewFarm flow did not persist a new farm row directly from this
        // confirmation screen. "Create Farm" returns to CaseEntryFarm with seeded defaults.
        if (isPickFarmJourney)
        {
            if (string.IsNullOrWhiteSpace(ReturnRbse))
                return RedirectToPage("/Home");

            if (existingFarm is not null)
                return RedirectToPage("/Case/Farm", new { rbse = ReturnRbse, selectedCphh = Farm.CPHH });

            return RedirectToPage("/Case/Farm", new
            {
                rbse = ReturnRbse,
                newCphh = Farm.CPHH,
                forceNewFarmDetails = true,
                seedParish = DerivedParish,
                seedCounty = DerivedCounty,
                seedAdnsRegionId = DerivedAdnsRegionId,
                seedAuthorityId = DerivedAuthorityId,
                seedAuthorityCountyId = DerivedAuthorityCountyId,
                seedHerdmark1 = AcceptVetnetDetails && VetnetFound ? VetnetHerdmark : null,
                seedNumericHerdmark1 = AcceptVetnetDetails && VetnetFound ? VetnetNumericHerdmark : null
            });
        }

        if (existingFarm is not null)
        {
            ModelState.AddModelError("Farm.CPHH", FarmAlreadyExistsMessage);
            return Page();
        }

        try
        {
            var userId = await currentUserService.GetUserIdAsync();
            var command = Farm.ToAddCommand();
            await farmService.AddAsync(command, userId);
        }
        catch
        {
            ModelState.AddModelError(string.Empty, SaveFailedMessage);
            return Page();
        }

        TempData["Success"] = $"Farm {Farm.CPHH} has been created.";

        if (!string.IsNullOrEmpty(ReturnRbse) && ReturnToCaseFarm)
            return RedirectToPage("/Case/Farm", new { rbse = ReturnRbse, selectedCphh = Farm.CPHH });

        if (!string.IsNullOrEmpty(ReturnRbse))
            return RedirectToPage("/Case/Farm", new { rbse = ReturnRbse, newCphh = ReturnCphh ?? Farm.CPHH });

        return RedirectToPage("/Farm/Details", new { cphh = Farm.CPHH });
    }

    private async Task ApplyPickFarmVetnetDefaultsAsync()
    {
        VetnetMessage = null;
        VetnetHerdmark = null;
        VetnetNumericHerdmark = null;
        ShowBrusselsWarning = false;
        DerivedParish = null;
        DerivedCounty = null;
        DerivedAdnsRegionId = null;
        DerivedAuthorityId = null;
        DerivedAuthorityCountyId = null;

        if (!ReturnToCaseFarm)
            return;

        if (string.IsNullOrWhiteSpace(Farm.CPHH))
        {
            VetnetMessage = VetnetNotFoundMessage;
            return;
        }

        var vetnet = (await farmService.GetVetnetDetailsAsync(Farm.CPHH)).ToList();
        var preferred = vetnet.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v.Herdmark) || !string.IsNullOrWhiteSpace(v.NumericHerdmark))
                        ?? vetnet.FirstOrDefault();

        if (preferred is null)
        {
            VetnetMessage = VetnetNotFoundMessage;
        }
        else
        {
            VetnetHerdmark = preferred.Herdmark;
            VetnetNumericHerdmark = preferred.NumericHerdmark;
        }

        if (Farm.CPHH.Length >= 5)
        {
            var county = Farm.CPHH[..2];
            var parish = Farm.CPHH[2..5];
            var parishData = await geoLookup.GetParishAsync(county, parish);

            if (parishData is not null)
            {
                DerivedParish = parishData.Name;
                DerivedCounty = parishData.BSECounty;
                DerivedAdnsRegionId = parishData.ADNSRegionID;
                DerivedAuthorityId = parishData.AuthorityID;
                DerivedAuthorityCountyId = parishData.AuthorityCountyID;
            }

            ShowBrusselsWarning = !DerivedAdnsRegionId.HasValue;
        }
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
        cphh = CphhNormalizer.Normalize(cphh);
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

        return new JsonResult(new
        {
            mapRef1 = mapRef[..2],
            mapRef2 = mapRef[2..5],
            mapRef3 = mapRef[5..8]
        });
    }
}
