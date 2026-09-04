using BSE.Modules.Search.Models;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.Modules.Search.Services;
using BSE.SharedKernel;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSE.Host.Pages.Search;

[Authorize]
public class RelatedAnimalsModel : PageModel
{
    private readonly ICaseSearchService _search;
    private readonly ILookupDataService _lookups;
    private const int PageSize = 50;

    public RelatedAnimalsModel(ICaseSearchService search, ILookupDataService lookups)
    {
        _search = search;
        _lookups = lookups;
    }

    [BindProperty(SupportsGet = true)]
    [System.ComponentModel.DataAnnotations.RegularExpression(@"^(\d{9}|\d{2}/\d{2}/\d{5})?$", ErrorMessage = "Enter RBSE as 9 digits or in the format XX/XX/XXXXX.")]
    public string? Rbse { get; set; }

    [BindProperty(SupportsGet = true)] public string? Name { get; set; }
    [BindProperty(SupportsGet = true)] public string? Eartag { get; set; }

    [BindProperty(SupportsGet = true)]
    [System.ComponentModel.DataAnnotations.RegularExpression(@"^(\d{9}|\d{2}/\d{2}/\d{5})?$", ErrorMessage = "Enter RBSE as 9 digits or in the format XX/XX/XXXXX.")]
    public string? RelationRbse { get; set; }

    [BindProperty(SupportsGet = true)] public string? RelationType { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string SortColumn { get; set; } = "";
    [BindProperty(SupportsGet = true)] public bool SortDesc { get; set; }

    public IReadOnlyList<LookupItem> RelationTypeOptions { get; private set; } = [];
    public IReadOnlyList<RelatedAnimalResult> Results { get; private set; } = [];
    public bool HasSearched { get; private set; }

    public const string NoCriteriaMessage = "Please provide one or more search criteria";

    public bool NoCriteria { get; private set; }
    public int TotalCount => Results.Count;
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public IReadOnlyList<RelatedAnimalResult> PagedResults =>
        ApplySorting(Results)
            .Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    private IEnumerable<RelatedAnimalResult> ApplySorting(IReadOnlyList<RelatedAnimalResult> source) =>
        (SortColumn?.ToLowerInvariant(), SortDesc) switch
        {
            ("cphh",         false) => source.OrderBy(r => r.Cphh),
            ("cphh",         true)  => source.OrderByDescending(r => r.Cphh),
            ("relationtype", false) => source.OrderBy(r => r.RelationType),
            ("relationtype", true)  => source.OrderByDescending(r => r.RelationType),
            ("relsex",       false) => source.OrderBy(r => r.RelSex),
            ("relsex",       true)  => source.OrderByDescending(r => r.RelSex),
            ("eartag",       false) => source.OrderBy(r => r.Eartag),
            ("eartag",       true)  => source.OrderByDescending(r => r.Eartag),
            ("relbirthdate", false) => source.OrderBy(r => r.RelBirthDate),
            ("relbirthdate", true)  => source.OrderByDescending(r => r.RelBirthDate),
            ("relfate",      false) => source.OrderBy(r => r.RelFate),
            ("relfate",      true)  => source.OrderByDescending(r => r.RelFate),
            ("leftdate",     false) => source.OrderBy(r => r.LeftDate),
            ("leftdate",     true)  => source.OrderByDescending(r => r.LeftDate),
            ("relname",      false) => source.OrderBy(r => r.RelName),
            ("relname",      true)  => source.OrderByDescending(r => r.RelName),
            ("releartag",    false) => source.OrderBy(r => r.RelEartag),
            ("releartag",    true)  => source.OrderByDescending(r => r.RelEartag),
            ("relationrbse", false) => source.OrderBy(r => r.RelationRbse),
            ("relationrbse", true)  => source.OrderByDescending(r => r.RelationRbse),
            _                       => source.OrderBy(r => r.Rbse),
        };

    public async Task OnGetAsync()
    {
        RelationTypeOptions = BuildRelationTypeOptions(await _lookups.GetLookupAsync(LookupTableId.RelationType));
        if (!ModelState.IsValid) return;

        if (HasAnyFilter())
        {
            var results = await _search.GetRelatedAnimalsAsync(
                (Rbse ?? "").Replace("/", ""), Name ?? "", Eartag ?? "", (RelationRbse ?? "").Replace("/", ""), RelationType ?? "");
            Results = results.ToList().AsReadOnly();
            HasSearched = true;
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages) PageNumber = TotalPages;
        }
        else
        {
            NoCriteria = Request.Query.Count > 0;
        }
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        if (!HasAnyFilter()) return RedirectToPage();
        var rows = await _search.GetRelatedAnimalsAsync(
            (Rbse ?? "").Replace("/", ""), Name ?? "", Eartag ?? "", (RelationRbse ?? "").Replace("/", ""), RelationType ?? "");

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Results");
        // Legacy exported the raw result-set column names, not the on-screen captions.
        string[] headers = ["RBSE", "CPHH", "RelationType", "RelSex", "Eartag",
            "RelBirthDate", "RelFate", "LeftDate", "RelName", "RelEartag", "RelationRBSE"];
        for (var c = 1; c <= headers.Length; c++) { ws.Cell(1, c).Value = headers[c - 1]; ws.Cell(1, c).Style.Font.Bold = true; }
        var row = 2;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.Rbse;
            ws.Cell(row, 2).Value = r.Cphh;
            ws.Cell(row, 3).Value = r.RelationType;
            ws.Cell(row, 4).Value = r.RelSex;
            ws.Cell(row, 5).Value = r.Eartag;
            ws.Cell(row, 6).Value = r.RelBirthDate;
            ws.Cell(row, 7).Value = r.RelFate;
            ws.Cell(row, 8).Value = r.LeftDate?.ToString("dd/MM/yyyy");
            ws.Cell(row, 9).Value = r.RelName;
            ws.Cell(row, 10).Value = r.RelEartag;
            ws.Cell(row, 11).Value = r.RelationRbse;
            row++;
        }
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return new FileContentResult(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            { FileDownloadName = $"RelatedAnimals_{DateTime.Today:yyyyMMdd}.xlsx" };
    }

    private bool HasAnyFilter() =>
        !string.IsNullOrWhiteSpace(Rbse) || !string.IsNullOrWhiteSpace(Eartag) ||
        !string.IsNullOrWhiteSpace(Name) || !string.IsNullOrWhiteSpace(RelationRbse) ||
        !string.IsNullOrWhiteSpace(RelationType);

    // Dam and Sire are not lookup rows; the search proc matches them as literal filter values.
    private static List<LookupItem> BuildRelationTypeOptions(IEnumerable<LookupItem> lookups) =>
    [
        new() { Code = "DAM", Description = "Dam" },
        new() { Code = "SIRE", Description = "Sire" },
        .. lookups
    ];
}
