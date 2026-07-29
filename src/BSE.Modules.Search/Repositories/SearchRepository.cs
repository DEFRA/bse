using BSE.Infrastructure;
using BSE.Modules.Search.Models;

namespace BSE.Modules.Search.Repositories;

/// <summary>
/// Dapper-backed repository for all Search stored procedure calls.
/// SP names match filenames in src/BSE.Database/StoredProcedures/Search/ exactly.
/// SP parameter names match @-parameter names in each .sql file exactly.
/// GetSearchCase and GetSearchFarm use a 60-second command timeout as per Migration Plan Slice 6.
/// </summary>
public sealed class SearchRepository : DapperRepository, ISearchRepository
{
    private const int SearchCommandTimeoutSeconds = 60;

    public SearchRepository(IDbConnectionFactory connectionFactory)
        : base(connectionFactory) { }

    public async Task<IReadOnlyList<CaseSearchResult>> SearchCasesAsync(CaseSearchQuery q, CancellationToken ct = default)
    {
        var result = await QueryAsync<CaseSearchResult>("GetSearchCase", new
        {
            RBSE = q.Rbse,
            Eartag = q.Eartag,
            DBSE = q.Dbse,
            Fate = q.Fate,
            FinalResult = q.FinalResult,
            Sex = q.Sex,
            Survey = q.Survey,
            Notes = q.Notes,
            EarliestFormADate = q.EarliestFormADate,
            LatestFormADate = q.LatestFormADate,
            EarliestFinalResultDate = q.EarliestFinalResultDate,
            LatestFinalResultDate = q.LatestFinalResultDate,
            EarliestBirthDate = q.EarliestBirthDate,
            LatestBirthDate = q.LatestBirthDate,
            IncludeNonGBCases = q.IncludeNonGbCases,
            PassiveActive = q.PassiveActive,
            IsImportedCase = q.IsImportedCase
        }, SearchCommandTimeoutSeconds);
        return result.ToList();
    }

    public async Task<IReadOnlyList<FarmSearchResult>> SearchFarmsAsync(FarmSearchQuery q, CancellationToken ct = default)
    {
        var result = await QueryAsync<FarmSearchResult>("GetSearchFarm", new
        {
            CPHH = q.Cphh,
            OwnerName = q.OwnerName,
            Address = q.Address,
            County = q.County,
            Herdmark = q.Herdmark,
            NumericHerdmark = q.NumericHerdmark,
            IsDealer = q.IsDealer,
            AHO = q.Aho,
            IncludeNonGBFarms = q.IncludeNonGbFarms
        }, SearchCommandTimeoutSeconds);
        return result.ToList();
    }

    public async Task<IReadOnlyList<CaseDetailSearchResult>> GetCasesByCphhAsync(
        string cphh, string herdmark, string numericHerdmark, bool includeNonGb, CancellationToken ct = default)
    {
        var result = await QueryAsync<CaseDetailSearchResult>("GetSearchCaseByCPHH", new
        {
            CPHH = cphh,
            Herdmark = herdmark,
            NumericHerdmark = numericHerdmark,
            IncludeNonGBCases = includeNonGb
        });
        return result.ToList();
    }

    public async Task<IReadOnlyList<CaseDetailSearchResult>> GetCasesByEartagHerdmarkAsync(
        string eartagHerdmark, bool includeNonGb, CancellationToken ct = default)
    {
        var result = await QueryAsync<CaseDetailSearchResult>("GetSearchCaseByEartagHerdmark", new
        {
            EartagHerdmark = eartagHerdmark,
            IncludeNonGBCases = includeNonGb
        });
        return result.ToList();
    }

    public async Task<IReadOnlyList<OutstandingCaseResult>> GetOutstandingBse1sAsync(
        OutstandingSearchQuery q, CancellationToken ct = default)
    {
        var result = await QueryAsync<OutstandingCaseResult>("GetSearchOutstandingBSE1s", new
        {
            EarliestFormADate = q.EarliestFormADate,
            LatestFormADate = q.LatestFormADate,
            IncludeNonGBCases = q.IncludeNonGbCases
        });
        return result.ToList();
    }

    public async Task<IReadOnlyList<OutstandingCaseResult>> GetOutstandingFatesAsync(
        OutstandingSearchQuery q, CancellationToken ct = default)
    {
        var result = await QueryAsync<OutstandingCaseResult>("GetSearchOutstandingFates", new
        {
            EarliestFormADate = q.EarliestFormADate,
            LatestFormADate = q.LatestFormADate,
            IncludeNonGBCases = q.IncludeNonGbCases
        });
        return result.ToList();
    }

    public async Task<IReadOnlyList<OutstandingCaseResult>> GetOutstandingResultsAsync(
        OutstandingSearchQuery q, CancellationToken ct = default)
    {
        var result = await QueryAsync<OutstandingCaseResult>("GetSearchOutstandingResults", new
        {
            EarliestFormADate = q.EarliestFormADate,
            LatestFormADate = q.LatestFormADate,
            IncludeNonGBCases = q.IncludeNonGbCases
        });
        return result.ToList();
    }

    public async Task<IReadOnlyList<RelatedAnimalResult>> GetRelatedAnimalsAsync(
        string rbse, string name, string eartag, string relationRbse, string relationType, CancellationToken ct = default)
    {
        var result = await QueryAsync<RelatedAnimalResult>("GetSearchRelatedAnimals", new
        {
            RBSE = rbse,
            Name = name,
            Eartag = eartag,
            RelationRBSE = relationRbse,
            RelationType = relationType
        });
        return result.ToList();
    }
}
