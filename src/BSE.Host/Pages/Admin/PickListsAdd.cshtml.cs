using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Admin;

[Authorize(Policy = "PickListAccess")]
public class PickListsAddModel(
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

    [BindProperty(SupportsGet = true)] public int TableId { get; set; }
    [BindProperty(SupportsGet = true)] public string SortColumn { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    [BindProperty] public Dictionary<string, string> Fields { get; set; } = [];

    public EditableLookup? Lookup { get; private set; }
    public EditableLookupProcs? Procs { get; private set; }
    public IReadOnlyList<LuBSERegion> BseRegionOptions { get; private set; } = [];
    public IReadOnlyList<PickListsEditModel.FieldSpec> DisplayFields { get; private set; } = [];

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
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadAsync();
        if (!CanEdit || Procs is null) return RedirectToTable();

        ValidateRequired();
        if (await HasDuplicateKeyAsync()) ModelState.AddModelError(KeyColumn, PickListsModel.DuplicateCodeMessage);
        if (!ModelState.IsValid) return Page();

        try
        {
            await AddAsync();
            TempData["SuccessMessage"] = "Record added.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Add failed: {ex.Message}";
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

    private void ValidateRequired()
    {
        foreach (var field in DisplayFields.Where(f => !f.IsBoolean && !f.IsNumeric))
        {
            if (string.IsNullOrWhiteSpace(Field(field.Column)))
                ModelState.AddModelError(field.Column, $"Enter a {field.Label.ToLowerInvariant()}");
        }
    }

    private async Task<bool> HasDuplicateKeyAsync()
    {
        if (KeyColumn == "ID" || Procs is null) return false;

        var candidate = Field(KeyColumn);
        if (string.IsNullOrWhiteSpace(candidate)) return false;

        var rows = await lookupAdminService.GetLookupRowsAsync(Procs.SelectStoredProcedure);
        return rows.Any(r => string.Equals(PickListsModel.RowValue(r, KeyColumn), candidate, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<PickListsEditModel.FieldSpec> FieldsFor(int tableId) => tableId switch
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
