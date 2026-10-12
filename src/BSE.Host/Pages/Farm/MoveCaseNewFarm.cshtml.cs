using BSE.Host.Models.ViewModels;
using BSE.Host.Services;
using BSE.Host.Helpers;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IGeoLookupService = BSE.Host.Services.IGeoLookupService;

namespace BSE.Host.Pages.Farm;

[Authorize(Policy = "DEFRAOrVLAMaintenance")]
public class MoveCaseNewFarmModel(ICaseService caseService, IFarmService farmService, ICurrentUserService currentUserService, ILookupDataService lookups, IGeoLookupService geoLookup) : PageModel
{
    private const string FarmAlreadyExistsMessage = "A farm with this CPHH already exists.";
    private const string InvalidCphhMessage = "Enter CPHH as 11 digits in the format NN/NNN/NNNN/NN.";
    private const string SaveFailedMessage = "Unable to save farm details. Please check the entered values and try again.";

    [BindProperty]
    public FarmEditViewModel Farm { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnRbse { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnCphh { get; set; }

    public string DisplayRbse => BseFormat.FormatRbse(RbseHelper.ParseToRaw(ReturnRbse));

    public async Task<IActionResult> OnGetAsync(string? cphh = null)
    {
        await PrepopulateFromExistingCaseFarmAsync();
        Farm.CPHH = CphhNormalizer.Normalize(ReturnCphh ?? cphh);
        await LoadLookupsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Farm.CPHH = CphhNormalizer.Normalize(Farm.CPHH);

        if (string.IsNullOrWhiteSpace(Farm.CPHH) || Farm.CPHH.Length != 11)
            ModelState.AddModelError("Farm.CPHH", InvalidCphhMessage);

        // Legacy MoveCaseNewFarm.aspx.vb CheckMandatoryFields — OwnerName, Address1, AHO, Parish,
        // County and ADNSRegionID, previously not validated here at all.
        if (string.IsNullOrWhiteSpace(Farm.OwnerName))
            ModelState.AddModelError("Farm.OwnerName", "Enter an owner name for the farm.");
        if (string.IsNullOrWhiteSpace(Farm.Address1))
            ModelState.AddModelError("Farm.Address1", "Enter the first line of the farm address.");
        if (string.IsNullOrWhiteSpace(Farm.AHO))
            ModelState.AddModelError("Farm.AHO", "Specify an AHO for the farm.");
        if (string.IsNullOrWhiteSpace(Farm.Parish))
            ModelState.AddModelError("Farm.Parish", "Enter a parish for the farm.");
        if (string.IsNullOrWhiteSpace(Farm.County))
            ModelState.AddModelError("Farm.County", "Specify a county for the farm.");
        if (Farm.ADNSRegionID is null)
            ModelState.AddModelError("Farm.ADNSRegionID", "Specify an ADNS region for the farm.");

        await LoadLookupsAsync();

        // Legacy parity: PopulateControls() copies AuthorityCountyID/AuthorityID/ADNSRegionID
        // straight from the old farm, then rebuilds the Local Authority/ADNS Region dropdowns from
        // that AuthorityCountyID/AuthorityID and re-selects the copied value only if it's still
        // present in the freshly-rebuilt list — if the old farm's ADNS Region is no longer valid for
        // its Authority, the dropdown silently resets to blank and CheckMandatoryFields then rejects
        // the save with "You have not entered an ADNS Region." Reproduce that same rejection here
        // (never ported to this page) instead of silently moving the case with a now-stale ADNS Region.
        if (Farm.AuthorityID is > 0 && Farm.ADNSRegionID is > 0
            && ViewData["AdnsOptions"] is IEnumerable<LuADNSRegion> adnsOptions
            && !adnsOptions.Any(x => x.Id == Farm.ADNSRegionID.Value))
        {
            ModelState.AddModelError("Farm.ADNSRegionID", "Specify an ADNS region for the farm.");
        }

        if (!ModelState.IsValid)
            return Page();

        var existingFarm = await farmService.GetByCphhAsync(Farm.CPHH);
        if (existingFarm is not null)
        {
            ModelState.AddModelError("Farm.CPHH", FarmAlreadyExistsMessage);
            return Page();
        }

        try
        {
            var userId = await currentUserService.GetUserIdAsync();
            // Legacy parity: MoveCaseNewFarm.aspx has no HerdType/Pedigree/IsDealer controls at all —
            // its SP call always hardcodes HerdType=NULL, PedigreeType=NULL, IsDealer=False. Enforced
            // here too (not just by hiding the fields in the view) in case of a crafted POST.
            var command = Farm.ToAddCommand() with { HerdType = null, PedigreeType = null, IsDealer = false };
            await farmService.AddAsync(command, userId);
        }
        catch
        {
            ModelState.AddModelError(string.Empty, SaveFailedMessage);
            return Page();
        }

        TempData[MaintenanceConfirmationModel.TitleKey] = "New farm created";
        TempData[MaintenanceConfirmationModel.SummaryKey] = $"Farm {BSE.Host.Helpers.BseFormat.FormatCphh(Farm.CPHH)} was created.";

        if (!string.IsNullOrEmpty(ReturnRbse))
            return RedirectToPage("/Case/MoveCase", new { rbse = ReturnRbse, newCphh = Farm.CPHH });

        return RedirectToPage("/Farm/Details", new { cphh = Farm.CPHH });
    }

    private async Task LoadLookupsAsync()
    {
        var countyTask = lookups.GetLookupAsync(LookupTableId.BSECounty);
        var ahoTask = lookups.GetLookupAsync(LookupTableId.AHO);
        var herdTypeTask = lookups.GetHerdTypesAsync();
        var pedigreeTask = lookups.GetLookupAsync(LookupTableId.PedigreeType);
        var authCountyTask = lookups.GetLookupAsync(LookupTableId.AuthorityCounty);

        await Task.WhenAll(countyTask, ahoTask, herdTypeTask, pedigreeTask, authCountyTask);

        ViewData["CountyOptions"] = await countyTask;
        ViewData["AhoOptions"] = await ahoTask;
        ViewData["HerdTypeOptions"] = await herdTypeTask;
        ViewData["PedigreeOptions"] = await pedigreeTask;
        ViewData["AuthorityCountyOptions"] = await authCountyTask;

        ViewData["AuthorityOptions"] = Farm.AuthorityCountyID is > 0
            ? await lookups.GetAuthoritiesByCountyAsync(Farm.AuthorityCountyID.Value)
            : (IEnumerable<LuAuthority>)[];

        ViewData["AdnsOptions"] = Farm.AuthorityID is > 0
            ? await lookups.GetADNSRegionsByAuthorityAsync(Farm.AuthorityID.Value)
            : (IEnumerable<LuADNSRegion>)[];
    }

    public async Task<IActionResult> OnGetAuthoritiesAsync(int? authorityCountyId)
    {
        if (authorityCountyId is null or 0) return new JsonResult(Array.Empty<object>());
        var items = await lookups.GetAuthoritiesByCountyAsync(authorityCountyId.Value);
        return new JsonResult(items.Select(a => new { id = a.Id, name = a.Name }));
    }

    public async Task<IActionResult> OnGetAdnsRegionsAsync(int? authorityId)
    {
        if (authorityId is null or 0) return new JsonResult(Array.Empty<object>());
        var items = await lookups.GetADNSRegionsByAuthorityAsync(authorityId.Value);
        return new JsonResult(items.Select(r => new { id = r.Id, name = r.Name }));
    }

    public async Task<IActionResult> OnGetEstimateMapReferenceAsync(string? cphh)
    {
        cphh = CphhNormalizer.Normalize(cphh);
        if (string.IsNullOrEmpty(cphh) || cphh.Length < 5)
            return new JsonResult(new { error = "CPHH must be at least 5 characters to estimate a map reference." });

        var estimate = await geoLookup.EstimateMapReferenceAsync(cphh);
        if (estimate.Error is not null)
            return new JsonResult(new { error = estimate.Error });

        return new JsonResult(new { mapRef1 = estimate.MapRef1, mapRef2 = estimate.MapRef2, mapRef3 = estimate.MapRef3 });
    }

    public async Task<IActionResult> OnGetValidateMapReferenceAsync(string? cphh, string? mapRef)
    {
        cphh = CphhNormalizer.Normalize(cphh);
        mapRef = (mapRef ?? "").Trim().ToUpperInvariant();
        if (cphh.Length < 5 || mapRef.Length < 8)
            return new JsonResult(new { valid = true });

        var valid = await geoLookup.IsMapReferenceWithinParishAsync(cphh, mapRef);
        return valid
            ? new JsonResult(new { valid = true })
            : new JsonResult(new { valid = false, message = "Map reference does not lie within the parish boundaries for this CPHH." });
    }

    private async Task PrepopulateFromExistingCaseFarmAsync()
    {
        var rbse = RbseHelper.ParseToRaw(ReturnRbse);
        if (rbse.Length != 9)
            return;

        var record = await caseService.GetCaseAsync(rbse);
        if (record is null || string.IsNullOrWhiteSpace(record.Cphh))
            return;

        var currentFarm = await farmService.GetByCphhAsync(record.Cphh);
        if (currentFarm is null)
            return;

        Farm = new FarmEditViewModel
        {
            // Legacy MoveCaseNewFarm pre-populates core farm identity/location fields.
            OwnerName = currentFarm.OwnerName,
            Address1 = currentFarm.Address1,
            Address2 = currentFarm.Address2,
            Address3 = currentFarm.Address3,
            Postcode = currentFarm.Postcode,
            Parish = currentFarm.Parish,
            District = currentFarm.District,
            County = currentFarm.County,
            AHO = currentFarm.AHO,
            AuthorityCountyID = currentFarm.AuthorityCountyID,
            AuthorityID = currentFarm.AuthorityID,
            ADNSRegionID = currentFarm.ADNSRegionID
        };
    }
}
