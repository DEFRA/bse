using BSE.Modules.ReferenceData.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BSE.Host.Pages.Admin;

[Authorize(Policy = "PickListAccess")]
public class PickListsAddModel(
    IEditableLookupAdminService lookupAdminService,
    ILookupDataService lookupDataService) : PickListFormPageModelBase(lookupAdminService, lookupDataService)
{
    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        if (!CanEdit || Procs is null) return RedirectToTable();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadAsync();
        Fields ??= [];
        if (!CanEdit || Procs is null) return RedirectToTable();

        try
        {
            ValidateRequired();
            if (await HasDuplicateKeyAsync()) ModelState.AddModelError(KeyColumn, PickListsModel.DuplicateCodeMessage);
            if (TableId == AhroId && await HasDuplicateAhroNameAsync()) ModelState.AddModelError("Name", PickListsModel.DuplicateNameMessage);
            if (!ModelState.IsValid) return Page();

            await AddAsync();
            TempData["SuccessMessage"] = "Record added.";
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 2627 or 2601)
        {
            if (KeyColumn == "ID")
            {
                // No pre-check runs for ID-keyed tables (Supplier/AHRO) — the database's own
                // PK/unique constraint is the only thing that ever catches a genuine duplicate here.
                ModelState.AddModelError(KeyColumn, PickListsModel.DuplicateCodeMessage);
            }
            else
            {
                // HasDuplicateKeyAsync above already ruled out an exact-value match against this
                // KeyColumn, so reaching a duplicate-key error here means the database silently
                // truncated an oversized value until it collided with a shorter existing row (e.g.
                // entering "77" into a column that only holds one digit) — not a genuine typed dupe.
                ModelState.AddModelError(KeyColumn, PickListsModel.AddFailedMessage);
            }
            return Page();
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 8152 or 2628 or 547)
        {
            // e.g. luValuationAge's Code column only accepts a single digit — entering "12" trips
            // a truncation (8152/2628) or CHECK constraint (547) error, not a duplicate key.
            ModelState.AddModelError(KeyColumn, PickListsModel.AddFailedMessage);
            return Page();
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = PickListsModel.AddFailedMessage;
            return Page();
        }

        return RedirectToTable();
    }

    protected override string Field(string column) => Fields?.GetValueOrDefault(column, "")?.Trim() ?? string.Empty;

    private bool BoolField(string column) =>
        Fields.TryGetValue(column, out var v) && (v == "true" || v == "on" || v == "1" || v == "True");

    private Task AddAsync() => TableId switch
    {
        TestTypeId => LookupAdmin.AddTestTypeAsync(Field("Code"), Field(DescriptionColumn), BoolField("IsActive")),
        RelationFateId => LookupAdmin.AddRelationFateAsync(Field("Code"), Field(DescriptionColumn), BoolField("IsActive")),
        BreedId => LookupAdmin.AddBreedAsync(Field("Code"), Field("FullName"), Field("AmalgamatedName")),
        AhoId => LookupAdmin.AddAHOAsync(Field("Code"), Field("Name"), IntField(BseRegionIdColumn)),
        SupplierId => LookupAdmin.AddSupplierAsync(Field("Name"), Field("Details")),
        BseCountyId => LookupAdmin.AddBSECountyAsync(Field("IDColumn"), Field("Code"), Field(DescriptionColumn), IntField(BseRegionIdColumn)),
        TseTestingSiteId => LookupAdmin.AddTSETestingSiteAsync(Field("Name"), Field("Address"), Field("CPH"), Field("AHO")),
        AhroId => LookupAdmin.AddAHROAsync(Field("Name")),
        _ => LookupAdmin.AddCodeDescriptionItemAsync(Procs!.InsertStoredProcedure, Field("Code"), Field(DescriptionColumn))
    };

    private async Task<bool> HasDuplicateAhroNameAsync()
    {
        var candidate = Field("Name");
        if (string.IsNullOrWhiteSpace(candidate) || Procs is null) return false;

        var rows = await LookupAdmin.GetLookupRowsAsync(Procs.SelectStoredProcedure);
        return rows.Any(r => string.Equals(PickListsModel.RowValue(r, "Name"), candidate, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> HasDuplicateKeyAsync()
    {
        if (KeyColumn == "ID" || Procs is null) return false;

        var candidate = Field(KeyColumn);
        if (string.IsNullOrWhiteSpace(candidate)) return false;

        var rows = await LookupAdmin.GetLookupRowsAsync(Procs.SelectStoredProcedure);
        return rows.Any(r => string.Equals(PickListsModel.RowValue(r, KeyColumn), candidate, StringComparison.OrdinalIgnoreCase));
    }
}
