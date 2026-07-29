using BSE.Modules.Search.Models;
using BSE.Modules.Search.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Search;

[Authorize]
public class RelatedAnimalsModel : PageModel
{
    private readonly ICaseSearchService _search;

    public RelatedAnimalsModel(ICaseSearchService search) => _search = search;

    [BindProperty(SupportsGet = true)] public string Rbse { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string Name { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string Eartag { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string RelationRbse { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string RelationType { get; set; } = "";

    public IReadOnlyList<RelatedAnimalResult> Results { get; private set; } = [];
    public bool HasSearched { get; private set; }

    public async Task OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(Rbse) || !string.IsNullOrWhiteSpace(Eartag) || !string.IsNullOrWhiteSpace(Name))
        {
            var results = await _search.GetRelatedAnimalsAsync(Rbse, Name, Eartag, RelationRbse, RelationType);
            Results = results.ToList().AsReadOnly();
            HasSearched = true;
        }
    }
}
