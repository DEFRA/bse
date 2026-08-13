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
    [System.ComponentModel.DataAnnotations.RegularExpression("^(?:\\d{9})?$", ErrorMessage = "Enter RBSE as 9 digits.")]
    public string? Rbse { get; set; }

    [BindProperty(SupportsGet = true)] public string? Name { get; set; }
    [BindProperty(SupportsGet = true)] public string? Eartag { get; set; }

    [BindProperty(SupportsGet = true)]
    [System.ComponentModel.DataAnnotations.RegularExpression("^(?:\\d{9})?$", ErrorMessage = "Enter RBSE as 9 digits.")]
    public string? RelationRbse { get; set; }

    [BindProperty(SupportsGet = true)] public string? RelationType { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

    public IReadOnlyList<LookupItem> RelationTypeOptions { get; private set; } = [];
    public IReadOnlyList<RelatedAnimalResult> Results { get; private set; } = [];
    public bool HasSearched { get; private set; }
    public int TotalCount => Results.Count;
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public IReadOnlyList<RelatedAnimalResult> PagedResults =>
        Results.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();

    public async Task OnGetAsync()
    {
        RelationTypeOptions = (await _lookups.GetLookupAsync(LookupTableId.RelationType)).ToList();
        if (!ModelState.IsValid) return;

        if (HasAnyFilter())
        {
            var results = await _search.GetRelatedAnimalsAsync(
                Rbse ?? "", Name ?? "", Eartag ?? "", RelationRbse ?? "", RelationType ?? "");
            Results = results.ToList().AsReadOnly();
            HasSearched = true;
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages) PageNumber = TotalPages;
        }
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        if (!HasAnyFilter()) return RedirectToPage();
        var rows = await _search.GetRelatedAnimalsAsync(
            Rbse ?? "", Name ?? "", Eartag ?? "", RelationRbse ?? "", RelationType ?? "");

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Results");
        string[] headers = ["RBSE", "CPHH", "Relation Type", "Relation Sex", "Eartag",
            "Relation Birth Date", "Relation Fate", "Left Date", "Name", "Relation Eartag", "Relation RBSE"];
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
}
