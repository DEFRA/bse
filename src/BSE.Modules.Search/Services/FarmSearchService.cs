using BSE.Modules.Search.Models;
using BSE.Modules.Search.Repositories;

namespace BSE.Modules.Search.Services;

public sealed class FarmSearchService : IFarmSearchService
{
    private readonly ISearchRepository _repository;

    public FarmSearchService(ISearchRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<FarmSearchResult>> SearchFarmsAsync(FarmSearchQuery query, CancellationToken ct = default)
        => _repository.SearchFarmsAsync(query, ct);
}
