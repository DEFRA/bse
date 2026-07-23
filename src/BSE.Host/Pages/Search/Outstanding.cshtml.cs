using BSE.Host.Models.ViewModels;
using BSE.Modules.Search.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Search;

[Authorize]
public class OutstandingModel : PageModel
{
    private readonly IOutstandingDataSearchService _search;

    public OutstandingModel(IOutstandingDataSearchService search) => _search = search;

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public OutstandingSearchViewModel Filter { get; set; } = new();

    public async Task OnGetAsync()
    {
        if (Filter.EarliestFormADate.HasValue || Filter.LatestFormADate.HasValue)
        {
            var query = Filter.ToQuery();
            var results = Filter.SearchType switch
            {
                "Fates" => await _search.GetOutstandingFatesAsync(query),
                "Results" => await _search.GetOutstandingResultsAsync(query),
                _ => await _search.GetOutstandingBse1sAsync(query)
            };
            Filter.Results = results.ToList().AsReadOnly();
            Filter.HasSearched = true;
        }
    }
}
