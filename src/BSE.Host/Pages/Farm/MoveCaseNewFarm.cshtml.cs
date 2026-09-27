using BSE.Host.Models.ViewModels;
using BSE.Host.Services;
using BSE.Host.Helpers;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using IGeoLookupService = BSE.Modules.ReferenceData.Services.IGeoLookupService;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Farm;

[Authorize(Policy = "DEFRAMaintenance")]
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

        await LoadLookupsAsync();
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
            var command = Farm.ToAddCommand();
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
