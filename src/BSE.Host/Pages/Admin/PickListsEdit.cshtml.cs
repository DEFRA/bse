using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Admin;

[Authorize(Policy = "PickListAccess")]
public class PickListsEditModel(
    IEditableLookupAdminService lookupAdminService,
    ILookupDataService lookupDataService) : PageModel
{
    private const int TestTypeId = 7;
    private const int BreedId = 13;
    private const int AhoId = 16;
    private const int SupplierId = 17;
    private const int RelationFateId = 19;
    private const int BseCountyId = 23;
    private const int TseTestingSiteId = 27;
    private const int AhroId = 28;

    public sealed record FieldSpec(
        string Column,
        string Label,
        bool IsBoolean = false,
        bool IsNumeric = false,
        bool IsRegionLookup = false);

    [BindProperty(SupportsGet = true)] public int TableId { get; set; }
    [BindProperty(SupportsGet = true)] public string Key { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public string SortColumn { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    [BindProperty] public Dictionary<string, string> Fields { get; set; } = [];
    [BindProperty] public string OriginalKey { get; set; } = string.Empty;

    public EditableLookup? Lookup { get; private set; }
    public EditableLookupProcs? Procs { get; private set; }
    public IReadOnlyList<LuBSERegion> BseRegionOptions { get; private set; } = [];
    public IReadOnlyList<FieldSpec> DisplayFields { get; private set; } = [];

    public bool CanEdit => User.IsInRole("VLAMaintenance");

    public string KeyColumn => TableId switch
    {
        SupplierId or AhroId => "ID",
        TseTestingSiteId => "CPH",
        _ => "Code"
    };

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        if (!CanEdit || Procs is null) return RedirectToTable();

        var row = (await lookupAdminService.GetLookupRowsAsync(Procs.SelectStoredProcedure))
            .FirstOrDefault(r => string.Equals(RowValue(r, KeyColumn), Key, StringComparison.OrdinalIgnoreCase));

        if (row is null) return RedirectToTable();

        OriginalKey = Key;
        Fields = DisplayFields.ToDictionary(
            f => f.Column,
            f => RowValue(row, f.Column));

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadAsync();
        if (!CanEdit || Procs is null) return RedirectToTable();

        ValidateRequired();
        if (await HasDuplicateKeyAsync(OriginalKey)) ModelState.AddModelError(KeyColumn, PickListsModel.DuplicateCodeMessage);
        if (!ModelState.IsValid) return Page();

        try
        {
            await EditAsync();
            TempData["SuccessMessage"] = "Record updated.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Update failed: {ex.Message}";
            return Page();
        }

        return RedirectToTable();
    }

    private async Task LoadAsync()
    {
        Lookup = (await lookupAdminService.GetEditableLookupsAsync()).FirstOrDefault(l => l.Id == TableId);
        Procs = await lookupAdminService.GetEditableLookupProcsAsync(TableId);
        DisplayFields = FieldsFor(TableId);

        if (DisplayFields.Any(f => f.IsRegionLookup))
            BseRegionOptions = (await lookupDataService.GetBSERegionsAsync()).ToList();
    }

    private IActionResult RedirectToTable() =>
        RedirectToPage("/Admin/PickLists", new { tableId = TableId, sortColumn = SortColumn, sortDesc = SortDesc, pageNumber = PageNumber });

    private string Field(string column) => Fields.GetValueOrDefault(column, "").Trim();

    private bool BoolField(string column) =>
        Fields.TryGetValue(column, out var v) && (v == "true" || v == "on" || v == "1" || v == "True" || v == "Yes");

    private int? IntField(string column) =>
        int.TryParse(Field(column), out var value) ? value : null;

    private Task EditAsync()
    {
        var original = OriginalKey;
        return TableId switch
        {
            TestTypeId => lookupAdminService.EditTestTypeAsync(original, Field("Code"), Field("Description"), BoolField("IsActive")),
            RelationFateId => lookupAdminService.EditRelationFateAsync(original, Field("Code"), Field("Description"), BoolField("IsActive")),
            BreedId => lookupAdminService.EditBreedAsync(original, Field("Code"), Field("FullName"), Field("AmalgamatedName")),
            AhoId => lookupAdminService.EditAHOAsync(original, Field("Code"), Field("Name"), IntField("BSERegionID")),
            SupplierId => lookupAdminService.EditSupplierAsync(ParseKey(original), Field("Name"), Field("Details")),
            BseCountyId => lookupAdminService.EditBSECountyAsync(Field("IDColumn"), original, Field("Code"), Field("Description"), IntField("BSERegionID")),
            TseTestingSiteId => lookupAdminService.EditTSETestingSiteAsync(original, Field("Name"), Field("Address"), Field("CPH"), Field("AHO")),
            AhroId => lookupAdminService.EditAHROAsync(ParseKey(original), Field("Name")),
            _ => lookupAdminService.EditCodeDescriptionItemAsync(Procs!.UpdateStoredProcedure, original, Field("Code"), Field("Description"))
        };
    }

    private void ValidateRequired()
    {
        foreach (var field in DisplayFields.Where(f => !f.IsBoolean && !f.IsNumeric))
        {
            if (string.IsNullOrWhiteSpace(Field(field.Column)))
                ModelState.AddModelError(field.Column, $"Enter a {field.Label.ToLowerInvariant()}");
        }
    }

    private async Task<bool> HasDuplicateKeyAsync(string originalKey)
    {
        if (KeyColumn == "ID") return false;

        var candidate = Field(KeyColumn);
        if (string.IsNullOrWhiteSpace(candidate) || Procs is null) return false;

        var rows = await lookupAdminService.GetLookupRowsAsync(Procs.SelectStoredProcedure);
        return rows.Any(r =>
            string.Equals(RowValue(r, KeyColumn), candidate, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(RowValue(r, KeyColumn), originalKey, StringComparison.OrdinalIgnoreCase));
    }

    private static int ParseKey(string value) => int.TryParse(value, out var id) ? id : 0;

    public static string RowValue(IDictionary<string, object?> row, string column)
    {
        if (!row.TryGetValue(column, out var value) || value is null) return "";
        return value is bool flag ? (flag ? "Yes" : "No") : value.ToString() ?? "";
    }

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
}