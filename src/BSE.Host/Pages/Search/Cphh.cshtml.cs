using BSE.Modules.Search.Models;
using BSE.Modules.Search.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Search;

[Authorize]
public class CphhModel(IFarmSearchService farmSearchService) : PageModel
{
    private const int PageSize = 10;

    [BindProperty(SupportsGet = true)]
    public string? Parish { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? County { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Herdmark { get; set; }

    [BindProperty(SupportsGet = true)] public string SortColumn { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public IReadOnlyList<FarmSearchResult> Results { get; private set; } = [];
    public bool HasSearched { get; private set; }

    public int TotalCount => Results.Count;
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public IReadOnlyList<FarmSearchResult> PagedResults =>
        ApplySorting(Results).Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

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

    private IEnumerable<FarmSearchResult> ApplySorting(IEnumerable<FarmSearchResult> results) => SortColumn switch
    {
        "OwnerName" => SortDesc ? results.OrderByDescending(f => f.OwnerName) : results.OrderBy(f => f.OwnerName),
        "Address" => SortDesc ? results.OrderByDescending(f => f.Address) : results.OrderBy(f => f.Address),
        "County" => SortDesc ? results.OrderByDescending(f => f.County) : results.OrderBy(f => f.County),
        "Herdmark" => SortDesc ? results.OrderByDescending(f => f.Herdmark) : results.OrderBy(f => f.Herdmark),
        "CasesCount" => SortDesc ? results.OrderByDescending(f => f.CasesCount) : results.OrderBy(f => f.CasesCount),
        "ConfirmedCasesCount" => SortDesc ? results.OrderByDescending(f => f.ConfirmedCasesCount) : results.OrderBy(f => f.ConfirmedCasesCount),
        "Cphh" => SortDesc ? results.OrderByDescending(f => f.Cphh) : results.OrderBy(f => f.Cphh),
        _ => results.OrderBy(f => f.Cphh),
    };
}
