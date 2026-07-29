using BSE.Modules.FarmManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Farm;

[Authorize]
public class LookupModel : PageModel
{
    private readonly IFarmService _farm;

    public LookupModel(IFarmService farm) => _farm = farm;

    [BindProperty(SupportsGet = true)] public string Cphh { get; set; } = "";
    public bool IsNotFound { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(Cphh)) return Page();

        var farm = await _farm.GetByCphhAsync(Cphh.Trim());
        if (farm is null)
        {
            IsNotFound = true;
            return Page();
        }

        return RedirectToPage("/Farm/Details", new { cphh = farm.CPHH });
    }
}
