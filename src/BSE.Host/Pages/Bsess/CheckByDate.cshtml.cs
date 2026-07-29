using BSE.Modules.BsessIntegration.Models;
using BSE.Modules.BsessIntegration.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Bsess;

public class CheckByDateModel(IBsessCheckService bsessCheckService) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-1);
    [BindProperty(SupportsGet = true)] public DateTime EndDate { get; set; } = DateTime.Today;

    public IReadOnlyList<BsessDiscrepancyRecord> Discrepancies { get; private set; } = [];
    public bool HasSearched { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (Request.Query.ContainsKey(nameof(StartDate)))
        {
            HasSearched = true;
            Discrepancies = await bsessCheckService.GetCheckByDateAsync(StartDate, EndDate);
        }
        return Page();
    }
}
