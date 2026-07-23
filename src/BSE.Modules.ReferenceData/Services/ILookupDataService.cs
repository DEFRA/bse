using BSE.Modules.ReferenceData.Models;
using BSE.SharedKernel;

namespace BSE.Modules.ReferenceData.Services;

public interface ILookupDataService
{
    Task<IEnumerable<LuBreed>> GetBreedsAsync();
    Task<IEnumerable<LuCaseType>> GetCaseTypesAsync();
    Task<IEnumerable<LuCaseFate>> GetCaseFatesAsync();
    Task<IEnumerable<LuAnimalStatus>> GetAnimalStatusesAsync();
    Task<IEnumerable<LuAnimalOrigin>> GetAnimalOriginsAsync();
    Task<IEnumerable<LuSex>> GetSexesAsync();
    Task<IEnumerable<LuHerdType>> GetHerdTypesAsync();
    Task<IEnumerable<LuADNSRegion>> GetADNSRegionsAsync();
    Task<IEnumerable<LuBSECounty>> GetCountiesAsync();
    Task<IEnumerable<LuBSERegion>> GetBSERegionsAsync();
    Task<IEnumerable<LuTestType>> GetTestTypesAsync();
    Task<IEnumerable<LuTestResult>> GetTestResultsAsync();
    Task<IEnumerable<LuUserGroup>> GetUserGroupsAsync();

    /// <summary>
    /// Returns a generic Id+Description projection for the specified lookup table.
    /// Useful for populating dropdowns that do not need the full typed record.
    /// </summary>
    Task<IEnumerable<LookupItem>> GetLookupAsync(LookupTableId tableId);
}
