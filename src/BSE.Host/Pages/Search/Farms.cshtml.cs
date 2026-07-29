using BSE.Host.Models.ViewModels;
using BSE.Modules.Search.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Search;

[Authorize]
public class FarmsModel : PageModel
{
    private readonly IFarmSearchService _search;

    public FarmsModel(IFarmSearchService search) => _search = search;

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public FarmSearchViewModel Filter { get; set; } = new();

    public async Task OnGetAsync()
    {
        if (HasAnyFilter())
        {
            var results = await _search.SearchFarmsAsync(Filter.ToQuery());
            Filter.Results = results.ToList().AsReadOnly();
            Filter.HasSearched = true;
        }
    }

    private bool HasAnyFilter() =>
        !string.IsNullOrWhiteSpace(Filter.Cphh) ||
        !string.IsNullOrWhiteSpace(Filter.OwnerName) ||
        !string.IsNullOrWhiteSpace(Filter.Address) ||
        !string.IsNullOrWhiteSpace(Filter.County) ||
        !string.IsNullOrWhiteSpace(Filter.Herdmark);
}
