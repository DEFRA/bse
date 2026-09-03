using BSE.Modules.Search.Models;

namespace BSE.Modules.Search.Repositories;

public interface ISearchRepository
{
    Task<IReadOnlyList<CaseSearchResult>> SearchCasesAsync(CaseSearchQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<FarmSearchResult>> SearchFarmsAsync(FarmSearchQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<CaseDetailSearchResult>> GetCasesByCphhAsync(string cphh, string herdmark, string numericHerdmark, bool includeNonGb, CancellationToken ct = default);
    Task<IReadOnlyList<CaseDetailSearchResult>> GetCasesByEartagHerdmarkAsync(string eartagHerdmark, bool includeNonGb, CancellationToken ct = default);
    Task<IReadOnlyList<OutstandingCaseResult>> GetOutstandingBse1sAsync(OutstandingSearchQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<OutstandingCaseResult>> GetOutstandingFatesAsync(OutstandingSearchQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<OutstandingCaseResult>> GetOutstandingResultsAsync(OutstandingSearchQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<RelatedAnimalResult>> GetRelatedAnimalsAsync(string rbse, string name, string eartag, string relationRbse, string relationType, CancellationToken ct = default);
}
