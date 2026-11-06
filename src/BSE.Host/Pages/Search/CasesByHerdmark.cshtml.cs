using System.ComponentModel.DataAnnotations;
using BSE.Modules.Search.Models;
using BSE.Modules.Search.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Search;

[Authorize]
public class CasesByHerdmarkModel : PageModel, ICaseDetailResultsModel
{
    private readonly ICaseSearchService _search;
    private const int PageSize = 10;

    public CasesByHerdmarkModel(ICaseSearchService search) => _search = search;

    // Legacy applied no format rule to the herdmark box; the search is a substring LIKE match
    // on EartagCountry + EartagHerdmark. MaxLength mirrors the legacy textbox.
    [BindProperty(SupportsGet = true)]
    [StringLength(35, ErrorMessage = "Herdmark must be 35 characters or fewer.")]
    public string? Herdmark { get; set; }
    [BindProperty(SupportsGet = true)] public bool IncludeNonGb { get; set; }
    [BindProperty(SupportsGet = true)] public bool Searched { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string SortColumn { get; set; } = "";
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }

    public IReadOnlyList<CaseDetailSearchResult> Results { get; private set; } = [];
    public bool HasSearched { get; private set; }
    public int TotalCount => Results.Count;
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public IReadOnlyList<CaseDetailSearchResult> PagedResults() =>
        ApplySorting(Results)
            .Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    private IEnumerable<CaseDetailSearchResult> ApplySorting(IReadOnlyList<CaseDetailSearchResult> source) =>
        CaseDetailSort.Apply(source, SortColumn, SortDesc);

    public async Task OnGetAsync()
    {
        if (!ModelState.IsValid) return;
        if (!Searched) return;

        // Legacy passed the box through as typed; a blank value makes the SP match every case.
        var results = await _search.GetCasesByEartagHerdmarkAsync((Herdmark ?? "").Trim(), IncludeNonGb);
        Results = results.ToList().AsReadOnly();
        HasSearched = true;
        if (PageNumber < 1) PageNumber = 1;
        if (PageNumber > TotalPages) PageNumber = TotalPages;
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var results = await _search.GetCasesByEartagHerdmarkAsync((Herdmark ?? "").Trim(), IncludeNonGb);
        return CaseDetailExcelExport.Build(ApplySorting(results.ToList()), "casebyherdmarksearchresults.xlsx");
    }
}
