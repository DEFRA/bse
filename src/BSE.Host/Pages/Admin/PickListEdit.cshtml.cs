using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Admin;

[Authorize(Policy = "Admin")]
public class PickListEditModel(IEditableLookupAdminService lookupAdminService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int TableId { get; set; }

    [BindProperty]
    public Dictionary<string, string> Fields { get; set; } = [];

    public EditableLookupProcs? Procs { get; private set; }
    public EditableLookup? Lookup { get; private set; }
    public IReadOnlyList<IDictionary<string, object?>> Rows { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var all = await lookupAdminService.GetEditableLookupsAsync();
        Lookup = all.FirstOrDefault(l => l.Id == TableId);
        Procs = await lookupAdminService.GetEditableLookupProcsAsync(TableId);
        if (Procs is not null)
        {
            var rows = await lookupAdminService.GetLookupRowsAsync(Procs.SelectStoredProcedure);
            Rows = rows.ToList();
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        Procs = await lookupAdminService.GetEditableLookupProcsAsync(TableId);
        if (Procs is null) return RedirectToPage();
        try
        {
            await lookupAdminService.AddCodeDescriptionItemAsync(
                Procs.InsertStoredProcedure,
                Fields.GetValueOrDefault("Code", ""),
                Fields.GetValueOrDefault("Description", ""));
            TempData["Success"] = "Row added.";
        }
        catch (Exception ex) { TempData["Error"] = $"Add failed: {ex.Message}"; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        Procs = await lookupAdminService.GetEditableLookupProcsAsync(TableId);
        if (Procs is null) return RedirectToPage();
        try
        {
            await lookupAdminService.DeleteCodeDescriptionItemAsync(
                Procs.DeleteStoredProcedure,
                Fields.GetValueOrDefault("Code", ""));
            TempData["Success"] = "Row deleted.";
        }
        catch (Exception ex) { TempData["Error"] = $"Delete failed: {ex.Message}"; }
        return RedirectToPage();
    }
}
