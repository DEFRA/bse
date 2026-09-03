using BSE.Modules.ReferenceData.Models;

namespace BSE.Modules.ReferenceData.Services;

public interface IEditableLookupAdminService
{
    Task<IEnumerable<EditableLookup>> GetEditableLookupsAsync();
    Task<EditableLookupProcs?> GetEditableLookupProcsAsync(int tableId);

    /// <summary>
    /// Executes the Select SP for the given table and returns each row as a
    /// column-name → value dictionary for generic grid rendering in the admin UI.
    /// </summary>
    Task<IEnumerable<IDictionary<string, object?>>> GetLookupRowsAsync(string selectSpName);

    // ── Common Code + Description tables ─────────────────────────────────────
    Task AddCodeDescriptionItemAsync(string addSpName, string code, string description);
    Task EditCodeDescriptionItemAsync(string editSpName, string originalCode, string code, string description);
    Task DeleteCodeDescriptionItemAsync(string deleteSpName, string code);

    // ── Breed (Code + FullName + AmalgamatedName) ─────────────────────────────
    Task AddBreedAsync(string code, string fullName, string amalgamatedName);
    Task EditBreedAsync(string originalCode, string code, string fullName, string amalgamatedName);
    Task DeleteBreedAsync(string code);

    // ── TestType / RelationFate (Code + Description + IsActive) ──────────────
    Task AddTestTypeAsync(string code, string description, bool isActive);
    Task EditTestTypeAsync(string originalCode, string code, string description, bool isActive);
    Task DeleteTestTypeAsync(string code);
    Task AddRelationFateAsync(string code, string description, bool isActive);
    Task EditRelationFateAsync(string originalCode, string code, string description, bool isActive);
    Task DeleteRelationFateAsync(string code);

    // ── BSE County (IDColumn + Code + Description + BSERegionID) ─────────────
    Task AddBSECountyAsync(string idColumn, string code, string description, int? bseRegionId);
    Task EditBSECountyAsync(string idColumn, string originalCode, string code, string description, int? bseRegionId);
    Task DeleteBSECountyAsync(string code);

    // ── AHO (Code + Name + BSERegionID) ──────────────────────────────────────
    Task AddAHOAsync(string code, string name, int? bseRegionId);
    Task EditAHOAsync(string originalCode, string code, string name, int? bseRegionId);
    Task DeleteAHOAsync(string code);

    // ── AHRO (integer-keyed: Name only for Add; ID for Edit/Delete) ───────────
    Task AddAHROAsync(string name);
    Task EditAHROAsync(int id, string name);
    Task DeleteAHROAsync(string name);

    // ── Supplier (integer-keyed: Name + Details) ──────────────────────────────
    Task AddSupplierAsync(string name, string? details);
    Task EditSupplierAsync(int id, string name, string? details);
    Task DeleteSupplierAsync(int id);

    // ── ADNS Region (integer ID + Name) ───────────────────────────────────────
    Task AddADNSRegionAsync(int id, string name);
    Task EditADNSRegionAsync(int id, string name);
    Task DeleteADNSRegionAsync(int id);

    // ── TSE Testing Site (Name + Address + CPH + AHO) ────────────────────────
    Task AddTSETestingSiteAsync(string name, string address, string cph, string aho);
    Task EditTSETestingSiteAsync(string originalCph, string name, string address, string cph, string aho);
    Task DeleteTSETestingSiteAsync(string cph);
}
