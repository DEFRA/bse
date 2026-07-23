using BSE.Infrastructure;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Repositories;

namespace BSE.Modules.ReferenceData.Services;

public sealed class EditableLookupAdminService : IEditableLookupAdminService
{
    private readonly ILookupRepository _repo;
    private readonly IDbRepository _db;

    public EditableLookupAdminService(ILookupRepository repo, IDbRepository db)
    {
        _repo = repo;
        _db = db;
    }

    public Task<IEnumerable<EditableLookup>> GetEditableLookupsAsync()
        => _repo.GetEditableLookupsAsync();

    public Task<EditableLookupProcs?> GetEditableLookupProcsAsync(int tableId)
        => _repo.GetEditableLookupProcsAsync(tableId);

    public async Task<IEnumerable<IDictionary<string, object?>>> GetLookupRowsAsync(string selectSpName)
    {
        var rows = await _db.QueryAsync<dynamic>(selectSpName);
        return rows.Select(r => (IDictionary<string, object?>)(IDictionary<string, object>)r);
    }

    // ── Common Code + Description ─────────────────────────────────────────────

    public Task AddCodeDescriptionItemAsync(string addSpName, string code, string description)
        => _db.ExecuteAsync(addSpName, new { Code = code, Description = description });

    public Task EditCodeDescriptionItemAsync(string editSpName, string originalCode, string code, string description)
        => _db.ExecuteAsync(editSpName, new { Original_Code = originalCode, Code = code, Description = description });

    public Task DeleteCodeDescriptionItemAsync(string deleteSpName, string code)
        => _db.ExecuteAsync(deleteSpName, new { Code = code });

    // ── Breed ─────────────────────────────────────────────────────────────────

    public Task AddBreedAsync(string code, string fullName, string amalgamatedName)
        => _db.ExecuteAsync("AddluBreed",
            new { Code = code, FullName = fullName, AmalgamatedName = amalgamatedName });

    public Task EditBreedAsync(string originalCode, string code, string fullName, string amalgamatedName)
        => _db.ExecuteAsync("EditluBreed",
            new { Original_Code = originalCode, Code = code, FullName = fullName, AmalgamatedName = amalgamatedName });

    public Task DeleteBreedAsync(string code)
        => _db.ExecuteAsync("DeleteluBreed", new { Code = code });

    // ── TestType ──────────────────────────────────────────────────────────────

    public Task AddTestTypeAsync(string code, string description, bool isActive)
        => _db.ExecuteAsync("AddluTestType",
            new { Code = code, Description = description, IsActive = isActive });

    public Task EditTestTypeAsync(string originalCode, string code, string description, bool isActive)
        => _db.ExecuteAsync("EditluTestType",
            new { Original_Code = originalCode, Code = code, Description = description, IsActive = isActive });

    public Task DeleteTestTypeAsync(string code)
        => _db.ExecuteAsync("DeleteluTestType", new { Code = code });

    // ── RelationFate ──────────────────────────────────────────────────────────

    public Task AddRelationFateAsync(string code, string description, bool isActive)
        => _db.ExecuteAsync("AddluRelationFate",
            new { Code = code, Description = description, IsActive = isActive });

    public Task EditRelationFateAsync(string originalCode, string code, string description, bool isActive)
        => _db.ExecuteAsync("EditluRelationFate",
            new { Original_Code = originalCode, Code = code, Description = description, IsActive = isActive });

    public Task DeleteRelationFateAsync(string code)
        => _db.ExecuteAsync("DeleteluRelationFate", new { Code = code });

    // ── BSE County ────────────────────────────────────────────────────────────

    public Task AddBSECountyAsync(string idColumn, string code, string description, int? bseRegionId)
        => _db.ExecuteAsync("AddluBSECounty",
            new { IDColumn = idColumn, Code = code, Description = description, BSERegionID = bseRegionId });

    public Task EditBSECountyAsync(string idColumn, string originalCode, string code, string description, int? bseRegionId)
        => _db.ExecuteAsync("EditluBSECounty",
            new { IDColumn = idColumn, Original_Code = originalCode, Code = code, Description = description, BSERegionID = bseRegionId });

    public Task DeleteBSECountyAsync(string code)
        => _db.ExecuteAsync("DeleteluBSECounty", new { Code = code });

    // ── AHO ───────────────────────────────────────────────────────────────────

    public Task AddAHOAsync(string code, string name, int? bseRegionId)
        => _db.ExecuteAsync("AddluAHO",
            new { Code = code, Name = name, BSERegionID = bseRegionId });

    public Task EditAHOAsync(string originalCode, string code, string name, int? bseRegionId)
        => _db.ExecuteAsync("EditluAHO",
            new { Original_Code = originalCode, Code = code, Name = name, BSERegionID = bseRegionId });

    public Task DeleteAHOAsync(string code)
        => _db.ExecuteAsync("DeleteluAHO", new { Code = code });

    // ── AHRO ──────────────────────────────────────────────────────────────────

    public Task AddAHROAsync(string name)
        => _db.ExecuteAsync("AddluAHRO", new { Name = name });

    public Task EditAHROAsync(int id, string name)
        => _db.ExecuteAsync("EditluAHRO", new { ID = id, Name = name });

    public Task DeleteAHROAsync(string name)
        => _db.ExecuteAsync("DeleteluAHRO", new { Name = name });

    // ── Supplier ──────────────────────────────────────────────────────────────

    public Task AddSupplierAsync(string name, string? details)
        => _db.ExecuteAsync("AddluSupplier", new { Name = name, Details = details });

    public Task EditSupplierAsync(int id, string name, string? details)
        => _db.ExecuteAsync("EditluSupplier", new { ID = id, Name = name, Details = details });

    public Task DeleteSupplierAsync(int id)
        => _db.ExecuteAsync("DeleteluSupplier", new { ID = id });

    // ── ADNS Region ───────────────────────────────────────────────────────────

    public Task AddADNSRegionAsync(int id, string name)
        => _db.ExecuteAsync("AddluADNSRegion", new { ID = id, Name = name });

    public Task EditADNSRegionAsync(int id, string name)
        => _db.ExecuteAsync("EditluADNSRegion", new { ID = id, Name = name });

    public Task DeleteADNSRegionAsync(int id)
        => _db.ExecuteAsync("DeleteluADNSRegion", new { ID = id });

    // ── TSE Testing Site ──────────────────────────────────────────────────────

    public Task AddTSETestingSiteAsync(string name, string address, string cph, string aho)
        => _db.ExecuteAsync("AddluTSETestingSite",
            new { Name = name, Address = address, CPH = cph, AHO = aho });

    public Task EditTSETestingSiteAsync(string originalCph, string name, string address, string cph, string aho)
        => _db.ExecuteAsync("EditluTSETestingSite",
            new { Original_CPH = originalCph, Name = name, Address = address, CPH = cph, AHO = aho });

    public Task DeleteTSETestingSiteAsync(string cph)
        => _db.ExecuteAsync("DeleteluTSETestingSite", new { CPH = cph });
}
