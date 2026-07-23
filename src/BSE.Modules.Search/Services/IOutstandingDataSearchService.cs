using BSE.Modules.Search.Models;

namespace BSE.Modules.Search.Services;

public interface IOutstandingDataSearchService
{
    Task<IReadOnlyList<OutstandingCaseResult>> GetOutstandingBse1sAsync(OutstandingSearchQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<OutstandingCaseResult>> GetOutstandingFatesAsync(OutstandingSearchQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<OutstandingCaseResult>> GetOutstandingResultsAsync(OutstandingSearchQuery query, CancellationToken ct = default);
}
