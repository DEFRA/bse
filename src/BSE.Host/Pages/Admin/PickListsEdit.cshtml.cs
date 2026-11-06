using BSE.Modules.ReferenceData.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BSE.Host.Pages.Admin;

[Authorize(Policy = "PickListAccess")]
public class PickListsEditModel(
    IEditableLookupAdminService lookupAdminService,
    ILookupDataService lookupDataService) : PickListFormPageModelBase(lookupAdminService, lookupDataService)
{
    [BindProperty(SupportsGet = true)] public string Key { get; set; } = string.Empty;

    [BindProperty] public string OriginalKey { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        if (!CanEdit || Procs is null) return RedirectToTable();

        var row = (await LookupAdmin.GetLookupRowsAsync(Procs.SelectStoredProcedure))
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

        try
        {
            ValidateRequired();
            if (await HasDuplicateKeyAsync(OriginalKey)) ModelState.AddModelError(KeyColumn, PickListsModel.DuplicateCodeMessage);
            if (TableId == AhroId && await HasDuplicateAhroNameAsync(OriginalKey)) ModelState.AddModelError("Name", PickListsModel.DuplicateNameMessage);
            if (!ModelState.IsValid) return Page();

            await EditAsync();
            TempData["SuccessMessage"] = "Record updated.";
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 2627 or 2601 or 8152 or 2628 or 547)
        {
            ModelState.AddModelError(KeyColumn, PickListsModel.SaveFailedMessage);
            return Page();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Update failed: {ex.Message}";
            return Page();
        }

        return RedirectToTable();
    }

    protected override string Field(string column) => Fields.GetValueOrDefault(column, "").Trim();

    private bool BoolField(string column) =>
        Fields.TryGetValue(column, out var v) && (v == "true" || v == "on" || v == "1" || v == "True" || v == "Yes");

    private Task EditAsync()
    {
        var original = OriginalKey;
        return TableId switch
        {
            TestTypeId => LookupAdmin.EditTestTypeAsync(original, Field("Code"), Field(DescriptionColumn), BoolField("IsActive")),
            RelationFateId => LookupAdmin.EditRelationFateAsync(original, Field("Code"), Field(DescriptionColumn), BoolField("IsActive")),
            BreedId => LookupAdmin.EditBreedAsync(original, Field("Code"), Field("FullName"), Field("AmalgamatedName")),
            AhoId => LookupAdmin.EditAHOAsync(original, Field("Code"), Field("Name"), IntField(BseRegionIdColumn)),
            SupplierId => LookupAdmin.EditSupplierAsync(ParseKey(original), Field("Name"), Field("Details")),
            BseCountyId => LookupAdmin.EditBSECountyAsync(Field("IDColumn"), original, Field("Code"), Field(DescriptionColumn), IntField(BseRegionIdColumn)),
            TseTestingSiteId => LookupAdmin.EditTSETestingSiteAsync(original, Field("Name"), Field("Address"), Field("CPH"), Field("AHO")),
            AhroId => LookupAdmin.EditAHROAsync(ParseKey(original), Field("Name")),
            _ => LookupAdmin.EditCodeDescriptionItemAsync(Procs!.UpdateStoredProcedure, original, Field("Code"), Field(DescriptionColumn))
        };
    }

    private async Task<bool> HasDuplicateAhroNameAsync(string originalKey)
    {
        var candidate = Field("Name");
        if (string.IsNullOrWhiteSpace(candidate) || Procs is null) return false;

        var rows = await LookupAdmin.GetLookupRowsAsync(Procs.SelectStoredProcedure);
        return rows.Any(r =>
            string.Equals(RowValue(r, "Name"), candidate, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(RowValue(r, KeyColumn), originalKey, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> HasDuplicateKeyAsync(string originalKey)
    {
        if (KeyColumn == "ID") return false;

        var candidate = Field(KeyColumn);
        if (string.IsNullOrWhiteSpace(candidate) || Procs is null) return false;

        var rows = await LookupAdmin.GetLookupRowsAsync(Procs.SelectStoredProcedure);
        return rows.Any(r =>
            string.Equals(RowValue(r, KeyColumn), candidate, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(RowValue(r, KeyColumn), originalKey, StringComparison.OrdinalIgnoreCase));
    }

    private static int ParseKey(string value) => int.TryParse(value, out var id) ? id : 0;

    public static string RowValue(IDictionary<string, object?> row, string column)
    {
        if (!row.TryGetValue(column, out var value) || value is null) return "";
        if (value is bool flag) return flag ? "Yes" : "No";
        return value.ToString() ?? "";
    }
}
