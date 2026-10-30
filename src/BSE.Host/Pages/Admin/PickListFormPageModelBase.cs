using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Admin;

/// <summary>State, field definitions and validation shared by the pick-list add and edit pages.</summary>
public abstract class PickListFormPageModelBase(
    IEditableLookupAdminService lookupAdminService,
    ILookupDataService lookupDataService) : PageModel, IPickListFormModel
{
    protected const string BseRegionIdColumn = "BSERegionID";
    protected const string DescriptionColumn = "Description";

    protected const int TestTypeId = 7;
    protected const int BreedId = 13;
    protected const int AhoId = 16;
    protected const int SupplierId = 17;
    protected const int RelationFateId = 19;
    protected const int BseCountyId = 23;
    protected const int TseTestingSiteId = 27;
    protected const int AhroId = 28;

    public sealed record FieldSpec(
        string Column,
        string Label,
        bool IsBoolean = false,
        bool IsNumeric = false,
        bool IsRegionLookup = false,
        bool IsAhoLookup = false);

    protected IEditableLookupAdminService LookupAdmin { get; } = lookupAdminService;

    [BindProperty(SupportsGet = true)] public int TableId { get; set; }
    [BindProperty(SupportsGet = true)] public string SortColumn { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    [BindProperty] public Dictionary<string, string> Fields { get; set; } = [];

    public EditableLookup? Lookup { get; private set; }
    public EditableLookupProcs? Procs { get; private set; }
    public IReadOnlyList<LuBseRegion> BseRegionOptions { get; private set; } = [];
    public IReadOnlyList<ILookupItem> AhoOptions { get; private set; } = [];
    public IReadOnlyList<FieldSpec> DisplayFields { get; private set; } = [];

    public bool CanEdit => User.IsInRole("VLAMaintenance");

    public string KeyColumn => TableId switch
    {
        SupplierId or AhroId => "ID",
        TseTestingSiteId => "CPH",
        _ => "Code"
    };

    protected abstract string Field(string column);

    protected async Task LoadAsync()
    {
        Lookup = (await LookupAdmin.GetEditableLookupsAsync()).FirstOrDefault(l => l.Id == TableId);
        Procs = await LookupAdmin.GetEditableLookupProcsAsync(TableId);
        DisplayFields = FieldsFor(TableId);

        if (DisplayFields.Any(f => f.IsRegionLookup))
            BseRegionOptions = (await lookupDataService.GetBSERegionsAsync()).ToList();

        if (DisplayFields.Any(f => f.IsAhoLookup))
            AhoOptions = (await lookupDataService.GetLookupAsync(LookupTableId.AHO)).ToList();
    }

    protected RedirectToPageResult RedirectToTable() =>
        RedirectToPage("/Admin/PickLists", new { tableId = TableId, sortColumn = SortColumn, sortDesc = SortDesc, pageNumber = PageNumber });

    protected int? IntField(string column) =>
        int.TryParse(Field(column), out var value) ? value : null;

    protected void ValidateRequired()
    {
        foreach (var field in DisplayFields.Where(f => !f.IsBoolean && !f.IsNumeric && string.IsNullOrWhiteSpace(Field(f.Column))))
        {
            ModelState.AddModelError(field.Column, $"Enter a {field.Label.ToLowerInvariant()}");
        }

        foreach (var field in DisplayFields.Where(f => (f.IsRegionLookup || f.IsAhoLookup) && string.IsNullOrWhiteSpace(Field(f.Column))))
        {
            ModelState.AddModelError(field.Column, $"Select a {field.Label.ToLowerInvariant()}");
        }
    }

    private static IReadOnlyList<FieldSpec> FieldsFor(int tableId) => tableId switch
    {
        TestTypeId or RelationFateId =>
        [
            new("Code", "Code"),
            new(DescriptionColumn, DescriptionColumn),
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
            new(BseRegionIdColumn, "BSE Region", IsNumeric: true, IsRegionLookup: true)
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
            new(DescriptionColumn, DescriptionColumn),
            new(BseRegionIdColumn, "BSE Region", IsNumeric: true, IsRegionLookup: true)
        ],
        TseTestingSiteId =>
        [
            new("CPH", "CPH"),
            new("Name", "Name"),
            new("Address", "Address"),
            new("AHO", "AHO", IsAhoLookup: true)
        ],
        AhroId => [new("Name", "Name")],
        _ =>
        [
            new("Code", "Code"),
            new(DescriptionColumn, DescriptionColumn)
        ]
    };
}
