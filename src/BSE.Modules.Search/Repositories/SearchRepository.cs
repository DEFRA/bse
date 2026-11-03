using System.Data;
using BSE.Infrastructure;
using BSE.Modules.Search.Models;
using Dapper;

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

    public async Task<IReadOnlyList<CaseSearchResult>> SearchCasesAsync(CaseSearchQuery query, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("RBSE",                    query.Rbse         ?? "", DbType.AnsiString,  size: 9);
        p.Add("Eartag",                  query.Eartag       ?? "", DbType.AnsiString,  size: 35);
        p.Add("DBSE",                    query.Dbse         ?? "", DbType.AnsiString,  size: 7);
        p.Add("Fate",                    query.Fate         ?? "", DbType.AnsiString,  size: 4);
        p.Add("FinalResult",             query.FinalResult  ?? "", DbType.AnsiString,  size: 5);
        p.Add("Sex",                     query.Sex          ?? "", DbType.AnsiString,  size: 1);
        p.Add("Survey",                  query.Survey       ?? "", DbType.AnsiString,  size: 4);
        p.Add("Notes",                   query.Notes        ?? "", DbType.AnsiString,  size: 500);
        p.Add("PassiveActive",           query.PassiveActive ?? "", DbType.AnsiString,  size: 1);
        p.Add("EarliestFormADate",       (object?)query.EarliestFormADate       ?? DBNull.Value, DbType.DateTime);
        p.Add("LatestFormADate",         (object?)query.LatestFormADate         ?? DBNull.Value, DbType.DateTime);
        p.Add("EarliestFinalResultDate", (object?)query.EarliestFinalResultDate ?? DBNull.Value, DbType.DateTime);
        p.Add("LatestFinalResultDate",   (object?)query.LatestFinalResultDate   ?? DBNull.Value, DbType.DateTime);
        p.Add("EarliestBirthDate",       (object?)query.EarliestBirthDate       ?? DBNull.Value, DbType.DateTime);
        p.Add("LatestBirthDate",         (object?)query.LatestBirthDate         ?? DBNull.Value, DbType.DateTime);
        p.Add("IncludeNonGBCases",       query.IncludeNonGbCases,         DbType.Boolean);
        p.Add("IsImportedCase",          query.IsImportedCase,            DbType.Boolean);

        var result = await QueryAsync<CaseSearchResult>("GetSearchCase", p, SearchCommandTimeoutSeconds);
        return result.ToList();
    }

    public async Task<IReadOnlyList<FarmSearchResult>> SearchFarmsAsync(FarmSearchQuery query, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("CPHH",             query.Cphh             ?? string.Empty, DbType.AnsiString, size: 11);
        p.Add("OwnerName",        query.OwnerName        ?? string.Empty, DbType.AnsiString, size: 100);
        p.Add("Address",          query.Address          ?? string.Empty, DbType.AnsiString, size: 160);
        p.Add("County",           query.County           ?? string.Empty, DbType.AnsiString, size: 15);
        p.Add("Herdmark",         query.Herdmark         ?? string.Empty, DbType.AnsiString, size: 8);
        p.Add("NumericHerdmark",  query.NumericHerdmark  ?? string.Empty, DbType.AnsiString, size: 6);
        p.Add("IsDealer",         query.IsDealer,                                DbType.Boolean);
        p.Add("AHO",              query.Aho              ?? string.Empty, DbType.AnsiString, size: 2);
        p.Add("IncludeNonGBFarms", query.IncludeNonGbFarms,                      DbType.Boolean);

        var result = await QueryAsync<FarmSearchResult>("GetSearchFarm", p, SearchCommandTimeoutSeconds);
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
        OutstandingSearchQuery query, CancellationToken ct = default)
    {
        var result = await QueryAsync<OutstandingCaseResult>("GetSearchOutstandingBSE1s", new
        {
            EarliestFormADate = query.EarliestFormADate,
            LatestFormADate = query.LatestFormADate,
            IncludeNonGBCases = query.IncludeNonGbCases
        });
        return result.ToList();
    }

    public async Task<IReadOnlyList<OutstandingCaseResult>> GetOutstandingFatesAsync(
        OutstandingSearchQuery query, CancellationToken ct = default)
    {
        var result = await QueryAsync<OutstandingCaseResult>("GetSearchOutstandingFates", new
        {
            EarliestFormADate = query.EarliestFormADate,
            LatestFormADate = query.LatestFormADate,
            IncludeNonGBCases = query.IncludeNonGbCases
        });
        return result.ToList();
    }

    public async Task<IReadOnlyList<OutstandingCaseResult>> GetOutstandingResultsAsync(
        OutstandingSearchQuery query, CancellationToken ct = default)
    {
        var result = await QueryAsync<OutstandingCaseResult>("GetSearchOutstandingResults", new
        {
            EarliestFormADate = query.EarliestFormADate,
            LatestFormADate = query.LatestFormADate,
            IncludeNonGBCases = query.IncludeNonGbCases
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
