using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace BSE.Host.Pages.Admin;

[Authorize(Policy = "PickListAccess")]
public class PickListsModel(
    IEditableLookupAdminService lookupAdminService,
    ILookupDataService lookupDataService) : PageModel
{
    private const int PageSize = 10;

    // Legacy PickListMaintenance.aspx defaulted to table 11 (Valuation Age) when none was requested.
    private const int DefaultTableId = 11;

    // Table IDs from legacy Common.vb LOOKUP_* constants.
    private const int TestTypeId = 7;
    private const int BreedId = 13;
    private const int AhoId = 16;
    private const int SupplierId = 17;
    private const int RelationFateId = 19;
    private const int BseCountyId = 23;
    private const int TseTestingSiteId = 27;
    private const int AhroId = 28;

    public const string DuplicateCodeMessage = "The Code you have selected is already used";

    public sealed record FieldSpec(
        string Column,
        string Label,
        bool IsBoolean = false,
        bool IsNumeric = false,
        bool IsRegionLookup = false);

    [BindProperty(SupportsGet = true)] public int TableId { get; set; }
    [BindProperty(SupportsGet = true)] public string SortColumn { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    [BindProperty] public Dictionary<string, string> Fields { get; set; } = [];
    [BindProperty] public string? OriginalKey { get; set; }

    public IReadOnlyList<EditableLookup> Lookups { get; private set; } = [];
    public EditableLookup? Lookup { get; private set; }
    public EditableLookupProcs? Procs { get; private set; }
    public IReadOnlyList<IDictionary<string, object?>> Rows { get; private set; } = [];
    public IReadOnlyList<LuBSERegion> BseRegionOptions { get; private set; } = [];

    /// <summary>Columns rendered in the grid, in the order returned by the table's select procedure.</summary>
    public IReadOnlyList<FieldSpec> DisplayFields { get; private set; } = [];

    public bool CanEdit => User.IsInRole("VLAMaintenance");

    public int TotalCount => Rows.Count;
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public IReadOnlyList<IDictionary<string, object?>> PagedRows =>
        ApplySorting(Rows).Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        await LoadAsync();
        if (!CanEdit || Procs is null) return RedirectToTable();

        ValidateRequired();
        if (HasDuplicateKey(originalKey: null)) ModelState.AddModelError(KeyColumn, DuplicateCodeMessage);
        if (!ModelState.IsValid) return Page();

        try
        {
            await AddAsync();
            TempData["SuccessMessage"] = "Record added.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Add failed: {ex.Message}";
        }
        return RedirectToTable();
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        await LoadAsync();
        if (!CanEdit || Procs is null) return RedirectToTable();

        try
        {
            await DeleteAsync();
            TempData["SuccessMessage"] = "Record deleted.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Delete failed: {ex.Message}";
        }
        return RedirectToTable();
    }

    // ── Loading ──────────────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        Lookups = (await lookupAdminService.GetEditableLookupsAsync()).ToList();

        if (TableId <= 0)
        {
            TableId = Lookups.Any(l => l.Id == DefaultTableId)
                ? DefaultTableId
                : Lookups.FirstOrDefault()?.Id ?? 0;
        }

        Lookup = Lookups.FirstOrDefault(l => l.Id == TableId);
        Procs = await lookupAdminService.GetEditableLookupProcsAsync(TableId);
        DisplayFields = FieldsFor(TableId);

        if (DisplayFields.Any(f => f.IsRegionLookup))
        {
            BseRegionOptions = (await lookupDataService.GetBSERegionsAsync()).ToList();
        }

        if (Procs is null) return;

        Rows = (await lookupAdminService.GetLookupRowsAsync(Procs.SelectStoredProcedure)).ToList();

        if (PageNumber < 1) PageNumber = 1;
        if (PageNumber > TotalPages) PageNumber = TotalPages;
    }

    private IActionResult RedirectToTable() =>
        RedirectToPage(new { tableId = TableId, sortColumn = SortColumn, sortDesc = SortDesc, pageNumber = PageNumber });

    private IEnumerable<IDictionary<string, object?>> ApplySorting(IReadOnlyList<IDictionary<string, object?>> source)
    {
        if (string.IsNullOrEmpty(SortColumn) || !DisplayFields.Any(f => f.Column == SortColumn))
        {
            return source;
        }

        var field = DisplayFields.First(f => f.Column == SortColumn);
        if (field.IsRegionLookup)
        {
            return SortDesc
                ? source.OrderByDescending(r => DisplayValue(r, field), StringComparer.OrdinalIgnoreCase)
                : source.OrderBy(r => DisplayValue(r, field), StringComparer.OrdinalIgnoreCase);
        }

        if (field.IsNumeric)
        {
            return SortDesc
                ? source.OrderByDescending(r => NumericValue(r, SortColumn))
                : source.OrderBy(r => NumericValue(r, SortColumn));
        }

        return SortDesc
            ? source.OrderByDescending(r => RowValue(r, SortColumn), StringComparer.OrdinalIgnoreCase)
            : source.OrderBy(r => RowValue(r, SortColumn), StringComparer.OrdinalIgnoreCase);
    }

    private static decimal NumericValue(IDictionary<string, object?> row, string column) =>
        decimal.TryParse(RowValue(row, column), out var value) ? value : decimal.MinValue;

    // ── Per-table shapes (mirrors the legacy bespoke maintenance pages) ──────

    private static IReadOnlyList<FieldSpec> FieldsFor(int tableId) => tableId switch
    {
        TestTypeId or RelationFateId =>
        [
            new("Code", "Code"),
            new("Description", "Description"),
            new("IsActive", "Is Active", IsBoolean: true)
        ],
        BreedId =>
        [
            new("Code", "Code"),
            new("FullName", "Full Name"),
            new("AmalgamatedName", "Amalgamated Name")
        ],
        AhoId =>
        [
            new("Code", "Code"),
            new("Name", "Name"),
            new("BSERegionID", "BSE Region", IsNumeric: true, IsRegionLookup: true)
        ],
        SupplierId =>
        [
            new("Name", "Name"),
            new("Details", "Details")
        ],
        BseCountyId =>
        [
            new("IDColumn", "ID"),
            new("Code", "Code"),
            new("Description", "Description"),
            new("BSERegionID", "BSE Region", IsNumeric: true, IsRegionLookup: true)
        ],
        TseTestingSiteId =>
        [
            new("CPH", "CPH"),
            new("Name", "Name"),
            new("Address", "Address"),
            new("AHO", "AHO")
        ],
        AhroId => [new("Name", "Name")],
        _ =>
        [
            new("Code", "Code"),
            new("Description", "Description")
        ]
    };

    /// <summary>Column holding the value used to identify a row for edit and delete.</summary>
    public string KeyColumn => TableId switch
    {
        SupplierId or AhroId => "ID",
        TseTestingSiteId => "CPH",
        _ => "Code"
    };

    // ── Writes ───────────────────────────────────────────────────────────────

    private string Field(string column) => Fields.GetValueOrDefault(column, "").Trim();

    private bool BoolField(string column) =>
        Fields.TryGetValue(column, out var v) && (v == "true" || v == "on" || v == "1" || v == "True");

    private int? IntField(string column) =>
        int.TryParse(Field(column), out var value) ? value : null;

    private Task AddAsync() => TableId switch
    {
        TestTypeId => lookupAdminService.AddTestTypeAsync(Field("Code"), Field("Description"), BoolField("IsActive")),
        RelationFateId => lookupAdminService.AddRelationFateAsync(Field("Code"), Field("Description"), BoolField("IsActive")),
        BreedId => lookupAdminService.AddBreedAsync(Field("Code"), Field("FullName"), Field("AmalgamatedName")),
        AhoId => lookupAdminService.AddAHOAsync(Field("Code"), Field("Name"), IntField("BSERegionID")),
        SupplierId => lookupAdminService.AddSupplierAsync(Field("Name"), Field("Details")),
        BseCountyId => lookupAdminService.AddBSECountyAsync(Field("IDColumn"), Field("Code"), Field("Description"), IntField("BSERegionID")),
        TseTestingSiteId => lookupAdminService.AddTSETestingSiteAsync(Field("Name"), Field("Address"), Field("CPH"), Field("AHO")),
        AhroId => lookupAdminService.AddAHROAsync(Field("Name")),
        _ => lookupAdminService.AddCodeDescriptionItemAsync(Procs!.InsertStoredProcedure, Field("Code"), Field("Description"))
    };

    private Task DeleteAsync()
    {
        var key = Field(KeyColumn);
        return TableId switch
        {
            TestTypeId => lookupAdminService.DeleteTestTypeAsync(key),
            RelationFateId => lookupAdminService.DeleteRelationFateAsync(key),
            BreedId => lookupAdminService.DeleteBreedAsync(key),
            AhoId => lookupAdminService.DeleteAHOAsync(key),
            SupplierId => lookupAdminService.DeleteSupplierAsync(ParseKey(key)),
            BseCountyId => lookupAdminService.DeleteBSECountyAsync(key),
            TseTestingSiteId => lookupAdminService.DeleteTSETestingSiteAsync(key),
            AhroId => lookupAdminService.DeleteAHROAsync(Field("Name")),
            _ => lookupAdminService.DeleteCodeDescriptionItemAsync(Procs!.DeleteStoredProcedure, key)
        };
    }

    private static int ParseKey(string value) => int.TryParse(value, out var id) ? id : 0;

    // ── Validation ───────────────────────────────────────────────────────────

    private void ValidateRequired()
    {
        foreach (var field in DisplayFields.Where(f => !f.IsBoolean && !f.IsNumeric))
        {
            if (string.IsNullOrWhiteSpace(Field(field.Column)))
            {
                ModelState.AddModelError(field.Column, $"Enter a {field.Label.ToLowerInvariant()}");
            }
        }
    }

    /// <summary>Legacy blocked saving a Code already used by a different row.</summary>
    private bool HasDuplicateKey(string? originalKey)
    {
        if (KeyColumn == "ID") return false;

        var candidate = Field(KeyColumn);
        if (string.IsNullOrWhiteSpace(candidate)) return false;

        return Rows.Any(r =>
            string.Equals(RowValue(r, KeyColumn), candidate, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(RowValue(r, KeyColumn), originalKey ?? "", StringComparison.OrdinalIgnoreCase));
    }

    public static string RowValue(IDictionary<string, object?> row, string column)
    {
        if (!row.TryGetValue(column, out var value) || value is null) return "";
        return value is bool flag ? (flag ? "Yes" : "No") : value.ToString() ?? "";
    }

    /// <summary>Legacy rendered the BSE Region as its name, not the stored ID.</summary>
    public string DisplayValue(IDictionary<string, object?> row, FieldSpec field)
    {
        var raw = RowValue(row, field.Column);
        if (!field.IsRegionLookup || string.IsNullOrEmpty(raw)) return raw;

        return int.TryParse(raw, out var id)
            ? BseRegionOptions.FirstOrDefault(r => r.Id == id)?.Name ?? ""
            : raw;
    }

    public static bool RowBool(IDictionary<string, object?> row, string column) =>
        row.TryGetValue(column, out var value) && value is bool flag && flag;
}
