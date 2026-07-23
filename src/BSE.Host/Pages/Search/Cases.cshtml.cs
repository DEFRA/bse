using BSE.Host.Models.ViewModels;
using BSE.Modules.Search.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Search;

[Authorize]
public class CasesModel : PageModel
{
    private readonly ICaseSearchService _search;

    public CasesModel(ICaseSearchService search) => _search = search;

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public CaseSearchViewModel Filter { get; set; } = new();

    public async Task OnGetAsync()
    {
        if (HasAnyFilter())
        {
            var results = await _search.SearchCasesAsync(Filter.ToQuery());
            Filter.Results = results.ToList().AsReadOnly();
            Filter.HasSearched = true;
        }
    }

    private bool HasAnyFilter() =>
        !string.IsNullOrWhiteSpace(Filter.Rbse) ||
        !string.IsNullOrWhiteSpace(Filter.Eartag) ||
        !string.IsNullOrWhiteSpace(Filter.Dbse) ||
        !string.IsNullOrWhiteSpace(Filter.Fate) ||
        !string.IsNullOrWhiteSpace(Filter.FinalResult) ||
        !string.IsNullOrWhiteSpace(Filter.Survey) ||
        Filter.EarliestFormADate.HasValue ||
        Filter.LatestFormADate.HasValue;
}
