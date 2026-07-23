using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Admin;

[Authorize(Policy = "Admin")]
public class PickListsModel(IEditableLookupAdminService lookupAdminService) : PageModel
{
    public IEnumerable<EditableLookup> Lookups { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        Lookups = await lookupAdminService.GetEditableLookupsAsync();
        return Page();
    }
}
