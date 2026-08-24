using BSE.Modules.Search.Models;
using BSE.Modules.Search.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Search;

[Authorize]
public class CphhModel(IFarmSearchService farmSearchService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Parish { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? County { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Herdmark { get; set; }

    public IReadOnlyList<FarmSearchResult> Results { get; private set; } = [];
    public bool HasSearched { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(Parish) || !string.IsNullOrWhiteSpace(County) || !string.IsNullOrWhiteSpace(Herdmark))
        {
            HasSearched = true;
            var cphh = string.Concat(Parish?.Trim(), County?.Trim(), Herdmark?.Trim()).ToUpperInvariant();
            var query = new FarmSearchQuery(Cphh: cphh.Length >= 4 ? cphh : "",
                                            County: County?.Trim() ?? "",
                                            Herdmark: Herdmark?.Trim() ?? "");
            Results = (await farmSearchService.SearchFarmsAsync(query)).ToList();
        }
        return Page();
    }
}
