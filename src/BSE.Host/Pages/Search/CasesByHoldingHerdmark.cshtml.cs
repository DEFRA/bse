using System.ComponentModel.DataAnnotations;
using BSE.Modules.Search.Models;
using BSE.Modules.Search.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BSE.SharedKernel;

namespace BSE.Host.Pages.Search;

[Authorize]
public class CasesByHoldingHerdmarkModel : PageModel, ICaseDetailResultsModel
{
    private readonly ICaseSearchService _search;
    private const int PageSize = 10;

    public CasesByHoldingHerdmarkModel(ICaseSearchService search) => _search = search;

    [BindProperty(SupportsGet = true)]
    // Legacy searched by CPHH prefix (LIKE @CPHH + '%'), so a partial value such as "01" is valid.
    [RegularExpression(@"^(\d{2}(/)?(\d{0,3}(/)?(\d{0,4}(/)?\d{0,2})?)?)?$", ErrorMessage = "Enter CPHH as digits in the format NN[/]NNN[/]NNNN[/NN], or a shorter prefix such as the first 2 digits.")]
    public string? Cphh { get; set; }

    [BindProperty(SupportsGet = true)]
    [RegularExpression("^[A-Za-z]{0,4}[0-9]{0,4}$", ErrorMessage = "Enter a valid herdmark.")]
    public string? Herdmark { get; set; }

    [BindProperty(SupportsGet = true)]
    [RegularExpression("^(?:\\d{6})?$", ErrorMessage = "Numeric herdmark must be 6 digits.")]
    public string? NumericHerdmark { get; set; }
    [BindProperty(SupportsGet = true)] public bool IncludeNonGb { get; set; }
    [BindProperty(SupportsGet = true)] public bool Searched { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string SortColumn { get; set; } = "";
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }

    public IReadOnlyList<CaseDetailSearchResult> Results { get; private set; } = [];
    public bool HasSearched { get; private set; }

    public const string NoCriteriaMessage = "Please provide one or more search criteria";

    public bool NoCriteria { get; private set; }
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

        if (!string.IsNullOrWhiteSpace(Cphh) || !string.IsNullOrWhiteSpace(Herdmark) || !string.IsNullOrWhiteSpace(NumericHerdmark))
        {
            var results = await _search.GetCasesByCphhAsync(
                CphhNormalizer.Normalize(Cphh),
                (Herdmark ?? "").Trim(),
                (NumericHerdmark ?? "").Trim(),
                IncludeNonGb);
            Results = results.ToList().AsReadOnly();
            HasSearched = true;
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages) PageNumber = TotalPages;
        }
        else
        {
            NoCriteria = Searched || Request.Query.Count > 0;
        }
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        if (!HasAnyFilter()) return RedirectToPage();
        var results = await _search.GetCasesByCphhAsync(
            CphhNormalizer.Normalize(Cphh), (Herdmark ?? "").Trim(), (NumericHerdmark ?? "").Trim(), IncludeNonGb);
        return CaseDetailExcelExport.Build(ApplySorting(results.ToList()), "casebycphhsearchresults.xlsx");
    }

    private bool HasAnyFilter() =>
        !string.IsNullOrWhiteSpace(Cphh) || !string.IsNullOrWhiteSpace(Herdmark) || !string.IsNullOrWhiteSpace(NumericHerdmark);
}
