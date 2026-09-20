using BSE.Modules.BsessIntegration.Models;
using BSE.Modules.BsessIntegration.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Bsess;

[Authorize(Policy = "AuditAccess")]
public class CheckByRbseModel(IBsessCheckService bsessCheckService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    public BsessCheckByRbseResult? Result { get; private set; }
    public bool HasSearched { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (Request.Query.ContainsKey(nameof(Rbse)))
        {
            if (string.IsNullOrWhiteSpace(Rbse))
            {
                ModelState.AddModelError(nameof(Rbse), "Enter an RBSE number");
            }
            else
            {
                var rawRbse = RbseHelper.ParseToRaw(Rbse);
                if (!RbseHelper.IsValid(rawRbse))
                {
                    ModelState.AddModelError(nameof(Rbse), "Enter the RBSE number in the format 00/00/00000");
                }
                else
                {
                    Rbse = rawRbse;
                }
            }

            if (ModelState.IsValid)
            {
                HasSearched = true;
                Result = await bsessCheckService.GetCheckByRbseAsync(Rbse);
            }
        }
        return Page();
    }
}
