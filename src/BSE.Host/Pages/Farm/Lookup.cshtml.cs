using BSE.Modules.FarmManagement.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Farm;

// Legacy parity: PickFarm.aspx's EnableControls blocks DEFRA Viewer and VLA Data Entry (Home
// redirect), allowing only DEFRA Data Entry, DEFRA Maintenance and VLA Maintenance — the same
// group set as the FarmCreation claim.
[Authorize(Policy = "FarmCreation")]
public class LookupModel : PageModel
{
    private readonly IFarmService _farm;

    public LookupModel(IFarmService farm) => _farm = farm;

    [BindProperty(SupportsGet = true)] public string Cphh { get; set; } = "";
    public bool IsNotFound { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(Cphh)) return Page();

        var normalizedCphh = CphhNormalizer.Normalize(Cphh);
        var farm = await _farm.GetByCphhAsync(normalizedCphh);
        if (farm is null)
        {
            IsNotFound = true;
            return Page();
        }

        return RedirectToPage("/Farm/Details", new { cphh = normalizedCphh });
    }
}
