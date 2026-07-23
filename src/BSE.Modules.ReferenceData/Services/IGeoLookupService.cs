using BSE.Modules.ReferenceData.Models;

namespace BSE.Modules.ReferenceData.Services;

public interface IGeoLookupService
{
    Task<GeoMapReference?> GetMapReferenceAsync(string county, string parish);
    Task<ParishLookup?> GetParishAsync(string county, string parish);
    Task<IEnumerable<LuBSECounty>> GetNonGBCountyAsync();
}
