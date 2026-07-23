using BSE.Modules.ReferenceData.Models;
using BSE.SharedKernel;

namespace BSE.Modules.ReferenceData.Repositories;

public interface ILookupRepository
{
    // ── Simple no-param reads ──────────────────────────────────────────────────
    Task<IEnumerable<LuADNSRegion>> GetADNSRegionsAsync();
    Task<IEnumerable<LuADNSRegion>> GetADNSRegionsByAuthorityAsync(int authorityId);
    Task<IEnumerable<LuAHO>> GetAHOAsync();
    Task<IEnumerable<LuAHO>> GetAHOCodeAsync();
    Task<IEnumerable<LuAHRO>> GetAHROAsync();
    Task<IEnumerable<LuAHRO>> GetAHROCodeAsync();
    Task<IEnumerable<LuAnimalOrigin>> GetAnimalOriginsAsync();
    Task<IEnumerable<LuAnimalStatus>> GetAnimalStatusesAsync();
    Task<IEnumerable<LookupItem>> GetBirthDateSourcesAsync();
    Task<IEnumerable<LuBreed>> GetBreedsAsync();
    Task<IEnumerable<LuBSECounty>> GetBSECountiesAsync();
    Task<IEnumerable<LookupItem>> GetBSEFormsAsync();
    Task<IEnumerable<LuBSERegion>> GetBSERegionsAsync();
    Task<IEnumerable<LuCaseFate>> GetCaseFatesAsync();
    Task<IEnumerable<LuCaseType>> GetCaseTypesAsync();
    Task<IEnumerable<LookupItem>> GetDocumentTypesAsync();
    Task<IEnumerable<LookupItem>> GetFeedRisksAsync();
    Task<IEnumerable<LuHerdType>> GetHerdTypesAsync();
    Task<IEnumerable<LookupItem>> GetHorizontalRisksAsync();
    Task<IEnumerable<LookupItem>> GetMaternalRisksAsync();
    Task<IEnumerable<LookupItem>> GetOwnerTypesAsync();
    Task<IEnumerable<LookupItem>> GetPedigreeTypesAsync();
    Task<IEnumerable<LookupItem>> GetRationTypesAsync();
    Task<IEnumerable<LookupItem>> GetRegionalLabsAsync();
    Task<IEnumerable<LuRelationFate>> GetRelationFatesAsync();
    Task<IEnumerable<LookupItem>> GetRelationTypesAsync();
    Task<IEnumerable<LookupItem>> GetReportedLocationsAsync();
    Task<IEnumerable<LuSex>> GetSexesAsync();
    Task<IEnumerable<LuSupplier>> GetSuppliersAsync();
    Task<IEnumerable<LookupItem>> GetSurveysAsync();
    Task<IEnumerable<LuTestResult>> GetTestResultsAsync();
    Task<IEnumerable<LuTestType>> GetTestTypesAsync();
    Task<IEnumerable<LuTSETestingSite>> GetTSETestingSitesAsync();
    Task<IEnumerable<LookupItem>> GetValuationAgesAsync();
    Task<IEnumerable<LuAuthorityCounty>> GetAuthorityCountiesAsync();
    Task<IEnumerable<LuAuthority>> GetAuthoritiesByCountyAsync(int authorityCountyId);
    Task<IEnumerable<EditableLookup>> GetEditableLookupsAsync();
    Task<EditableLookupProcs?> GetEditableLookupProcsAsync(int id);
    Task<IEnumerable<LuUserGroup>> GetUserGroupsAsync();

    // ── Geo lookups ───────────────────────────────────────────────────────────
    Task<GeoMapReference?> GetMapReferenceByCountyParishAsync(string county, string parish);
    Task<ParishLookup?> GetParishByCountyParishAsync(string county, string parish);
    Task<IEnumerable<LuBSECounty>> GetNonGBCountiesAsync();
    Task<IEnumerable<LuSupplier>> GetPossibleSuppliersAsync(string name);
    Task<LuSupplier?> GetSupplierByNameAsync(string name);
    Task<MapReference?> GetXYReferenceByPrefixCodeAsync(string code);
    Task<string?> GetPrefixCodeByXYReferenceAsync(string xCoordPrefix, string yCoordPrefix);
}
