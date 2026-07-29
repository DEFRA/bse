using BSE.Modules.BsessIntegration.Models;
using BSE.Modules.BsessIntegration.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Bsess;

public class CheckByRbseModel(IBsessCheckService bsessCheckService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Rbse { get; set; } = string.Empty;

    public BsessCheckByRbseResult? Result { get; private set; }
    public bool HasSearched { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(Rbse))
        {
            HasSearched = true;
            Result = await bsessCheckService.GetCheckByRbseAsync(Rbse);
        }
        return Page();
    }
}
