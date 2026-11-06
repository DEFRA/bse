using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BSE.Host.Helpers;

/// <summary>Dropdown options and AJAX lookups shared by the farm add/edit/create pages.</summary>
public static class FarmFormLookups
{
    /// <summary>Loads every farm form dropdown into <paramref name="viewData"/>; authority and ADNS
    /// options are pre-filtered for the currently selected county/authority.</summary>
    public static async Task LoadOptionsAsync(
        ViewDataDictionary viewData, ILookupDataService lookups, int? authorityCountyId, int? authorityId)
    {
        var countyTask = lookups.GetLookupAsync(LookupTableId.BSECounty);
        var ahoTask = lookups.GetLookupAsync(LookupTableId.AHO);
        var herdTypeTask = lookups.GetHerdTypesAsync();
        var pedigreeTask = lookups.GetLookupAsync(LookupTableId.PedigreeType);
        var authCountyTask = lookups.GetLookupAsync(LookupTableId.AuthorityCounty);

        await Task.WhenAll(countyTask, ahoTask, herdTypeTask, pedigreeTask, authCountyTask);

        viewData["CountyOptions"] = await countyTask;
        viewData["AhoOptions"] = await ahoTask;
        viewData["HerdTypeOptions"] = await herdTypeTask;
        viewData["PedigreeOptions"] = await pedigreeTask;
        viewData["AuthorityCountyOptions"] = await authCountyTask;

        viewData["AuthorityOptions"] = authorityCountyId is > 0
            ? await lookups.GetAuthoritiesByCountyAsync(authorityCountyId.Value)
            : (IEnumerable<LuAuthority>)[];

        viewData["AdnsOptions"] = authorityId is > 0
            ? await lookups.GetADNSRegionsByAuthorityAsync(authorityId.Value)
            : (IEnumerable<LuAdnsRegion>)[];
    }

    /// <summary>AJAX handler body: authorities for a given authority county.</summary>
    public static async Task<IActionResult> AuthoritiesAsync(ILookupDataService lookups, int? authorityCountyId)
    {
        if (authorityCountyId is null or 0) return new JsonResult(Array.Empty<object>());
        var items = await lookups.GetAuthoritiesByCountyAsync(authorityCountyId.Value);
        return new JsonResult(items.Select(a => new { id = a.Id, name = a.Name }));
    }

    /// <summary>AJAX handler body: ADNS regions for a given local authority.</summary>
    public static async Task<IActionResult> AdnsRegionsAsync(ILookupDataService lookups, int? authorityId)
    {
        if (authorityId is null or 0) return new JsonResult(Array.Empty<object>());
        var items = await lookups.GetADNSRegionsByAuthorityAsync(authorityId.Value);
        return new JsonResult(items.Select(r => new { id = r.Id, name = r.Name }));
    }

    /// <summary>AJAX handler body: estimates the map reference from the parish centre for the given CPHH.</summary>
    public static async Task<IActionResult> EstimateMapReferenceAsync(BSE.Host.Services.IGeoLookupService geoLookup, string? cphh)
    {
        if (string.IsNullOrEmpty(cphh) || cphh.Length < 5)
            return new JsonResult(new { error = "CPHH must be at least 5 characters to estimate a map reference." });

        var estimate = await geoLookup.EstimateMapReferenceAsync(cphh);
        if (estimate.Error is not null)
            return new JsonResult(new { error = estimate.Error });

        return new JsonResult(new { mapRef1 = estimate.MapRef1, mapRef2 = estimate.MapRef2, mapRef3 = estimate.MapRef3 });
    }

    /// <summary>AJAX handler body: validates a map reference against the parish, normalising the inputs first.</summary>
    public static async Task<IActionResult> ValidateMapReferenceAsync(
        BSE.Host.Services.IGeoLookupService geoLookup, string? cphh, string? mapRef)
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
}
