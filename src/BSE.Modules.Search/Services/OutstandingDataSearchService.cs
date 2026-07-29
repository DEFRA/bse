using BSE.Modules.Search.Models;
using BSE.Modules.Search.Repositories;

namespace BSE.Modules.Search.Services;

public sealed class OutstandingDataSearchService : IOutstandingDataSearchService
{
    private readonly ISearchRepository _repository;

    public OutstandingDataSearchService(ISearchRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<OutstandingCaseResult>> GetOutstandingBse1sAsync(OutstandingSearchQuery query, CancellationToken ct = default)
        => _repository.GetOutstandingBse1sAsync(query, ct);

    public Task<IReadOnlyList<OutstandingCaseResult>> GetOutstandingFatesAsync(OutstandingSearchQuery query, CancellationToken ct = default)
        => _repository.GetOutstandingFatesAsync(query, ct);

    public Task<IReadOnlyList<OutstandingCaseResult>> GetOutstandingResultsAsync(OutstandingSearchQuery query, CancellationToken ct = default)
        => _repository.GetOutstandingResultsAsync(query, ct);
}
