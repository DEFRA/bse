using BSE.Modules.Search.Models;

namespace BSE.Modules.Search.Services;

public interface IFarmSearchService
{
    Task<IReadOnlyList<FarmSearchResult>> SearchFarmsAsync(FarmSearchQuery query, CancellationToken ct = default);
}
