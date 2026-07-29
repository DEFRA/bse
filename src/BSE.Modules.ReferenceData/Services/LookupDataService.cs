using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Repositories;
using BSE.SharedKernel;

namespace BSE.Modules.ReferenceData.Services;

public sealed class LookupDataService : ILookupDataService
{
    private readonly ILookupRepository _repo;

    public LookupDataService(ILookupRepository repo) => _repo = repo;

    public Task<IEnumerable<LuBreed>> GetBreedsAsync() => _repo.GetBreedsAsync();
    public Task<IEnumerable<LuCaseType>> GetCaseTypesAsync() => _repo.GetCaseTypesAsync();
    public Task<IEnumerable<LuCaseFate>> GetCaseFatesAsync() => _repo.GetCaseFatesAsync();
    public Task<IEnumerable<LuAnimalStatus>> GetAnimalStatusesAsync() => _repo.GetAnimalStatusesAsync();
    public Task<IEnumerable<LuAnimalOrigin>> GetAnimalOriginsAsync() => _repo.GetAnimalOriginsAsync();
    public Task<IEnumerable<LuSex>> GetSexesAsync() => _repo.GetSexesAsync();
    public Task<IEnumerable<LuHerdType>> GetHerdTypesAsync() => _repo.GetHerdTypesAsync();
    public Task<IEnumerable<LuADNSRegion>> GetADNSRegionsAsync() => _repo.GetADNSRegionsAsync();
    public Task<IEnumerable<LuBSECounty>> GetCountiesAsync() => _repo.GetBSECountiesAsync();
    public Task<IEnumerable<LuBSERegion>> GetBSERegionsAsync() => _repo.GetBSERegionsAsync();
    public Task<IEnumerable<LuTestType>> GetTestTypesAsync() => _repo.GetTestTypesAsync();
    public Task<IEnumerable<LuTestResult>> GetTestResultsAsync() => _repo.GetTestResultsAsync();
    public Task<IEnumerable<LuUserGroup>> GetUserGroupsAsync() => _repo.GetUserGroupsAsync();

    public async Task<IEnumerable<LookupItem>> GetLookupAsync(LookupTableId tableId)
    {
        return tableId switch
        {
            LookupTableId.ADNSRegion =>
                (await _repo.GetADNSRegionsAsync()).Select(x => new LookupItem(x.Id, x.Name)),
            LookupTableId.AHO =>
                (await _repo.GetAHOAsync()).Select(x => new LookupItem(x.Id, x.Name)),
            LookupTableId.AHRO =>
                (await _repo.GetAHROAsync()).Select(x => new LookupItem(x.Id, x.Name)),
            LookupTableId.AnimalOrigin =>
                (await _repo.GetAnimalOriginsAsync()).Select(x => new LookupItem(x.Id, x.Description)),
            LookupTableId.AnimalStatus =>
                (await _repo.GetAnimalStatusesAsync()).Select(x => new LookupItem(x.Id, x.Description)),
            LookupTableId.BirthDateSource =>
                await _repo.GetBirthDateSourcesAsync(),
            LookupTableId.Breed =>
                (await _repo.GetBreedsAsync()).Select(x => new LookupItem(x.Id, x.FullName)),
            LookupTableId.BSECounty =>
                (await _repo.GetBSECountiesAsync()).Select(x => new LookupItem(x.Id, x.Description)),
            LookupTableId.BSEForm =>
                await _repo.GetBSEFormsAsync(),
            LookupTableId.BSERegion =>
                (await _repo.GetBSERegionsAsync()).Select(x => new LookupItem(x.Id, x.Name)),
            LookupTableId.CaseFate =>
                (await _repo.GetCaseFatesAsync()).Select(x => new LookupItem(x.Id, x.Description)),
            LookupTableId.CaseType =>
                (await _repo.GetCaseTypesAsync()).Select(x => new LookupItem(x.Id, x.Description)),
            LookupTableId.DocumentType =>
                await _repo.GetDocumentTypesAsync(),
            LookupTableId.FeedRisk =>
                await _repo.GetFeedRisksAsync(),
            LookupTableId.HerdType =>
                (await _repo.GetHerdTypesAsync()).Select(x => new LookupItem(x.Id, x.Description)),
            LookupTableId.HorizontalRisk =>
                await _repo.GetHorizontalRisksAsync(),
            LookupTableId.MaternalRisk =>
                await _repo.GetMaternalRisksAsync(),
            LookupTableId.OwnerType =>
                await _repo.GetOwnerTypesAsync(),
            LookupTableId.PedigreeType =>
                await _repo.GetPedigreeTypesAsync(),
            LookupTableId.RationType =>
                await _repo.GetRationTypesAsync(),
            LookupTableId.RegionalLab =>
                await _repo.GetRegionalLabsAsync(),
            LookupTableId.RelationFate =>
                (await _repo.GetRelationFatesAsync()).Select(x => new LookupItem(x.Id, x.Description)),
            LookupTableId.RelationType =>
                await _repo.GetRelationTypesAsync(),
            LookupTableId.ReportedLocation =>
                await _repo.GetReportedLocationsAsync(),
            LookupTableId.Sex =>
                (await _repo.GetSexesAsync()).Select(x => new LookupItem(x.Id, x.Description)),
            LookupTableId.Supplier =>
                (await _repo.GetSuppliersAsync()).Select(x => new LookupItem(x.Id, x.Name)),
            LookupTableId.Survey =>
                await _repo.GetSurveysAsync(),
            LookupTableId.TestResult =>
                (await _repo.GetTestResultsAsync()).Select(x => new LookupItem(x.Id, x.Description)),
            LookupTableId.TestType =>
                (await _repo.GetTestTypesAsync()).Select(x => new LookupItem(x.Id, x.Description)),
            LookupTableId.TSETestingSite =>
                (await _repo.GetTSETestingSitesAsync()).Select(x => new LookupItem(x.Id, x.Name)),
            LookupTableId.UserGroup =>
                (await _repo.GetUserGroupsAsync()).Select(x => new LookupItem(x.Id, x.Name)),
            LookupTableId.ValuationAge =>
                await _repo.GetValuationAgesAsync(),
            LookupTableId.AuthorityCounty =>
                (await _repo.GetAuthorityCountiesAsync()).Select(x => new LookupItem(x.Id, x.County)),
            _ => throw new ArgumentOutOfRangeException(nameof(tableId), tableId, "Unknown LookupTableId.")
        };
    }
}
