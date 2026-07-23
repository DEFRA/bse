using BSE.Infrastructure;
using BSE.Modules.ReferenceData.Models;

namespace BSE.Modules.ReferenceData.Repositories;

/// <summary>
/// Typed repository for all reference-data read queries.
/// Inherits DapperRepository so each method delegates to the base QueryAsync / QuerySingleOrDefaultAsync,
/// ensuring SP names exactly match the filenames in StoredProcedures/ReferenceData/.
/// </summary>
public sealed class LookupRepository : DapperRepository, ILookupRepository
{
    public LookupRepository(IDbConnectionFactory connectionFactory)
        : base(connectionFactory) { }

    // ── Simple no-param reads ──────────────────────────────────────────────────

    public Task<IEnumerable<LuADNSRegion>> GetADNSRegionsAsync()
        => QueryAsync<LuADNSRegion>("GetluADNSRegion");

    public Task<IEnumerable<LuADNSRegion>> GetADNSRegionsByAuthorityAsync(int authorityId)
        => QueryAsync<LuADNSRegion>("GetluADNSRegionByAuthority", new { AuthorityID = authorityId });

    public Task<IEnumerable<LuAHO>> GetAHOAsync()
        => QueryAsync<LuAHO>("GetluAHO");

    public Task<IEnumerable<LuAHO>> GetAHOCodeAsync()
        => QueryAsync<LuAHO>("GetluAHOCode");

    public Task<IEnumerable<LuAHRO>> GetAHROAsync()
        => QueryAsync<LuAHRO>("GetluAHRO");

    public Task<IEnumerable<LuAHRO>> GetAHROCodeAsync()
        => QueryAsync<LuAHRO>("GetluAHROCode");

    public Task<IEnumerable<LuAnimalOrigin>> GetAnimalOriginsAsync()
        => QueryAsync<LuAnimalOrigin>("GetluAnimalOrigin");

    public Task<IEnumerable<LuAnimalStatus>> GetAnimalStatusesAsync()
        => QueryAsync<LuAnimalStatus>("GetluAnimalStatus");

    public Task<IEnumerable<LookupItem>> GetBirthDateSourcesAsync()
        => QueryAsync<LookupItem>("GetluBirthDateSource");

    public Task<IEnumerable<LuBreed>> GetBreedsAsync()
        => QueryAsync<LuBreed>("GetluBreed");

    public Task<IEnumerable<LuBSECounty>> GetBSECountiesAsync()
        => QueryAsync<LuBSECounty>("GetluBSECounty");

    public Task<IEnumerable<LookupItem>> GetBSEFormsAsync()
        => QueryAsync<LookupItem>("GetluBSEForm");

    public Task<IEnumerable<LuBSERegion>> GetBSERegionsAsync()
        => QueryAsync<LuBSERegion>("GetluBSERegion");

    public Task<IEnumerable<LuCaseFate>> GetCaseFatesAsync()
        => QueryAsync<LuCaseFate>("GetluCaseFate");

    public Task<IEnumerable<LuCaseType>> GetCaseTypesAsync()
        => QueryAsync<LuCaseType>("GetluCaseType");

    public Task<IEnumerable<LookupItem>> GetDocumentTypesAsync()
        => QueryAsync<LookupItem>("GetluDocumentType");

    public Task<IEnumerable<LookupItem>> GetFeedRisksAsync()
        => QueryAsync<LookupItem>("GetluFeedRisk");

    public Task<IEnumerable<LuHerdType>> GetHerdTypesAsync()
        => QueryAsync<LuHerdType>("GetluHerdType");

    public Task<IEnumerable<LookupItem>> GetHorizontalRisksAsync()
        => QueryAsync<LookupItem>("GetluHorizontalRisk");

    public Task<IEnumerable<LookupItem>> GetMaternalRisksAsync()
        => QueryAsync<LookupItem>("GetluMaternalRisk");

    public Task<IEnumerable<LookupItem>> GetOwnerTypesAsync()
        => QueryAsync<LookupItem>("GetluOwnerType");

    public Task<IEnumerable<LookupItem>> GetPedigreeTypesAsync()
        => QueryAsync<LookupItem>("GetluPedigreeType");

    public Task<IEnumerable<LookupItem>> GetRationTypesAsync()
        => QueryAsync<LookupItem>("GetluRationType");

    public Task<IEnumerable<LookupItem>> GetRegionalLabsAsync()
        => QueryAsync<LookupItem>("GetluRegionalLab");

    public Task<IEnumerable<LuRelationFate>> GetRelationFatesAsync()
        => QueryAsync<LuRelationFate>("GetluRelationFate");

    public Task<IEnumerable<LookupItem>> GetRelationTypesAsync()
        => QueryAsync<LookupItem>("GetluRelationType");

    public Task<IEnumerable<LookupItem>> GetReportedLocationsAsync()
        => QueryAsync<LookupItem>("GetluReportedLocation");

    public Task<IEnumerable<LuSex>> GetSexesAsync()
        => QueryAsync<LuSex>("GetluSex");

    public Task<IEnumerable<LuSupplier>> GetSuppliersAsync()
        => QueryAsync<LuSupplier>("GetluSupplier");

    public Task<IEnumerable<LookupItem>> GetSurveysAsync()
        => QueryAsync<LookupItem>("GetluSurvey");

    public Task<IEnumerable<LuTestResult>> GetTestResultsAsync()
        => QueryAsync<LuTestResult>("GetluTestResult");

    public Task<IEnumerable<LuTestType>> GetTestTypesAsync()
        => QueryAsync<LuTestType>("GetluTestType");

    public Task<IEnumerable<LuTSETestingSite>> GetTSETestingSitesAsync()
        => QueryAsync<LuTSETestingSite>("GetluTSETestingSite");

    public Task<IEnumerable<LookupItem>> GetValuationAgesAsync()
        => QueryAsync<LookupItem>("GetluValuationAge");

    public Task<IEnumerable<LuAuthorityCounty>> GetAuthorityCountiesAsync()
        => QueryAsync<LuAuthorityCounty>("GetluAuthorityCountyAll");

    public Task<IEnumerable<LuAuthority>> GetAuthoritiesByCountyAsync(int authorityCountyId)
        => QueryAsync<LuAuthority>("GetluAuthorityByAuthorityCounty", new { AuthorityCountyID = authorityCountyId });

    public Task<IEnumerable<EditableLookup>> GetEditableLookupsAsync()
        => QueryAsync<EditableLookup>("GetEditableLookups");

    public Task<EditableLookupProcs?> GetEditableLookupProcsAsync(int id)
        => QuerySingleOrDefaultAsync<EditableLookupProcs>("GetEditableLookupProcs", new { ID = id });

    // GetluUserGroup SP resides in StoredProcedures/UserManagement/ but is called here as
    // luUserGroup is consumed by reference-data consumers ahead of Slice 3.
    public Task<IEnumerable<LuUserGroup>> GetUserGroupsAsync()
        => QueryAsync<LuUserGroup>("GetluUserGroup");

    // ── Geo lookups ───────────────────────────────────────────────────────────

    public Task<GeoMapReference?> GetMapReferenceByCountyParishAsync(string county, string parish)
        => QuerySingleOrDefaultAsync<GeoMapReference>("GetMapReferenceByCountyParish",
            new { County = county, Parish = parish });

    public Task<ParishLookup?> GetParishByCountyParishAsync(string county, string parish)
        => QuerySingleOrDefaultAsync<ParishLookup>("GetParishByCountyParish",
            new { County = county, Parish = parish });

    public Task<IEnumerable<LuBSECounty>> GetNonGBCountiesAsync()
        => QueryAsync<LuBSECounty>("GetNonGBCounty");

    public Task<IEnumerable<LuSupplier>> GetPossibleSuppliersAsync(string name)
        => QueryAsync<LuSupplier>("GetPossibleSuppliers", new { Name = name });

    public Task<LuSupplier?> GetSupplierByNameAsync(string name)
        => QuerySingleOrDefaultAsync<LuSupplier>("GetSupplierByName", new { Name = name });

    public Task<MapReference?> GetXYReferenceByPrefixCodeAsync(string code)
        => QuerySingleOrDefaultAsync<MapReference>("GetXYReferenceByPrefixCode", new { Code = code });

    public async Task<string?> GetPrefixCodeByXYReferenceAsync(string xCoordPrefix, string yCoordPrefix)
    {
        var row = await QuerySingleOrDefaultAsync<MapReference>(
            "GetPrefixCodeByXYReference",
            new { XCoordPrefix = xCoordPrefix, YCoordPrefix = yCoordPrefix });
        return row?.Code;
    }
}
