using BSE.Modules.Search.Models;
using BSE.Modules.Search.Repositories;

namespace BSE.Modules.Search.Services;

public sealed class CaseSearchService : ICaseSearchService
{
    private readonly ISearchRepository _repository;

    public CaseSearchService(ISearchRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<CaseSearchResult>> SearchCasesAsync(CaseSearchQuery query, CancellationToken ct = default)
        => _repository.SearchCasesAsync(query, ct);

    public Task<IReadOnlyList<CaseDetailSearchResult>> GetCasesByCphhAsync(
        string cphh, string herdmark, string numericHerdmark, bool includeNonGb, CancellationToken ct = default)
        => _repository.GetCasesByCphhAsync(cphh, herdmark, numericHerdmark, includeNonGb, ct);

    public Task<IReadOnlyList<CaseDetailSearchResult>> GetCasesByEartagHerdmarkAsync(
        string eartagHerdmark, bool includeNonGb, CancellationToken ct = default)
        => _repository.GetCasesByEartagHerdmarkAsync(eartagHerdmark, includeNonGb, ct);

    public Task<IReadOnlyList<RelatedAnimalResult>> GetRelatedAnimalsAsync(
        string rbse, string name, string eartag, string relationRbse, string relationType, CancellationToken ct = default)
        => _repository.GetRelatedAnimalsAsync(rbse, name, eartag, relationRbse, relationType, ct);
}
