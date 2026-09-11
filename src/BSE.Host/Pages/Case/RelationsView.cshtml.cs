using BSE.Modules.AnimalRelations.Models;
using BSE.Modules.AnimalRelations.Repositories;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Case;

/// <summary>
/// Migrated equivalent of legacy RelationsPopup.aspx. Legacy opened this in a
/// <c>window.showModalDialog</c> popup — removed from every modern browser — so this
/// is a normal, linked page instead, consistent with the rest of the app.
/// Read-only: shows the dam, sire and related animals of an arbitrary RBSE, not
/// necessarily the case currently open on the Relations tab.
/// </summary>
[Authorize]
public class RelationsViewModel(IAnimalRelationsRepository relationsRepository) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    public RelationDetailsRecord? Details { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var rbse = RbseHelper.ParseToRaw(Rbse);
        if (rbse.Length == 0)
        {
            return RedirectToPage("/Home");
        }

        Details = await relationsRepository.GetRelationsDetailsByRbseAsync(rbse);
        return Page();
    }
}
