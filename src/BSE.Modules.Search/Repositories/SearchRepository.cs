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

    public async Task<IReadOnlyList<CaseSearchResult>> SearchCasesAsync(CaseSearchQuery q, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("RBSE",                    q.Rbse         ?? "", DbType.AnsiString,  size: 9);
        p.Add("Eartag",                  q.Eartag       ?? "", DbType.AnsiString,  size: 35);
        p.Add("DBSE",                    q.Dbse         ?? "", DbType.AnsiString,  size: 7);
        p.Add("Fate",                    q.Fate         ?? "", DbType.AnsiString,  size: 4);
        p.Add("FinalResult",             q.FinalResult  ?? "", DbType.AnsiString,  size: 5);
        p.Add("Sex",                     q.Sex          ?? "", DbType.AnsiString,  size: 1);
        p.Add("Survey",                  q.Survey       ?? "", DbType.AnsiString,  size: 4);
        p.Add("Notes",                   q.Notes        ?? "", DbType.AnsiString,  size: 500);
        p.Add("PassiveActive",           q.PassiveActive ?? "", DbType.AnsiString,  size: 1);
        p.Add("EarliestFormADate",       (object?)q.EarliestFormADate       ?? DBNull.Value, DbType.DateTime);
        p.Add("LatestFormADate",         (object?)q.LatestFormADate         ?? DBNull.Value, DbType.DateTime);
        p.Add("EarliestFinalResultDate", (object?)q.EarliestFinalResultDate ?? DBNull.Value, DbType.DateTime);
        p.Add("LatestFinalResultDate",   (object?)q.LatestFinalResultDate   ?? DBNull.Value, DbType.DateTime);
        p.Add("EarliestBirthDate",       (object?)q.EarliestBirthDate       ?? DBNull.Value, DbType.DateTime);
        p.Add("LatestBirthDate",         (object?)q.LatestBirthDate         ?? DBNull.Value, DbType.DateTime);
        p.Add("IncludeNonGBCases",       q.IncludeNonGbCases,         DbType.Boolean);
        p.Add("IsImportedCase",          q.IsImportedCase,            DbType.Boolean);

        var result = await QueryAsync<CaseSearchResult>("GetSearchCase", p, SearchCommandTimeoutSeconds);
        return result.ToList();
    }

    public async Task<IReadOnlyList<FarmSearchResult>> SearchFarmsAsync(FarmSearchQuery q, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("CPHH",             q.Cphh             ?? string.Empty, DbType.AnsiString, size: 11);
        p.Add("OwnerName",        q.OwnerName        ?? string.Empty, DbType.AnsiString, size: 100);
        p.Add("Address",          q.Address          ?? string.Empty, DbType.AnsiString, size: 160);
        p.Add("County",           q.County           ?? string.Empty, DbType.AnsiString, size: 15);
        p.Add("Herdmark",         q.Herdmark         ?? string.Empty, DbType.AnsiString, size: 8);
        p.Add("NumericHerdmark",  q.NumericHerdmark  ?? string.Empty, DbType.AnsiString, size: 6);
        p.Add("IsDealer",         q.IsDealer,                                DbType.Boolean);
        p.Add("AHO",              q.Aho              ?? string.Empty, DbType.AnsiString, size: 2);
        p.Add("IncludeNonGBFarms", q.IncludeNonGbFarms,                      DbType.Boolean);

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
