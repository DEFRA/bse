using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Repositories;

namespace BSE.Modules.ReferenceData.Services;

public sealed class GeoLookupService : IGeoLookupService
{
    private readonly ILookupRepository _repo;

    public GeoLookupService(ILookupRepository repo) => _repo = repo;

    public Task<GeoMapReference?> GetMapReferenceAsync(string county, string parish)
        => _repo.GetMapReferenceByCountyParishAsync(county, parish);

    public Task<ParishLookup?> GetParishAsync(string county, string parish)
        => _repo.GetParishByCountyParishAsync(county, parish);

    public Task<IEnumerable<LuBSECounty>> GetNonGBCountyAsync()
        => _repo.GetNonGBCountiesAsync();
}
